import { maxReminders, planReminders } from '@/features/reminders';
import { child, guardian, task } from '../test-support/factories';

const now = new Date(2026, 9, 17, 10, 0); // Sat Oct 17, 10:00 local
const endOfDay = (day: number) => new Date(2026, 9, day, 23, 59).toISOString();

it("reminds about the viewer's own open tasks: due-day afternoon, then overdue the next morning", () => {
  const dishes = task({ id: 'dishes', title: 'Empty the dishwasher', dueDate: endOfDay(18), assignedTo: [child.id] });

  const reminders = planReminders([dishes], child, now);

  expect(reminders.map((r) => [r.identifier, r.title, r.date])).toEqual([
    ['task-dishes-due-soon', 'Due today', new Date(2026, 9, 18, 17, 0)],
    ['task-dishes-overdue', 'Overdue', new Date(2026, 9, 19, 9, 0)],
  ]);
  expect(reminders[0].body).toBe('Empty the dishwasher');
});

it('reminds an hour before tasks due earlier than the afternoon', () => {
  const morning = task({ dueDate: new Date(2026, 9, 18, 8, 30).toISOString(), assignedTo: [child.id] });

  expect(planReminders([morning], child, now)[0].date).toEqual(new Date(2026, 9, 18, 7, 30));
});

it("skips completed, undated and other people's tasks, and reminders already in the past", () => {
  const tasks = [
    task({ dueDate: endOfDay(18), assignedTo: [child.id], isCompleted: true }),
    task({ assignedTo: [child.id] }),
    task({ dueDate: endOfDay(18), assignedTo: [guardian.id] }),
  ];
  const dueToday = task({ id: 'today', dueDate: endOfDay(17), assignedTo: [child.id] });
  const dueLastWeek = task({ dueDate: endOfDay(10), assignedTo: [child.id] });

  const reminders = planReminders([...tasks, dueToday, dueLastWeek], child, now);

  expect(reminders.map((r) => r.identifier)).toEqual(['task-today-due-soon', 'task-today-overdue']);
});

it(`keeps only the soonest ${maxReminders} reminders`, () => {
  const many = Array.from({ length: 40 }, (_, i) => task({ dueDate: endOfDay(18 + i), assignedTo: [child.id] }));

  const reminders = planReminders(many, child, now);

  expect(reminders).toHaveLength(maxReminders);
  expect(reminders[0].date).toEqual(new Date(2026, 9, 18, 17, 0));
});
