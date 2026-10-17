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

## Checks

```bash
npm run typecheck
npm run lint
npm test
```
