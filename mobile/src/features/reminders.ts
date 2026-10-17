import { isAssignedTo, type Task, type Viewer } from './tasks';

export type Reminder = {
  identifier: string;
  taskId: string;
  title: string;
  body: string;
  date: Date;
};

// iOS keeps at most 64 pending local notifications per app
export const maxReminders = 60;

const dueSoonHour = 17;
const overdueHour = 9;

// Reminders for the viewer's own open tasks that have a due date:
// "Due today" at 5 pm on the due day (or an hour before, if it's due earlier),
// and one "Overdue" nudge at 9 am the day after. Soonest first.
export function planReminders(tasks: readonly Task[], viewer: Viewer, now: Date = new Date()): Reminder[] {
  const reminders = tasks
    .filter((task) => !task.isCompleted && task.dueDate && isAssignedTo(task, viewer.id))
    .flatMap((task) => {
      const due = new Date(task.dueDate!);
      const dueSoonAt = new Date(Math.min(atHour(due, 0, dueSoonHour).getTime(), due.getTime() - 3_600_000));
      const overdueAt = atHour(due, 1, overdueHour);

      return [
        {
          identifier: `task-${task.id}-due-soon`,
          taskId: task.id,
          title: 'Due today',
          body: task.title,
          date: dueSoonAt,
        },
        {
          identifier: `task-${task.id}-overdue`,
          taskId: task.id,
          title: 'Overdue',
          body: `${task.title} was due ${due.toLocaleDateString(undefined, { weekday: 'long' })}.`,
          date: overdueAt,
        },
      ];
    })
    .filter((reminder) => reminder.date.getTime() > now.getTime())
    .sort((a, b) => a.date.getTime() - b.date.getTime());

  return reminders.slice(0, maxReminders);
}

// The given hour, local time, `dayOffset` days after `date`'s day
function atHour(date: Date, dayOffset: number, hour: number): Date {
  return new Date(date.getFullYear(), date.getMonth(), date.getDate() + dayOffset, hour, 0, 0);
}
