import { useQuery } from '@apollo/client/react';
import Constants from 'expo-constants';

import { useAuth } from '@/auth/auth-provider';
import { roleLabels } from '@/features/roles';
import { useViewer } from '@/features/use-viewer';
import { FamilyMembersDocument } from '@/graphql/generated/graphql';
import { Body, Button, Card, Heading, Screen } from '@/ui/components';

export default function SettingsScreen() {
  const { signOut } = useAuth();
  const viewer = useViewer();
  const { data } = useQuery(FamilyMembersDocument);

  return (
    <Screen title="Settings">
      <Card>
        <Heading>
          {viewer.firstName} {viewer.lastName}
        </Heading>
        <Body muted>{viewer.email}</Body>
        <Body>
          {roleLabels[viewer.role]}
          {data?.myFamily ? ` in ${data.myFamily.name}` : ''}
        </Body>
      </Card>
      <Button label="Sign out" variant="secondary" onPress={signOut} />
      <Body muted>Version {Constants.expoConfig?.version}</Body>
    </Screen>
  );
}
