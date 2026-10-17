import type { TaskFieldsFragment, UserRole } from '@/graphql/generated/graphql';

export type Task = TaskFieldsFragment;
export type Viewer = { id: string; role: UserRole };
export type TaskScope = 'mine' | 'family';
export type TaskStatus = 'open' | 'done';

export const isGuardian = (viewer: Viewer) => viewer.role === 'GUARDIAN';

export function isAssignedTo(task: Task, userId: string): boolean {
  return task.taskAssignments.some((assignment) => assignment.userId === userId);
}

// Mirrors the API rule: Guardians complete any family task, Children only their own
export function canComplete(task: Task, viewer: Viewer): boolean {
  return isGuardian(viewer) || isAssignedTo(task, viewer.id);
}

// Open tasks soonest first (undated last); done tasks most recently completed first
export function visibleTasks(tasks: readonly Task[], { scope, status, viewer }: { scope: TaskScope; status: TaskStatus; viewer: Viewer }): Task[] {
  const inScope = tasks.filter((task) => scope === 'family' || isAssignedTo(task, viewer.id));

  if (status === 'done') {
    return inScope
      .filter((task) => task.isCompleted)
      .sort((a, b) => timeOf(b.completedAt) - timeOf(a.completedAt));
  }

  return inScope
    .filter((task) => !task.isCompleted)
    .sort((a, b) => (timeOf(a.dueDate, Infinity) - timeOf(b.dueDate, Infinity)) || a.title.localeCompare(b.title));
}

export function dueLabel(dueDate: string | null | undefined, now: Date = new Date()): { text: string; overdue: boolean } | null {
  if (!dueDate) return null;

  const due = new Date(dueDate);
  const days = Math.round((startOfDay(due) - startOfDay(now)) / 86_400_000);
  const date = due.toLocaleDateString(undefined, { weekday: 'short', month: 'short', day: 'numeric' });

  if (due.getTime() < now.getTime()) return { text: days === 0 ? 'Due earlier today' : `Overdue · ${date}`, overdue: true };
  if (days === 0) return { text: 'Due today', overdue: false };
  if (days === 1) return { text: 'Due tomorrow', overdue: false };
  return { text: `Due ${date}`, overdue: false };
}

export function assigneeNames(task: Task): string {
  return task.taskAssignments.map((assignment) => assignment.user.firstName).join(', ');
}

function timeOf(value: string | null | undefined, fallback = 0): number {
  return value ? new Date(value).getTime() : fallback;
}

function startOfDay(date: Date): number {
  return new Date(date.getFullYear(), date.getMonth(), date.getDate()).getTime();
}
