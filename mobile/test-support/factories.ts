import type { Task, Viewer } from '@/features/tasks';

export const guardian: Viewer = { id: 'user-guardian', role: 'GUARDIAN' };
export const child: Viewer = { id: 'user-child', role: 'CHILD' };

let next = 0;

export function task(overrides: Partial<Task> & { assignedTo?: string[] } = {}): Task {
  const { assignedTo = [], ...rest } = overrides;
  const id = rest.id ?? `task-${++next}`;
  return {
    __typename: 'Task',
    id,
    title: `Task ${id}`,
    description: null,
    dueDate: null,
    isCompleted: false,
    completedAt: null,
    taskAssignments: assignedTo.map((userId) => ({
      __typename: 'TaskAssignment',
      taskId: id,
      userId,
      user: { __typename: 'User', id: userId, firstName: userId === child.id ? 'Emma' : 'Sarah' },
    })),
    ...rest,
  } as Task;
}
