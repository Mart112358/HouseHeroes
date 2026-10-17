import { useMutation } from '@apollo/client/react';
import { Pressable, StyleSheet, Text, View } from 'react-native';

import { CompleteTaskDocument, ReopenTaskDocument } from '@/graphql/generated/graphql';
import { spacing, useTheme } from '@/ui/theme';
import { assigneeNames, canComplete, dueLabel, type Task, type Viewer } from './tasks';

export function TaskRow({ task, viewer, onOpen }: { task: Task; viewer: Viewer; onOpen?: () => void }) {
  const theme = useTheme();
  const [complete, completing] = useMutation(CompleteTaskDocument);
  const [reopen, reopening] = useMutation(ReopenTaskDocument);
  const allowed = canComplete(task, viewer);
  const due = dueLabel(task.dueDate);
  const names = assigneeNames(task);

  const toggle = () => {
    const variables = { taskId: task.id };
    (task.isCompleted ? reopen({ variables }) : complete({ variables })).catch(() => undefined);
  };

  return (
    <View style={[styles.row, { backgroundColor: theme.surface, borderColor: theme.border }]}>
      <Pressable
        accessibilityRole="checkbox"
        accessibilityLabel={task.isCompleted ? `Mark "${task.title}" as not done` : `Mark "${task.title}" as done`}
        accessibilityState={{ checked: task.isCompleted, disabled: !allowed }}
        disabled={!allowed || completing.loading || reopening.loading}
        onPress={toggle}
        hitSlop={8}
        style={[
          styles.check,
          { borderColor: allowed ? theme.primary : theme.border },
          task.isCompleted && { backgroundColor: theme.success, borderColor: theme.success },
        ]}>
        {task.isCompleted ? <Text style={[styles.checkMark, { color: theme.onPrimary }]}>✓</Text> : null}
      </Pressable>

      <Pressable style={styles.body} onPress={onOpen} disabled={!onOpen} accessibilityRole={onOpen ? 'button' : undefined}>
        <Text style={[styles.title, { color: theme.text }, task.isCompleted && styles.done]}>{task.title}</Text>
        {due && !task.isCompleted ? <Text style={[styles.meta, { color: due.overdue ? theme.danger : theme.textMuted }]}>{due.text}</Text> : null}
        <Text style={[styles.meta, { color: theme.textMuted }]}>{names ? `For ${names}` : 'Not assigned yet'}</Text>
      </Pressable>
    </View>
  );
}

const styles = StyleSheet.create({
  row: { flexDirection: 'row', gap: spacing.md, padding: spacing.md, borderRadius: 14, borderWidth: StyleSheet.hairlineWidth, alignItems: 'flex-start' },
  check: { width: 28, height: 28, borderRadius: 14, borderWidth: 2, alignItems: 'center', justifyContent: 'center', marginTop: 2 },
  checkMark: { fontSize: 16, fontWeight: '700' },
  body: { flex: 1, gap: 2 },
  title: { fontSize: 17, fontWeight: '600' },
  done: { textDecorationLine: 'line-through', opacity: 0.6 },
  meta: { fontSize: 14 },
});
