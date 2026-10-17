import { useQuery } from '@apollo/client/react';
import { router } from 'expo-router';
import { useState } from 'react';
import { FlatList, RefreshControl, StyleSheet, View } from 'react-native';

import { TaskRow } from '@/features/task-row';
import { isGuardian, visibleTasks, type TaskScope, type TaskStatus } from '@/features/tasks';
import { useViewer } from '@/features/use-viewer';
import { MyTasksDocument } from '@/graphql/generated/graphql';
import { Body, Button, Message, Screen, Segmented } from '@/ui/components';
import { spacing } from '@/ui/theme';

export default function TasksScreen() {
  const viewer = useViewer();
  const guardian = isGuardian(viewer);
  const { data, loading, error, refetch } = useQuery(MyTasksDocument);
  const [scope, setScope] = useState<TaskScope>(guardian ? 'family' : 'mine');
  const [status, setStatus] = useState<TaskStatus>('open');
  const [refreshing, setRefreshing] = useState(false);

  const refresh = async () => {
    setRefreshing(true);
    await refetch().catch(() => undefined);
    setRefreshing(false);
  };

  const tasks = visibleTasks(data?.myTasks ?? [], { scope, status, viewer });

  return (
    <Screen title="Tasks" scroll={false} action={guardian ? <Button label="New task" onPress={() => router.push('/task/new')} /> : null}>
      <View style={styles.filters}>
        <Segmented
          options={[
            { value: 'mine', label: 'Mine' },
            { value: 'family', label: 'Everyone' },
          ]}
          value={scope}
          onChange={setScope}
        />
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

      <FlatList
        data={tasks}
        keyExtractor={(task) => task.id}
        contentContainerStyle={styles.list}
        refreshControl={<RefreshControl refreshing={refreshing} onRefresh={refresh} />}
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
