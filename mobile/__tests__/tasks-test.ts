import { canComplete, dueLabel, visibleTasks } from '@/features/tasks';
import { child, guardian, task } from '../test-support/factories';

describe('visibleTasks', () => {
  it('shows only the viewer\'s tasks in "mine" and everything in "family"', () => {
    const mine = task({ assignedTo: [child.id] });
    const theirs = task({ assignedTo: [guardian.id] });

    expect(visibleTasks([mine, theirs], { scope: 'mine', status: 'open', viewer: child })).toEqual([mine]);
    expect(visibleTasks([mine, theirs], { scope: 'family', status: 'open', viewer: child })).toHaveLength(2);
  });

  it('orders open tasks by due date, undated last, then by title', () => {
    const undated = task({ title: 'A undated' });
    const later = task({ title: 'Later', dueDate: '2026-11-05T12:00:00Z' });
    const sooner = task({ title: 'Sooner', dueDate: '2026-11-01T12:00:00Z' });
    const done = task({ isCompleted: true, completedAt: '2026-10-01T12:00:00Z' });

    const result = visibleTasks([undated, later, done, sooner], { scope: 'family', status: 'open', viewer: guardian });

    expect(result.map((t) => t.title)).toEqual(['Sooner', 'Later', 'A undated']);
  });

  it('orders done tasks by most recently completed', () => {
    const older = task({ isCompleted: true, completedAt: '2026-10-01T12:00:00Z' });
    const newer = task({ isCompleted: true, completedAt: '2026-10-03T12:00:00Z' });
    const open = task();

    expect(visibleTasks([older, open, newer], { scope: 'family', status: 'done', viewer: guardian })).toEqual([newer, older]);
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
