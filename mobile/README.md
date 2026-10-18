# HouseHeroes mobile app

React Native app built with [Expo](https://docs.expo.dev) (SDK 58) and Expo Router.

## Run it

Start the backend first (`dotnet run --project src/HouseHeroes.AppHost` from the repo root), then:

```bash
npm install
npm run ios       # builds a development build and opens it in the iOS simulator
npm run android   # Android emulator; set EXPO_PUBLIC_GRAPHQL_URL=http://10.0.2.2:5528/graphql
```

Sign-in uses Entra External ID, so it doesn't work in Expo Go: it needs the `househeroes://auth` redirect URI, which only a development build registers.

## Change the API calls

Operations live in `src/graphql/*.graphql`. After editing them, or after the API schema changes:

```bash
# from the repo root: rewrite mobile/schema.graphql from the API
UPDATE_SCHEMA=1 dotnet test tests/HouseHeroes.ApiService.Tests --filter SchemaTests
# from mobile/: regenerate the typed documents in src/graphql/generated
npm run codegen
```

## Release builds (EAS)

`eas.json` has three build profiles. `preview` makes internal-distribution builds for testers against staging, `production` makes store builds. Each profile reads its variables from the matching EAS environment. A release build refuses to start without `EXPO_PUBLIC_GRAPHQL_URL`.

One-time setup (needs an Expo account):

```bash
npx eas-cli@latest login
npx eas-cli@latest init        # links the project and adds its ID to app.json
npx eas-cli@latest env:set --name EXPO_PUBLIC_GRAPHQL_URL --value https://<staging api host>/graphql --environment preview --visibility plaintext
npx eas-cli@latest env:set --name EXPO_PUBLIC_GRAPHQL_URL --value https://<production api host>/graphql --environment production --visibility plaintext
# optional: a feedback form or mailto: link shown in Settings
npx eas-cli@latest env:set --name EXPO_PUBLIC_FEEDBACK_URL --value <url> --environment preview --visibility plaintext
```

The API host is the Container App URL (`terraform output container_app_url` in `infra/`).

```bash
npx eas-cli@latest build --profile preview --platform all      # builds for testers
npx eas-cli@latest build --profile production --platform all   # store builds
npx eas-cli@latest submit --profile production --platform ios  # TestFlight / App Store Connect
```

## Checks

```bash
npm run typecheck
npm run lint
npm test
```
