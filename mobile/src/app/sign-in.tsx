import { useState } from 'react';

import { useAuth } from '@/auth/auth-provider';
import { Body, Button, Card, Heading, Message, Screen } from '@/ui/components';

export default function SignInScreen() {
  const { signIn, error } = useAuth();
  const [busy, setBusy] = useState(false);

  const onSignIn = async () => {
    setBusy(true);
    try {
      await signIn();
    } finally {
      setBusy(false);
    }
  };

  return (
    <Screen title="HouseHeroes">
      <Body muted>Share chores across your family, even across two homes.</Body>
      <Card>
        <Heading>Welcome</Heading>
        <Body>Sign in, or create an account with your email address.</Body>
        <Button label="Sign in or create account" onPress={onSignIn} busy={busy} />
        {error ? <Message tone="danger">{error}</Message> : null}
      </Card>
    </Screen>
  );
}
