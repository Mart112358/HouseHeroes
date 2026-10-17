import * as SecureStore from 'expo-secure-store';
import { TokenResponse, type DiscoveryDocument, type TokenResponseConfig } from 'expo-auth-session';

const storageKey = 'househeroes.auth.tokens';

export type RefreshTokens = (refreshToken: string, discovery: DiscoveryDocument) => Promise<TokenResponse>;

// Keeps the signed-in user's tokens in the device's secure storage and hands out
// a valid access token, refreshing it when it is about to expire.
export class TokenStore {
  private tokens: TokenResponse | null = null;
  private refreshing: Promise<string | null> | null = null;

  // The sign-in service's endpoints, loaded once the app starts
  private discovery: DiscoveryDocument | null = null;

  constructor(private readonly refresh: RefreshTokens) {}

  setDiscovery(discovery: DiscoveryDocument | null): void {
    this.discovery = discovery;
  }

  async load(): Promise<boolean> {
    const stored = await SecureStore.getItemAsync(storageKey);
    this.tokens = stored ? new TokenResponse(JSON.parse(stored) as TokenResponseConfig) : null;
    return this.tokens !== null;
  }

  async save(tokens: TokenResponse): Promise<void> {
    this.tokens = tokens;
    await SecureStore.setItemAsync(storageKey, JSON.stringify(tokens.getRequestConfig()));
  }

  async clear(): Promise<void> {
    this.tokens = null;
    await SecureStore.deleteItemAsync(storageKey);
  }

  // Returns null when there is no session or it can't be refreshed.
  // Throws when a refresh is needed but the sign-in service hasn't been reached yet.
  async getAccessToken(): Promise<string | null> {
    if (!this.tokens) return null;
    if (!this.tokens.shouldRefresh()) return this.tokens.accessToken;

    // One refresh at a time, shared by every request that needs it
    this.refreshing ??= this.refreshTokens().finally(() => {
      this.refreshing = null;
    });
    return this.refreshing;
  }

  private async refreshTokens(): Promise<string | null> {
    const refreshToken = this.tokens?.refreshToken;
    if (!refreshToken) {
      await this.clear();
      return null;
    }
    if (!this.discovery) throw new Error('Sign-in service is not reachable.');

    try {
      const refreshed = await this.refresh(refreshToken, this.discovery);
      // Entra doesn't always return a new refresh token; keep the old one then
      refreshed.refreshToken ??= refreshToken;
      await this.save(refreshed);
      return refreshed.accessToken;
    } catch {
      await this.clear();
      return null;
    }
  }
}
