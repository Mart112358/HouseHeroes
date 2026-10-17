import { useMutation, useQuery } from '@apollo/client/react';
import { router } from 'expo-router';
import { useState } from 'react';
import { Pressable, Share, StyleSheet, Text } from 'react-native';

import { roleLabels, roleOptions } from '@/features/roles';
import { isGuardian } from '@/features/tasks';
import { useViewer } from '@/features/use-viewer';
import { CreateFamilyInviteDocument, FamilyMembersDocument, type UserRole } from '@/graphql/generated/graphql';
import { Body, Button, Card, Heading, Message, Screen, Segmented } from '@/ui/components';
import { spacing, useTheme } from '@/ui/theme';

export default function FamilyScreen() {
  const theme = useTheme();
  const viewer = useViewer();
  const guardian = isGuardian(viewer);
  const { data, error } = useQuery(FamilyMembersDocument);

  return (
    <Screen title={data?.myFamily?.name ?? 'Family'}>
      {error && !data ? <Message tone="danger">Couldn&apos;t load your family.</Message> : null}

      <Card>
        <Heading>Members</Heading>
        {data?.familyMembers.map((member) => (
          <Pressable
            key={member.id}
            disabled={!guardian}
            accessibilityRole={guardian ? 'button' : undefined}
            onPress={() => router.push(`/member/${member.id}`)}
            style={[styles.member, { borderColor: theme.border }]}>
            <Text style={[styles.name, { color: theme.text }]}>
              {member.firstName} {member.lastName}
              {member.id === viewer.id ? ' (you)' : ''}
            </Text>
            <Text style={{ color: theme.textMuted }}>{roleLabels[member.role]}</Text>
          </Pressable>
        ))}
      </Card>

      {guardian ? <InviteCard /> : null}
    </Screen>
  );
}

function InviteCard() {
  const theme = useTheme();
  const [role, setRole] = useState<UserRole>('CHILD');
  const [createInvite, { data, loading, error, reset }] = useMutation(CreateFamilyInviteDocument);
  const invite = data?.createFamilyInvite;

  const share = (code: string) =>
    Share.share({ message: `Join our family on HouseHeroes! Sign up in the app and enter this invite code: ${code}` });

  return (
    <Card>
      <Heading>Invite someone</Heading>
      <Body muted>They&apos;ll sign up in the app and enter the code. Each code works once and expires after 7 days.</Body>
      <Segmented
        options={roleOptions}
        value={role}
        onChange={(value) => {
          setRole(value);
          reset();
        }}
      />
      {invite ? (
        <>
          <Text selectable style={[styles.code, { color: theme.text }]} accessibilityLabel={`Invite code ${invite.code.split('').join(' ')}`}>
            {invite.code}
          </Text>
          <Body muted>
            Joins as {roleLabels[invite.role].toLowerCase()} · expires {new Date(invite.expiresAt).toLocaleDateString()}
          </Body>
          <Button label="Share code" onPress={() => share(invite.code)} />
          <Button label="Create another code" variant="secondary" busy={loading} onPress={() => createInvite({ variables: { role } })} />
        </>
      ) : (
        <Button label={`Create ${roleLabels[role].toLowerCase()} invite`} busy={loading} onPress={() => createInvite({ variables: { role } }).catch(() => undefined)} />
      )}
      {error ? <Message tone="danger">Couldn&apos;t create an invite. Try again.</Message> : null}
    </Card>
  );
}

const styles = StyleSheet.create({
  member: { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center', paddingVertical: spacing.md, borderTopWidth: StyleSheet.hairlineWidth, minHeight: 48 },
  name: { fontSize: 16, fontWeight: '500', flexShrink: 1 },
  code: { fontSize: 34, fontWeight: '700', letterSpacing: 6, textAlign: 'center', fontVariant: ['tabular-nums'] },
});
