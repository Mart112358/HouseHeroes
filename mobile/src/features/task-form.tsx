import DateTimePicker from '@react-native-community/datetimepicker';
import { useQuery } from '@apollo/client/react';
import { useState, type ReactNode } from 'react';
import { Platform, Pressable, StyleSheet, Switch, Text, View } from 'react-native';

import { FamilyMembersDocument } from '@/graphql/generated/graphql';
import { Body, Button, Card, Heading, Message, Screen, TextField } from '@/ui/components';
import { spacing, useTheme } from '@/ui/theme';

export type TaskFormValues = {
  title: string;
  description: string;
  dueDate: Date | null;
  assigneeIds: string[];
};

type TaskFormProps = {
  initial: TaskFormValues;
  submitLabel: string;
  submitting: boolean;
  error: string | null;
  onSubmit: (values: TaskFormValues) => void;
  footer?: ReactNode;
};

export function TaskForm({ initial, submitLabel, submitting, error, onSubmit, footer }: TaskFormProps) {
  const theme = useTheme();
  const members = useQuery(FamilyMembersDocument);
  const [values, setValues] = useState(initial);
  const [showAndroidPicker, setShowAndroidPicker] = useState(false);
  const update = (change: Partial<TaskFormValues>) => setValues((current) => ({ ...current, ...change }));

  const toggleAssignee = (id: string) =>
    update({
      assigneeIds: values.assigneeIds.includes(id) ? values.assigneeIds.filter((a) => a !== id) : [...values.assigneeIds, id],
    });

  const setDueDate = (date: Date) => update({ dueDate: endOfDay(date) });

  return (
    <Screen>
      <TextField label="What needs doing?" placeholder="Empty the dishwasher" value={values.title} onChangeText={(title) => update({ title })} />
      <TextField
        label="Details (optional)"
        placeholder="Before dinner"
        value={values.description}
        onChangeText={(description) => update({ description })}
        multiline
      />

      <Card>
        <View style={styles.row}>
          <Heading>Due date</Heading>
          <Switch
            accessibilityLabel="Set a due date"
            value={values.dueDate !== null}
            onValueChange={(on) => {
              update({ dueDate: on ? endOfDay(new Date()) : null });
              if (on && Platform.OS === 'android') setShowAndroidPicker(true);
            }}
          />
        </View>
        {values.dueDate && Platform.OS === 'ios' ? (
          <DateTimePicker value={values.dueDate} mode="date" display="compact" onValueChange={(_, date) => setDueDate(date)} />
        ) : null}
        {values.dueDate && Platform.OS !== 'ios' ? (
          <Button label={values.dueDate.toLocaleDateString()} variant="secondary" onPress={() => setShowAndroidPicker(true)} />
        ) : null}
        {showAndroidPicker && values.dueDate ? (
          <DateTimePicker
            value={values.dueDate}
            mode="date"
            onValueChange={(_, date) => {
              setShowAndroidPicker(false);
              setDueDate(date);
            }}
            onDismiss={() => setShowAndroidPicker(false)}
          />
        ) : null}
      </Card>

      <Card>
        <Heading>Who&apos;s doing it?</Heading>
        {members.data?.familyMembers.map((member) => {
          const selected = values.assigneeIds.includes(member.id);
          return (
            <Pressable
              key={member.id}
              accessibilityRole="checkbox"
              accessibilityState={{ checked: selected }}
              onPress={() => toggleAssignee(member.id)}
              style={[styles.member, { borderColor: selected ? theme.primary : theme.border }]}>
              <Text style={[styles.memberName, { color: theme.text }]}>
                {member.firstName} {member.lastName}
              </Text>
              <Text style={{ color: selected ? theme.primary : theme.textMuted }}>{selected ? 'Assigned' : 'Tap to assign'}</Text>
            </Pressable>
          );
        }) ?? <Body muted>Loading family…</Body>}
      </Card>

      {error ? <Message tone="danger">{error}</Message> : null}
      <Button label={submitLabel} busy={submitting} disabled={!values.title.trim()} onPress={() => onSubmit({ ...values, title: values.title.trim() })} />
      {footer}
    </Screen>
  );
}

// Due dates are whole days: a task is overdue once its day has passed
function endOfDay(date: Date): Date {
  return new Date(date.getFullYear(), date.getMonth(), date.getDate(), 23, 59, 0);
}

const styles = StyleSheet.create({
  row: { flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between' },
  member: { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center', borderWidth: 1, borderRadius: 12, padding: spacing.md, minHeight: 48 },
  memberName: { fontSize: 16, fontWeight: '500' },
});
