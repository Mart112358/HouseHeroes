# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

HouseHeroes is a family task management app for modern families, including blended families and co-parenting households with shared custody. Product requirements live in `docs/PRDs/inception.md`.

Stack: .NET 9, .NET Aspire (local orchestration), ASP.NET Core + HotChocolate 15 GraphQL, EF Core 9 on SQL Server, React Native mobile app (Expo, Apollo Client with generated types), Microsoft Entra External ID for identity, Terraform on Azure Container Apps.

## Commands

The solution file is `HouseHeroes.slnx`.

```bash
# Run the backend stack (SQL Server container + API). Prompts for the `sql-password` Aspire parameter if not set.
dotnet run --project src/HouseHeroes.AppHost

# Build the backend
dotnet build

# Tests: API integration tests in tests/HouseHeroes.ApiService.Tests (needs Docker: each run starts a SQL Server container via Testcontainers)
dotnet test tests/HouseHeroes.ApiService.Tests
dotnet test tests/HouseHeroes.ApiService.Tests --filter "FullyQualifiedName~TaskPermissionTests.Child_cannot_delete_tasks"
# Projects target net9.0; with only the .NET 10 SDK installed, prefix with DOTNET_ROLL_FORWARD=Major

# EF Core migrations (after changing Models/ or AppDbContext)
dotnet ef migrations add MigrationName --project src/HouseHeroes.ApiService

# After changing the GraphQL schema: rewrite mobile/schema.graphql, then regenerate the typed client
UPDATE_SCHEMA=1 dotnet test tests/HouseHeroes.ApiService.Tests --filter SchemaTests
cd mobile && npm run codegen
```

### Mobile app (run from `mobile/`)

```bash
npm install
npm run ios                 # build a development build and open it in the iOS simulator (expo run:ios)
npm run android             # same for the Android emulator; set EXPO_PUBLIC_GRAPHQL_URL=http://10.0.2.2:5528/graphql
npm start                   # Metro dev server for an already-installed development build
npm run typecheck && npm run lint && npm test   # CI runs these plus a codegen freshness check
npx jest __tests__/tasks-test.ts                # a single test file
```

### Deployment (run from `infra/`)

```bash
./bootstrap-state.sh                 # once per subscription: create the Terraform remote state storage account
./deploy-terraform.sh staging        # provision infra incl. resource group (uses staging.tfvars / production.tfvars)
./deploy-app.sh staging [image-tag]  # build `final` + `migration` Dockerfile targets (linux/amd64), push to ACR, run migration job and wait, then update Container App
./destroy-terraform.sh staging
```

Terraform owns the resource group but ignores image changes on the Container App and migration job; `deploy-app.sh` owns the images. Terraform state is remote (Azure Storage), one file per environment selected via `<environment>.tfbackend`; always `terraform init -reconfigure -backend-config=<environment>.tfbackend` before running Terraform by hand.

Detailed Azure/Terraform setup docs are in `docs/README-Azure-Setup.md` and `docs/README-Terraform-Setup.md`. CI (`.github/workflows/ci-cd.yml`) builds and tests the backend, checks the mobile app (codegen, typecheck, lint, Jest), and runs a vulnerable-package scan.

## Architecture

- **HouseHeroes.AppHost**: Aspire orchestrator. Adds a SQL Server container with a `househeroes` database and wires it into the API via connection string reference.
- **HouseHeroes.ServiceDefaults**: Shared OpenTelemetry, resilience, service discovery, and health checks. `/health` and `/alive` are mapped **only in Development**.
- **HouseHeroes.ApiService**: All backend logic. `Program.cs` sets up JWT bearer auth (Entra ID) and GraphQL (`/graphql`, with projections/filtering/sorting). In Development only, it applies migrations and runs `DatabaseSeeder` on startup (two sample families: Paquin with shared custody, Johnson as a blended family). In Azure, migrations run through a separate Container App job (`househeroes-migration-job`).
- **mobile/**: Expo (SDK 58) React Native app with Expo Router; read `mobile/AGENTS.md` before touching Expo APIs (they change every SDK, so check the versioned docs). Routes live in `src/app/`: the root `_layout.tsx` uses `Stack.Protected` to show `sign-in` (signed out), `welcome` (signed in, no family yet) or the `(tabs)` app (Tasks, Family, Settings) plus `task/*` and `member/*` screens. Other code: `src/auth/` (External ID sign-in via `expo-auth-session` with PKCE; tokens in `expo-secure-store`, refreshed by `TokenStore`), `src/graphql/` (operations in `*.graphql`, Apollo client, and `generated/` typed documents from `npm run codegen`; don't hand-edit generated code), `src/features/` (task and role logic, shared rows and forms, and reminders: `planReminders` decides what to schedule, `RemindersProvider` in the tabs layout keeps `expo-notifications` local notifications in sync, and sign-out cancels them), `src/ui/` (theme and basic components). The API URL and Entra settings are in `src/config.ts`. `ios/` and `android/` are generated and git-ignored; configure native behaviour in `app.json`.

### Auth and multi-tenancy model

- Identity is Entra External ID (`househeroesapp.ciamlogin.com`), which handles email/password sign-up; the API has no password auth of its own. `User.EntraUserId` links a token to a DB user. `UserService` resolves the current user from claims (`ClaimsPrincipalExtensions`); a signed-in user has no DB record until `RegisterNewUser` creates one.
- Each `Family` is a logical tenant. Every query and mutation is `[Authorize]` and scoped to the caller's family; never take a family or user ID from input as the caller's identity.
- Mutations resolve the caller with `userService.GetRegisteredUserAsync(claimsPrincipal)` (`GraphQL/CallerGuards.cs`) and check roles with `user.EnsureGuardian()`. Guardians manage tasks; Children complete tasks assigned to them.
- Joining a family: a Guardian calls `createFamilyInvite(role)` and shares the code; the new user calls `registerNewUser(input: { createNewFamily: false, inviteCode })` and gets the invite's role. Family and member mutations live in `GraphQL/Mutations/FamilyMutations.cs` (a `Mutation` type extension).
- Errors use `GraphQLErrors` codes: `NOT_REGISTERED`, `FORBIDDEN`, `NOT_FOUND`. Records in another family are reported as `NOT_FOUND`.
- Tests authenticate with the `X-Test-User` header (an Entra user ID) via `TestAuthHandler`, and create isolated data with `TestData`.
- Plan and decisions: `docs/ROADMAP.md`.

### Gotchas

- The domain entity `Models.Task` collides with `System.Threading.Tasks.Task`. Existing code writes `System.Threading.Tasks.Task<T>` for async return types and `Models.Task` for the entity. Follow that convention.
- `mobile/schema.graphql` is generated by `SchemaTests`, not by hand; that test fails when the API schema changes until you regenerate it.
- Mobile tests use React Native Testing Library 14, where `render` and `fireEvent` are async (`await` them). Shared test data lives in `mobile/test-support/` because Jest treats every file in `__tests__/` as a test suite.
