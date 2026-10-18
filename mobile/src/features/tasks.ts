import type { TaskFieldsFragment, UserRole } from '@/graphql/generated/graphql';

export type Task = TaskFieldsFragment;
export type Viewer = { id: string; role: UserRole };
// Whose tasks to show: a member's ID, or everyone in the family
export type TaskOwner = string | 'everyone';
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
export function visibleTasks(tasks: readonly Task[], { owner, status }: { owner: TaskOwner; status: TaskStatus }): Task[] {
  const inScope = tasks.filter((task) => owner === 'everyone' || isAssignedTo(task, owner));

  if (status === 'done') {
    return inScope
      .filter((task) => task.isCompleted)
      .sort((a, b) => timeOf(b.completedAt) - timeOf(a.completedAt));
  }

  return inScope
    .filter((task) => !task.isCompleted)
    .sort((a, b) => (timeOf(a.dueDate, Infinity) - timeOf(b.dueDate, Infinity)) || a.title.localeCompare(b.title));
}

export type TaskSection = { title: string; data: Task[] };

// Splits sorted open tasks into Overdue, Today, Tomorrow, This week, Later and No due date, keeping their order
export function groupByDueDate(tasks: readonly Task[], now: Date = new Date()): TaskSection[] {
  const sections: TaskSection[] = [];
  for (const task of tasks) {
    const title = dueGroup(task.dueDate, now);
    const section = sections.find((s) => s.title === title);
    if (section) section.data.push(task);
    else sections.push({ title, data: [task] });
  }
  return sections;
}

function dueGroup(dueDate: string | null | undefined, now: Date): string {
  if (!dueDate) return 'No due date';

  const due = new Date(dueDate);
  if (due.getTime() < now.getTime()) return 'Overdue';

  const days = Math.round((startOfDay(due) - startOfDay(now)) / 86_400_000);
  if (days === 0) return 'Today';
  if (days === 1) return 'Tomorrow';
  if (days < 7) return 'This week';
  return 'Later';
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
