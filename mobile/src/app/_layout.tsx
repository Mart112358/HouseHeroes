import { ApolloProvider, useQuery } from '@apollo/client/react';
import { Stack } from 'expo-router';
import * as SplashScreen from 'expo-splash-screen';
import { useEffect, useMemo, type PropsWithChildren } from 'react';

import { AuthProvider, useAuth } from '@/auth/auth-provider';
import { createApolloClient } from '@/graphql/client';
import { CurrentUserDocument } from '@/graphql/generated/graphql';

SplashScreen.preventAutoHideAsync();

export default function RootLayout() {
  return (
    <AuthProvider>
      <ApiProvider>
        <RootNavigator />
      </ApiProvider>
    </AuthProvider>
  );
}

function ApiProvider({ children }: PropsWithChildren) {
  const { status, getAccessToken } = useAuth();
  const client = useMemo(() => createApolloClient(getAccessToken), [getAccessToken]);

  // Don't show one user's cached family data to the next
  useEffect(() => {
    if (status === 'signedOut') client.clearStore();
  }, [status, client]);

  return <ApolloProvider client={client}>{children}</ApolloProvider>;
}

// Signed out → sign-in; signed in but not in a family yet → welcome; otherwise the app.
function RootNavigator() {
  const { status } = useAuth();
  const signedIn = status === 'signedIn';
  const { data, loading } = useQuery(CurrentUserDocument, { skip: !signedIn });
  const registered = signedIn && !!data?.currentUser;
  const resolving = status === 'loading' || (signedIn && loading && !data);

  useEffect(() => {
    if (!resolving) SplashScreen.hide();
  }, [resolving]);

  return (
    <Stack screenOptions={{ headerShown: false }}>
      <Stack.Protected guard={!signedIn}>
        <Stack.Screen name="sign-in" />
      </Stack.Protected>
      <Stack.Protected guard={signedIn && !registered}>
        <Stack.Screen name="welcome" />
      </Stack.Protected>
      <Stack.Protected guard={registered}>
        <Stack.Screen name="(tabs)" />
        <Stack.Screen name="task/new" options={{ presentation: 'modal', headerShown: true, title: 'New task' }} />
        <Stack.Screen name="task/[id]" options={{ headerShown: true, title: 'Edit task' }} />
        <Stack.Screen name="member/[id]" options={{ headerShown: true, title: 'Family member' }} />
      </Stack.Protected>
    </Stack>
  );
}
