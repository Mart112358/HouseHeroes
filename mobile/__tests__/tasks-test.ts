import { canComplete, dueLabel, groupByDueDate, visibleTasks } from '@/features/tasks';
import { child, guardian, task } from '../test-support/factories';

describe('visibleTasks', () => {
  it("shows one member's tasks, or everyone's", () => {
    const childs = task({ assignedTo: [child.id] });
    const parents = task({ assignedTo: [guardian.id] });

    expect(visibleTasks([childs, parents], { owner: child.id, status: 'open' })).toEqual([childs]);
    expect(visibleTasks([childs, parents], { owner: guardian.id, status: 'open' })).toEqual([parents]);
    expect(visibleTasks([childs, parents], { owner: 'everyone', status: 'open' })).toHaveLength(2);
  });

  it('orders open tasks by due date, undated last, then by title', () => {
    const undated = task({ title: 'A undated' });
    const later = task({ title: 'Later', dueDate: '2026-11-05T12:00:00Z' });
    const sooner = task({ title: 'Sooner', dueDate: '2026-11-01T12:00:00Z' });
    const done = task({ isCompleted: true, completedAt: '2026-10-01T12:00:00Z' });

    const result = visibleTasks([undated, later, done, sooner], { owner: 'everyone', status: 'open' });

    expect(result.map((t) => t.title)).toEqual(['Sooner', 'Later', 'A undated']);
  });

  it('orders done tasks by most recently completed', () => {
    const older = task({ isCompleted: true, completedAt: '2026-10-01T12:00:00Z' });
    const newer = task({ isCompleted: true, completedAt: '2026-10-03T12:00:00Z' });
    const open = task();

    expect(visibleTasks([older, open, newer], { owner: 'everyone', status: 'done' })).toEqual([newer, older]);
  });
});

describe('groupByDueDate', () => {
  const now = new Date(2026, 9, 17, 10, 0); // Saturday Oct 17, 10:00 local
  const due = (day: number) => new Date(2026, 9, day, 23, 59).toISOString();

  it('groups tasks by day and week, keeping their order', () => {
    const tasks = [
      task({ title: 'Late', dueDate: new Date(2026, 9, 16, 18, 0).toISOString() }),
      task({ title: 'Today', dueDate: due(17) }),
      task({ title: 'Tomorrow', dueDate: due(18) }),
      task({ title: 'Wednesday', dueDate: due(21) }),
      task({ title: 'Friday', dueDate: due(23) }),
      task({ title: 'Next month', dueDate: due(40) }),
      task({ title: 'Someday' }),
    ];

    const sections = groupByDueDate(tasks, now).map((s) => [s.title, s.data.map((t) => t.title)]);

    expect(sections).toEqual([
      ['Overdue', ['Late']],
      ['Today', ['Today']],
      ['Tomorrow', ['Tomorrow']],
      ['This week', ['Wednesday', 'Friday']],
      ['Later', ['Next month']],
      ['No due date', ['Someday']],
    ]);
  });
});

describe('canComplete', () => {
  it('lets guardians complete any task and children only their own', () => {
    const childsTask = task({ assignedTo: [child.id] });
    const parentsTask = task({ assignedTo: [guardian.id] });

    expect(canComplete(parentsTask, guardian)).toBe(true);
    expect(canComplete(childsTask, child)).toBe(true);
    expect(canComplete(parentsTask, child)).toBe(false);
  });
});

describe('dueLabel', () => {
  const now = new Date(2026, 9, 17, 10, 0); // Oct 17, 10:00 local

  it('describes today, tomorrow and overdue tasks', () => {
    expect(dueLabel(new Date(2026, 9, 17, 23, 59).toISOString(), now)).toEqual({ text: 'Due today', overdue: false });
    expect(dueLabel(new Date(2026, 9, 18, 23, 59).toISOString(), now)).toEqual({ text: 'Due tomorrow', overdue: false });
    expect(dueLabel(new Date(2026, 9, 15, 23, 59).toISOString(), now)?.overdue).toBe(true);
    expect(dueLabel(null, now)).toBeNull();
  });
});
