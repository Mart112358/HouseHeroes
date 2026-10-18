import { useMutation, useQuery } from '@apollo/client/react';
import Constants from 'expo-constants';
import { useState } from 'react';
import { Alert, Linking, StyleSheet, Switch, View } from 'react-native';

import { useAuth } from '@/auth/auth-provider';
import { useReminders } from '@/features/reminders-provider';
import { roleLabels } from '@/features/roles';
import { useViewer } from '@/features/use-viewer';
import { config } from '@/config';
import { DeleteMyAccountDocument, FamilyMembersDocument } from '@/graphql/generated/graphql';
import { Body, Button, Card, Heading, Message, Screen } from '@/ui/components';

export default function SettingsScreen() {
  const { signOut } = useAuth();
  const viewer = useViewer();
  const { data } = useQuery(FamilyMembersDocument);
  const reminders = useReminders();
  const [deleteMyAccount, deleting] = useMutation(DeleteMyAccountDocument);
  const [deleteError, setDeleteError] = useState<string | null>(null);

  const confirmDelete = () =>
    Alert.alert(
      'Delete your account?',
      'Your HouseHeroes account and your task assignments are deleted. Tasks you created stay with your family. This can\'t be undone.',
      [
        { text: 'Cancel', style: 'cancel' },
        {
          text: 'Delete account',
          style: 'destructive',
          onPress: async () => {
            setDeleteError(null);
            try {
              await deleteMyAccount();
              await signOut();
            } catch (e) {
              // e.g. "Make another member a parent before deleting your account."
              setDeleteError(e instanceof Error ? e.message : "Couldn't delete your account. Try again.");
            }
          },
        },
      ]
    );

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
      <Card>
        <View style={styles.row}>
          <Heading>Reminders</Heading>
          <Switch accessibilityLabel="Task reminders" value={reminders.enabled} onValueChange={reminders.setEnabled} />
        </View>
        <Body muted>A reminder on the afternoon a task of yours is due, and the next morning if it&apos;s still not done.</Body>
        {reminders.enabled && reminders.blocked ? (
          <>
            <Message tone="danger">Notifications are turned off for HouseHeroes in your phone&apos;s settings.</Message>
            <Button label="Open settings" variant="secondary" onPress={() => Linking.openSettings()} />
          </>
        ) : null}
      </Card>
      {config.feedbackUrl ? <Button label="Send feedback" variant="secondary" onPress={() => Linking.openURL(config.feedbackUrl!)} /> : null}
      <Button label="Sign out" variant="secondary" onPress={signOut} />
      <Button label="Delete account" variant="danger" busy={deleting.loading} onPress={confirmDelete} />
      {deleteError ? <Message tone="danger">{deleteError}</Message> : null}
      <Body muted>Version {Constants.expoConfig?.version}</Body>
    </Screen>
  );
}

const styles = StyleSheet.create({
  row: { flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between' },
});
