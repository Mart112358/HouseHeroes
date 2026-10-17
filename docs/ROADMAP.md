# HouseHeroes MVP Roadmap

Goal (from `docs/PRDs/inception.md`): the app is live on the App Store and Google Play, real families use it regularly, and a feedback loop is running.

Each milestone ends with something a family member can do in the running app. Infrastructure work waits until a milestone needs it.

## Decisions

- **Every member has their own login.** The app targets children old enough to use a phone or tablet. Members join a family with an invite code; the invite sets their role (Guardian or Child).
- **Identity is Entra External ID** (`househeroesapp` tenant), which provides the PRD's email/password sign-up. Google and Apple sign-in can be added later as identity providers without code changes.
- **One family per user for the MVP.** Separated parents share one family as Guardians. A child who belongs to two different families (two blended households) is post-MVP.

## Where things stand (2026-10-17)

- **API:** the data model (Family, User, Task, TaskAssignment) is in place. As of Milestone 1, every operation requires sign-in and is limited to the caller's family, covered by integration tests. Invite codes and member management are in the API (Milestone 2).
- **Mobile:** only the login and welcome screens exist. The welcome screen's create/join actions are stubs. `schema.graphql` and the `.graphql` operations are stale, calling fields the API no longer has. The GraphQL client doesn't send the access token. The Tasks, Family and Settings tabs all show the same placeholder page.
- **Identity:** the app and API use `login.microsoftonline.com` as the authority for the External ID tenant. External ID expects `househeroesapp.ciamlogin.com`. Until that's fixed, sign-up flows won't show and the API will likely reject real tokens.
- **Infra:** Terraform and the deploy scripts are ready but untested (branch `infra/deploy-pipeline-fixes`).

## Milestone 1: Secure, tested API

Every operation is authenticated and limited to the caller's family. This has to come first: once anything is deployed, the current API lets anyone read or delete any family's tasks.

- [x] Integration test project (real SQL Server via Testcontainers, test authentication scheme), run by CI's `dotnet test`
- [x] All queries and mutations require authentication; remove `getAllTasks` and `GET /api/tasks`
- [x] Mutations take the family and creator from the caller, not from input (`createTask` no longer accepts `familyId` / `createdById`)
- [x] Assigning, completing and deleting check that the task and assignee belong to the caller's family
- [x] Role rules: Guardians create, edit, delete and assign tasks. Children see the family's tasks and complete their own assignments.
- [x] Remove `createFamily` and `createUser`; registration replaces them

**Done when:** tests prove that a user from one family can't read or change another family's data, and that a Child can't create or delete tasks.

## Milestone 2: Sign up and join a family

PRD flow 1: create an account, create a family, add members.

- [ ] Fix the External ID authority (`ciamlogin.com`) in the app and API; confirm `email`, `given_name` and `family_name` claims reach the API
- [ ] Mobile GraphQL client sends the MSAL access token; regenerate `schema.graphql` from the running API
- [ ] After sign-in, route by registration state: unregistered users see the welcome screen, registered users see the main tabs
- [x] Guardians create invite codes (single-use, expire after 7 days, role chosen by the Guardian); `registerNewUser` joins by code instead of family ID
- [x] Remove the unused `UserService.GetOrCreateUserAsync` (it creates users with no family, which violates the foreign key)
- [x] API: Guardians edit a member's name and role and remove members; a family always keeps at least one Guardian
- [ ] Family tab: list members; Guardians edit and remove members and create invites (show the code to share)
- [ ] Welcome screen: create a family, or join with an invite code
- [ ] Mobile build works locally (MAUI workload for the .NET 9 SDK) for iOS and Android

**Done when:** a parent signs up, creates a family and invites a co-parent and a child, and both join from their own phones.

## Milestone 3: Tasks end to end

PRD flows 2, 3 and 5.

- [x] API: `updateTask`, `unassignTask`, `reopenTask`, and `createTask` with `assigneeIds`; `myTasks` filtering and sorting (by member, status, due date) tested to stay within the family
- [ ] Task list filtered by member, date and status (today, this week, overdue, completed)
- [ ] Create and edit task screen: title, description, due date, assign one or more members
- [ ] Children's view: "My tasks", mark complete
- [ ] Guardians' view: the whole family's tasks with completion status

**Done when:** a Guardian creates and assigns a task, the child completes it on their phone, and the Guardian sees it marked done.

## Milestone 4: Reminders

PRD: notifications for upcoming or overdue tasks.

- [ ] Local notifications on each device for the user's own assigned tasks (due soon, overdue), rescheduled whenever tasks are loaded
- [ ] Notification permission prompt and a setting to turn reminders off

PRD flow 4 says parents "receive a notification or can view updates". The Guardians' view in Milestone 3 covers this for the MVP. Server push notifications when a child completes a task come after launch.

**Done when:** a child gets a reminder before a task is due without opening the app.

## Milestone 5: Beta and store launch

- [ ] Deploy staging (bootstrap remote state, then `deploy-terraform.sh` and `deploy-app.sh`); the mobile API endpoint comes from configuration per build, not hard-coded `localhost`
- [ ] In-app account deletion (required by the App Store and Google Play), privacy policy and terms
- [ ] Crash and error reporting (Application Insights is already deployed for the API)
- [ ] TestFlight and Google Play internal testing with a handful of real families
- [ ] Feedback channel in the app (a link or form is enough)
- [ ] Production deploy, store listings, submission

**Done when:** the app is live in both stores and early-adopter families are using it.

## After launch

From the PRD's post-MVP list, roughly in order of value to the target families:

- Custody schedules: which household a child is in, and which tasks apply where
- Recurring tasks and calendar view
- Push notifications to Guardians when tasks are completed
- Points and rewards
- Task history and completion reports
- Children belonging to more than one family
- Subscription plans (free and paid tiers)
- Google and Apple sign-in
