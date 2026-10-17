import { useApolloClient, useMutation, useQuery } from '@apollo/client/react';
import { router, useLocalSearchParams } from 'expo-router';
import { useState } from 'react';
import { Alert } from 'react-native';

import { TaskForm, type TaskFormValues } from '@/features/task-form';
import {
  AssignTaskDocument,
  DeleteTaskDocument,
  MyTasksDocument,
  TaskByIdDocument,
  UnassignTaskDocument,
  UpdateTaskDocument,
} from '@/graphql/generated/graphql';
import { Body, Button, Loading, Screen } from '@/ui/components';

export default function EditTaskScreen() {
  const { id } = useLocalSearchParams<{ id: string }>();
  const client = useApolloClient();
  const { data, loading } = useQuery(TaskByIdDocument, { variables: { id } });
  const [updateTask] = useMutation(UpdateTaskDocument);
  const [assignTask] = useMutation(AssignTaskDocument);
  const [unassignTask] = useMutation(UnassignTaskDocument);
  const [deleteTask, deleting] = useMutation(DeleteTaskDocument);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const task = data?.taskById;
  if (loading && !task) return <Loading />;
  if (!task) {
    return (
      <Screen>
        <Body muted>This task no longer exists.</Body>
      </Screen>
    );
  }

  const currentAssignees = task.taskAssignments.map((assignment) => assignment.userId);

  const save = async ({ title, description, dueDate, assigneeIds }: TaskFormValues) => {
    setSaving(true);
    setError(null);
    try {
      await updateTask({
        variables: { input: { taskId: task.id, title, description: description.trim() || null, dueDate: dueDate?.toISOString() ?? null } },
      });
      const added = assigneeIds.filter((userId) => !currentAssignees.includes(userId));
      const removed = currentAssignees.filter((userId) => !assigneeIds.includes(userId));
      await Promise.all([
        ...added.map((userId) => assignTask({ variables: { input: { taskId: task.id, userId } } })),
        ...removed.map((userId) => unassignTask({ variables: { input: { taskId: task.id, userId } } })),
      ]);
      await client.refetchQueries({ include: [MyTasksDocument] });
      router.back();
    } catch {
      setError("Couldn't save the task. Try again.");
    } finally {
      setSaving(false);
    }
  };

  const confirmDelete = () =>
    Alert.alert('Delete task?', `"${task.title}" will be removed for everyone.`, [
      { text: 'Cancel', style: 'cancel' },
      {
        text: 'Delete',
        style: 'destructive',
        onPress: async () => {
          try {
            await deleteTask({ variables: { taskId: task.id }, refetchQueries: [MyTasksDocument], awaitRefetchQueries: true });
            router.back();
          } catch {
            setError("Couldn't delete the task. Try again.");
          }
        },
      },
    ]);

  return (
    <TaskForm
      initial={{
        title: task.title,
        description: task.description ?? '',
        dueDate: task.dueDate ? new Date(task.dueDate) : null,
        assigneeIds: currentAssignees,
      }}
      submitLabel="Save changes"
      submitting={saving}
      error={error}
      onSubmit={save}
      footer={<Button label="Delete task" variant="danger" busy={deleting.loading} onPress={confirmDelete} />}
    />
  );
}
