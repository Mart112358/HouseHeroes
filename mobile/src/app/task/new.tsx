import { useMutation } from '@apollo/client/react';
import { router } from 'expo-router';
import { useState } from 'react';

import { TaskForm } from '@/features/task-form';
import { CreateTaskDocument, MyTasksDocument } from '@/graphql/generated/graphql';

export default function NewTaskScreen() {
  const [createTask, { loading }] = useMutation(CreateTaskDocument, { refetchQueries: [MyTasksDocument] });
  const [error, setError] = useState<string | null>(null);

  return (
    <TaskForm
      initial={{ title: '', description: '', dueDate: null, assigneeIds: [] }}
      submitLabel="Add task"
      submitting={loading}
      error={error}
      onSubmit={async ({ title, description, dueDate, assigneeIds }) => {
        setError(null);
        try {
          await createTask({
            variables: { input: { title, description: description.trim() || null, dueDate: dueDate?.toISOString() ?? null, assigneeIds } },
          });
          router.back();
        } catch {
          setError("Couldn't save the task. Try again.");
        }
      }}
    />
  );
}
