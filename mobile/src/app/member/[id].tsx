import { useMutation, useQuery } from '@apollo/client/react';
import { router, useLocalSearchParams } from 'expo-router';
import { useState } from 'react';
import { Alert } from 'react-native';

import { roleOptions } from '@/features/roles';
import { useViewer } from '@/features/use-viewer';
import {
  FamilyMembersDocument,
  MyTasksDocument,
  RemoveFamilyMemberDocument,
  UpdateFamilyMemberDocument,
  type UserRole,
} from '@/graphql/generated/graphql';
import { Body, Button, Heading, Loading, Message, Screen, Segmented, TextField } from '@/ui/components';

export default function MemberScreen() {
  const { id } = useLocalSearchParams<{ id: string }>();
  const viewer = useViewer();
  const { data, loading } = useQuery(FamilyMembersDocument);
  const member = data?.familyMembers.find((m) => m.id === id);

  if (loading && !member) return <Loading />;
  if (!member) {
    return (
      <Screen>
        <Body muted>This person is no longer in your family.</Body>
      </Screen>
    );
  }

  return <MemberForm key={member.id} member={member} isViewer={member.id === viewer.id} />;
}

function MemberForm({
  member,
  isViewer,
}: {
  member: { id: string; firstName: string; lastName: string; email: string; role: UserRole };
  isViewer: boolean;
}) {
  const [firstName, setFirstName] = useState(member.firstName);
  const [lastName, setLastName] = useState(member.lastName);
  const [role, setRole] = useState(member.role);
  const [error, setError] = useState<string | null>(null);
  const [update, updating] = useMutation(UpdateFamilyMemberDocument);
  const [remove, removing] = useMutation(RemoveFamilyMemberDocument, {
    refetchQueries: [FamilyMembersDocument, MyTasksDocument],
    awaitRefetchQueries: true,
  });

  const save = async () => {
    setError(null);
    try {
      await update({ variables: { input: { userId: member.id, firstName: firstName.trim(), lastName: lastName.trim(), role } } });
      router.back();
    } catch (e) {
      // e.g. "A family needs at least one guardian."
      setError(e instanceof Error ? e.message : "Couldn't save. Try again.");
    }
  };

  const confirmRemove = () =>
    Alert.alert(`Remove ${member.firstName}?`, 'They will lose access to your family. Tasks they created stay.', [
      { text: 'Cancel', style: 'cancel' },
      {
        text: 'Remove',
        style: 'destructive',
        onPress: async () => {
          try {
            await remove({ variables: { userId: member.id } });
            router.back();
          } catch {
            setError("Couldn't remove them. Try again.");
          }
        },
      },
    ]);

  return (
    <Screen>
      <Body muted>{member.email}</Body>
      <TextField label="First name" value={firstName} onChangeText={setFirstName} autoCapitalize="words" />
      <TextField label="Last name" value={lastName} onChangeText={setLastName} autoCapitalize="words" />
      <Heading>Role</Heading>
      <Segmented options={roleOptions} value={role} onChange={setRole} />
      <Body muted>Parents create and assign tasks and manage the family. Children complete the tasks assigned to them.</Body>
      {error ? <Message tone="danger">{error}</Message> : null}
      <Button label="Save" busy={updating.loading} disabled={!firstName.trim()} onPress={save} />
      {isViewer ? null : <Button label="Remove from family" variant="danger" busy={removing.loading} onPress={confirmRemove} />}
    </Screen>
  );
}
