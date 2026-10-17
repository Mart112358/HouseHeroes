import {
  AuthRequest,
  exchangeCodeAsync,
  makeRedirectUri,
  refreshAsync,
  useAutoDiscovery,
} from 'expo-auth-session';
import * as WebBrowser from 'expo-web-browser';
import { createContext, use, useCallback, useEffect, useMemo, useState, type PropsWithChildren } from 'react';

import { config } from '@/config';
import { TokenStore, type RefreshTokens } from './token-store';

WebBrowser.maybeCompleteAuthSession();

export type AuthStatus = 'loading' | 'signedOut' | 'signedIn';

type AuthContextValue = {
  status: AuthStatus;
  // Set when the last sign-in attempt failed
  error: string | null;
  signIn: () => Promise<void>;
  signOut: () => Promise<void>;
  getAccessToken: () => Promise<string | null>;
};

const AuthContext = createContext<AuthContextValue | null>(null);

const redirectUri = makeRedirectUri({ scheme: config.auth.redirectScheme, path: config.auth.redirectPath });

const refreshTokens: RefreshTokens = (refreshToken, discovery) =>
  refreshAsync({ clientId: config.auth.clientId, refreshToken, scopes: config.auth.scopes }, discovery);

export function AuthProvider({ children }: PropsWithChildren) {
  const discovery = useAutoDiscovery(config.auth.issuer);
  const [status, setStatus] = useState<AuthStatus>('loading');
  const [error, setError] = useState<string | null>(null);

  const [store] = useState(() => new TokenStore(refreshTokens));

  useEffect(() => {
    store.load().then((hasSession) => setStatus(hasSession ? 'signedIn' : 'signedOut'));
  }, [store]);

  useEffect(() => {
    store.setDiscovery(discovery);
  }, [store, discovery]);

  // Stable, so the API client can be created once
  const getAccessToken = useCallback(async () => {
    const token = await store.getAccessToken();
    // The session expired and couldn't be refreshed
    if (!token) setStatus('signedOut');
    return token;
  }, [store]);

  const value = useMemo<AuthContextValue>(
    () => ({
      status,
      error,
      getAccessToken,
      signIn: async () => {
        if (!discovery) {
          setError('Sign-in service is not reachable. Check your connection and try again.');
          return;
        }
        setError(null);

        const request = new AuthRequest({ clientId: config.auth.clientId, scopes: config.auth.scopes, redirectUri });
        const result = await request.promptAsync(discovery);
        if (result.type !== 'success') {
          if (result.type === 'error') setError(result.error?.message ?? 'Sign-in failed.');
          return;
        }

        try {
          const tokens = await exchangeCodeAsync(
            {
              clientId: config.auth.clientId,
              code: result.params.code,
              redirectUri,
              extraParams: { code_verifier: request.codeVerifier ?? '' },
            },
            discovery
          );
          await store.save(tokens);
          setStatus('signedIn');
        } catch (e) {
          setError(e instanceof Error ? e.message : 'Sign-in failed.');
        }
      },
      signOut: async () => {
        await store.clear();
        setStatus('signedOut');
        // End the browser session too, so the next sign-in can pick another account
        if (discovery?.endSessionEndpoint) {
          const logoutUrl = `${discovery.endSessionEndpoint}?post_logout_redirect_uri=${encodeURIComponent(redirectUri)}`;
          await WebBrowser.openAuthSessionAsync(logoutUrl, redirectUri).catch(() => undefined);
        }
      },
    }),
    [status, error, discovery, store, getAccessToken]
  );

  return <AuthContext value={value}>{children}</AuthContext>;
}

export function useAuth(): AuthContextValue {
  const context = use(AuthContext);
  if (!context) throw new Error('useAuth must be used within AuthProvider');
  return context;
}
