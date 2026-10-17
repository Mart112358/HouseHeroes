const tenantId = '3875acce-7943-4033-9283-eee45a3a219d';
const apiClientId = '78c6176b-78f1-49c7-ba7c-a7eb8933cb3e';

export const config = {
  // iOS simulator reaches the host as localhost; the Android emulator uses 10.0.2.2
  graphqlUrl: process.env.EXPO_PUBLIC_GRAPHQL_URL ?? 'http://localhost:5528/graphql',
  auth: {
    // Entra External ID (househeroesapp tenant)
    issuer: `https://househeroesapp.ciamlogin.com/${tenantId}/v2.0`,
    clientId: '7cd7528e-e23c-4f29-9267-0e4f2af1ac9d',
    scopes: ['openid', 'profile', 'email', 'offline_access', `api://${apiClientId}/access_as_user`],
    redirectScheme: 'househeroes',
    redirectPath: 'auth',
  },
};
