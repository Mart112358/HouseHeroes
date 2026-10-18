import { useQuery } from '@apollo/client/react';
import { router } from 'expo-router';
import { useState } from 'react';
import { RefreshControl, SectionList, StyleSheet, View } from 'react-native';

import { TaskRow } from '@/features/task-row';
import { groupByDueDate, isGuardian, visibleTasks, type TaskOwner, type TaskStatus } from '@/features/tasks';
import { useViewer } from '@/features/use-viewer';
import { FamilyMembersDocument, MyTasksDocument } from '@/graphql/generated/graphql';
import { Body, Button, Chips, Heading, Message, Screen, Segmented } from '@/ui/components';
import { spacing } from '@/ui/theme';

export default function TasksScreen() {
  const viewer = useViewer();
  const guardian = isGuardian(viewer);
  const { data, loading, error, refetch } = useQuery(MyTasksDocument);
  const members = useQuery(FamilyMembersDocument);
  const [owner, setOwner] = useState<TaskOwner>(guardian ? 'everyone' : viewer.id);
  const [status, setStatus] = useState<TaskStatus>('open');
  const [refreshing, setRefreshing] = useState(false);

  const refresh = async () => {
    setRefreshing(true);
    await Promise.all([refetch(), members.refetch()]).catch(() => undefined);
    setRefreshing(false);
  };

  const owners = [
    { value: 'everyone', label: 'Everyone' },
    { value: viewer.id, label: 'Me' },
    ...(members.data?.familyMembers ?? [])
      .filter((member) => member.id !== viewer.id)
      .map((member) => ({ value: member.id, label: member.firstName })),
  ];

  const tasks = visibleTasks(data?.myTasks ?? [], { owner, status });
  const sections = status === 'open' ? groupByDueDate(tasks) : tasks.length ? [{ title: '', data: tasks }] : [];

  return (
    <Screen title="Tasks" scroll={false} action={guardian ? <Button label="New task" onPress={() => router.push('/task/new')} /> : null}>
      <View style={styles.filters}>
        <Chips options={owners} value={owner} onChange={setOwner} />
        <Segmented
          options={[
            { value: 'open', label: 'To do' },
            { value: 'done', label: 'Done' },
          ]}
          value={status}
          onChange={setStatus}
        />
      </View>

      {error && !data ? <Message tone="danger">Couldn&apos;t load tasks. Pull down to try again.</Message> : null}

      <SectionList
        sections={sections}
        keyExtractor={(task) => task.id}
        contentContainerStyle={styles.list}
        stickySectionHeadersEnabled={false}
        refreshControl={<RefreshControl refreshing={refreshing} onRefresh={refresh} />}
        renderSectionHeader={({ section }) => (section.title ? <Heading>{section.title}</Heading> : null)}
        renderItem={({ item }) => (
          <TaskRow task={item} viewer={viewer} onOpen={guardian ? () => router.push(`/task/${item.id}`) : undefined} />
        )}
        ListEmptyComponent={
          loading ? undefined : (
            <Body muted>
              {status === 'done' ? 'Nothing finished yet.' : guardian ? 'No tasks to do. Add one with "New task".' : 'Nothing to do right now.'}
            </Body>
          )
        }
      />
    </Screen>
  );
}

const styles = StyleSheet.create({
  filters: { gap: spacing.sm },
  list: { gap: spacing.sm, paddingBottom: spacing.xl },
});
