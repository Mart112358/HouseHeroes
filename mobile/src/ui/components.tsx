import type { PropsWithChildren, ReactNode } from 'react';
import {
  ActivityIndicator,
  Pressable,
  ScrollView,
  StyleSheet,
  Text,
  TextInput,
  View,
  type TextInputProps,
} from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

import { spacing, useTheme } from './theme';

export function Screen({ title, action, children, scroll = true }: PropsWithChildren<{ title?: string; action?: ReactNode; scroll?: boolean }>) {
  const theme = useTheme();
  const content = (
    <>
      {title ? (
        <View style={styles.header}>
          <Text style={[styles.title, { color: theme.text }]} accessibilityRole="header">
            {title}
          </Text>
          {action}
        </View>
      ) : null}
      {children}
    </>
  );

  return (
    <SafeAreaView style={[styles.screen, { backgroundColor: theme.background }]} edges={['top', 'left', 'right']}>
      {scroll ? (
        <ScrollView contentContainerStyle={styles.content} keyboardShouldPersistTaps="handled">
          {content}
        </ScrollView>
      ) : (
        <View style={[styles.content, styles.fill]}>{content}</View>
      )}
    </SafeAreaView>
  );
}

export function Card({ children }: PropsWithChildren) {
  const theme = useTheme();
  return <View style={[styles.card, { backgroundColor: theme.surface, borderColor: theme.border }]}>{children}</View>;
}

export function Body({ children, muted }: PropsWithChildren<{ muted?: boolean }>) {
  const theme = useTheme();
  return <Text style={[styles.body, { color: muted ? theme.textMuted : theme.text }]}>{children}</Text>;
}

export function Heading({ children }: PropsWithChildren) {
  const theme = useTheme();
  return <Text style={[styles.heading, { color: theme.text }]}>{children}</Text>;
}

type ButtonProps = {
  label: string;
  onPress: () => void;
  variant?: 'primary' | 'secondary' | 'danger';
  busy?: boolean;
  disabled?: boolean;
};

export function Button({ label, onPress, variant = 'primary', busy, disabled }: ButtonProps) {
  const theme = useTheme();
  const isPrimary = variant === 'primary';
  const color = variant === 'danger' ? theme.danger : isPrimary ? theme.onPrimary : theme.primary;

  return (
    <Pressable
      accessibilityRole="button"
      accessibilityState={{ disabled: disabled || busy, busy }}
      disabled={disabled || busy}
      onPress={onPress}
      style={({ pressed }) => [
        styles.button,
        isPrimary
          ? { backgroundColor: theme.primary }
          : { backgroundColor: 'transparent', borderColor: variant === 'danger' ? theme.danger : theme.border, borderWidth: 1 },
        (pressed || disabled) && { opacity: 0.6 },
      ]}>
      {busy ? <ActivityIndicator color={color} /> : <Text style={[styles.buttonLabel, { color }]}>{label}</Text>}
    </Pressable>
  );
}

export function TextField({ label, ...props }: TextInputProps & { label: string }) {
  const theme = useTheme();
  return (
    <View style={styles.field}>
      <Text style={[styles.label, { color: theme.textMuted }]}>{label}</Text>
      <TextInput
        accessibilityLabel={label}
        placeholderTextColor={theme.textMuted}
        style={[styles.input, { color: theme.text, borderColor: theme.border, backgroundColor: theme.surface }]}
        {...props}
      />
    </View>
  );
}

// A row of mutually exclusive options
export function Segmented<T extends string>({
  options,
  value,
  onChange,
}: {
  options: { value: T; label: string }[];
  value: T;
  onChange: (value: T) => void;
}) {
  const theme = useTheme();
  return (
    <View style={[styles.segmented, { borderColor: theme.border, backgroundColor: theme.surface }]} accessibilityRole="tablist">
      {options.map((option) => {
        const selected = option.value === value;
        return (
          <Pressable
            key={option.value}
            accessibilityRole="tab"
            accessibilityState={{ selected }}
            onPress={() => onChange(option.value)}
            style={[styles.segment, selected && { backgroundColor: theme.primary }]}>
            <Text style={[styles.segmentLabel, { color: selected ? theme.onPrimary : theme.text }]}>{option.label}</Text>
          </Pressable>
        );
      })}
    </View>
  );
}

// A row of single-choice pills that scrolls sideways, for option lists of any length
export function Chips<T extends string>({
  options,
  value,
  onChange,
}: {
  options: { value: T; label: string }[];
  value: T;
  onChange: (value: T) => void;
}) {
  const theme = useTheme();
  return (
    <ScrollView horizontal showsHorizontalScrollIndicator={false} contentContainerStyle={styles.chips} accessibilityRole="tablist">
      {options.map((option) => {
        const selected = option.value === value;
        return (
          <Pressable
            key={option.value}
            accessibilityRole="tab"
            accessibilityState={{ selected }}
            onPress={() => onChange(option.value)}
            style={[styles.chip, { borderColor: selected ? theme.primary : theme.border, backgroundColor: selected ? theme.primary : theme.surface }]}>
            <Text style={[styles.segmentLabel, { color: selected ? theme.onPrimary : theme.text }]}>{option.label}</Text>
          </Pressable>
        );
      })}
    </ScrollView>
  );
}

export function Message({ children, tone = 'muted' }: PropsWithChildren<{ tone?: 'muted' | 'danger' | 'success' }>) {
  const theme = useTheme();
  const color = tone === 'danger' ? theme.danger : tone === 'success' ? theme.success : theme.textMuted;
  return (
    <Text style={[styles.body, { color }]} accessibilityLiveRegion="polite">
      {children}
    </Text>
  );
}

export function Loading() {
  const theme = useTheme();
  return (
    <View style={[styles.fill, styles.center, { backgroundColor: theme.background }]}>
      <ActivityIndicator color={theme.primary} size="large" />
    </View>
  );
}

const styles = StyleSheet.create({
  screen: { flex: 1 },
  fill: { flex: 1 },
  center: { alignItems: 'center', justifyContent: 'center' },
  content: { padding: spacing.lg, gap: spacing.lg },
  header: { flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between', gap: spacing.md },
  title: { fontSize: 30, fontWeight: '700', flexShrink: 1 },
  heading: { fontSize: 18, fontWeight: '600' },
  body: { fontSize: 16, lineHeight: 22 },
  card: { borderRadius: 14, borderWidth: StyleSheet.hairlineWidth, padding: spacing.lg, gap: spacing.md },
  button: { minHeight: 48, borderRadius: 12, paddingHorizontal: spacing.lg, alignItems: 'center', justifyContent: 'center' },
  buttonLabel: { fontSize: 16, fontWeight: '600' },
  field: { gap: spacing.xs },
  label: { fontSize: 14, fontWeight: '500' },
  input: { minHeight: 48, borderRadius: 12, borderWidth: 1, paddingHorizontal: spacing.md, fontSize: 16 },
  segmented: { flexDirection: 'row', borderRadius: 12, borderWidth: 1, padding: 3, gap: 3 },
  segment: { flex: 1, minHeight: 40, borderRadius: 9, alignItems: 'center', justifyContent: 'center' },
  segmentLabel: { fontSize: 15, fontWeight: '600' },
  chips: { gap: spacing.sm },
  chip: { minHeight: 36, paddingHorizontal: spacing.md, borderRadius: 18, borderWidth: 1, alignItems: 'center', justifyContent: 'center' },
});
