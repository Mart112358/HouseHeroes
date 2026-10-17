import { TokenResponse, type DiscoveryDocument } from 'expo-auth-session';

import { TokenStore } from '@/auth/token-store';

const mockStorage = new Map<string, string>();
jest.mock('expo-secure-store', () => ({
  getItemAsync: jest.fn(async (key: string) => mockStorage.get(key) ?? null),
  setItemAsync: jest.fn(async (key: string, value: string) => void mockStorage.set(key, value)),
  deleteItemAsync: jest.fn(async (key: string) => void mockStorage.delete(key)),
}));

const discovery = { tokenEndpoint: 'https://login.example/token' } as DiscoveryDocument;
const nowSeconds = () => Math.floor(Date.now() / 1000);

const freshTokens = (accessToken: string, refreshToken?: string) =>
  new TokenResponse({ accessToken, refreshToken, expiresIn: 3600, issuedAt: nowSeconds() });
const expiredTokens = () =>
  new TokenResponse({ accessToken: 'expired', refreshToken: 'refresh-1', expiresIn: 3600, issuedAt: nowSeconds() - 7200 });

beforeEach(() => mockStorage.clear());

it('has no token without a session', async () => {
  const store = new TokenStore(jest.fn());
  expect(await store.load()).toBe(false);
  expect(await store.getAccessToken()).toBeNull();
});

it('restores a saved session', async () => {
  await new TokenStore(jest.fn()).save(freshTokens('access-1', 'refresh-1'));

  const restored = new TokenStore(jest.fn());
  expect(await restored.load()).toBe(true);
  expect(await restored.getAccessToken()).toBe('access-1');
});

it('refreshes an expired token once for concurrent requests and keeps the refresh token', async () => {
  const refresh = jest.fn(async () => freshTokens('access-2'));
  const store = new TokenStore(refresh);
  store.setDiscovery(discovery);
  await store.save(expiredTokens());

  const tokens = await Promise.all([store.getAccessToken(), store.getAccessToken()]);

  expect(tokens).toEqual(['access-2', 'access-2']);
  expect(refresh).toHaveBeenCalledTimes(1);
  expect(refresh).toHaveBeenCalledWith('refresh-1', discovery);
  const saved = JSON.parse([...mockStorage.values()][0]);
  expect(saved.refreshToken).toBe('refresh-1');
});

it('ends the session when refreshing fails', async () => {
  const store = new TokenStore(jest.fn(async () => Promise.reject(new Error('invalid_grant'))));
  store.setDiscovery(discovery);
  await store.save(expiredTokens());

  expect(await store.getAccessToken()).toBeNull();
  expect(mockStorage.size).toBe(0);
});

it('keeps the session but fails the request when the sign-in service is unreachable', async () => {
  const store = new TokenStore(jest.fn());
  await store.save(expiredTokens());

  await expect(store.getAccessToken()).rejects.toThrow('not reachable');
  expect(mockStorage.size).toBe(1);
});
