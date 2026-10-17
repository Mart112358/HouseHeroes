import { useMutation, useQuery } from '@apollo/client/react';
import { useState } from 'react';

import { useAuth } from '@/auth/auth-provider';
import { CurrentUserDocument, RegisterNewUserDocument } from '@/graphql/generated/graphql';
import { Body, Button, Card, Heading, Message, Screen, TextField } from '@/ui/components';

// Shown after sign-in until the user belongs to a family
export default function WelcomeScreen() {
  const { signOut } = useAuth();
  const currentUser = useQuery(CurrentUserDocument);
  const [register, { loading }] = useMutation(RegisterNewUserDocument, {
    // Once registered, currentUser is non-null and the root layout shows the app
    refetchQueries: [CurrentUserDocument],
    awaitRefetchQueries: true,
  });
  const [familyName, setFamilyName] = useState('');
  const [inviteCode, setInviteCode] = useState('');
  const [message, setMessage] = useState<string | null>(null);

  if (currentUser.error) {
    return (
      <Screen title="Can't connect">
        <Body muted>HouseHeroes couldn&apos;t reach its server. Check your connection and try again.</Body>
        <Button label="Try again" onPress={() => currentUser.refetch()} />
        <Button label="Sign out" variant="secondary" onPress={signOut} />
      </Screen>
    );
  }

  const submit = async (input: { createNewFamily: boolean; familyName?: string; inviteCode?: string }) => {
    setMessage(null);
    try {
      const { data } = await register({ variables: { input } });
      if (!data?.registerNewUser.success) setMessage(data?.registerNewUser.message ?? 'Something went wrong.');
    } catch {
      setMessage("Couldn't reach HouseHeroes. Try again.");
    }
  };

  return (
    <Screen title="Welcome!">
      <Body muted>Start a family, or join one with the invite code a parent shared with you.</Body>

      <Card>
        <Heading>Start a family</Heading>
        <TextField label="Family name" placeholder="The Smiths" value={familyName} onChangeText={setFamilyName} autoCapitalize="words" />
        <Button
          label="Create family"
          busy={loading}
          disabled={!familyName.trim()}
          onPress={() => submit({ createNewFamily: true, familyName: familyName.trim() })}
        />
      </Card>

      <Card>
        <Heading>Join a family</Heading>
        <TextField
          label="Invite code"
          placeholder="K7MPX2QA"
          value={inviteCode}
          onChangeText={setInviteCode}
          autoCapitalize="characters"
          autoCorrect={false}
          maxLength={8}
        />
        <Button
          label="Join family"
          variant="secondary"
          busy={loading}
          disabled={!inviteCode.trim()}
          onPress={() => submit({ createNewFamily: false, inviteCode: inviteCode.trim() })}
        />
      </Card>

      {message ? <Message tone="danger">{message}</Message> : null}
      <Button label="Sign out" variant="secondary" onPress={signOut} />
    </Screen>
  );
}
