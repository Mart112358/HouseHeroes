import type { UserRole } from '@/graphql/generated/graphql';

export const roleLabels: Record<UserRole, string> = {
  GUARDIAN: 'Parent',
  CHILD: 'Child',
};

export const roleOptions = (['CHILD', 'GUARDIAN'] as const).map((value) => ({ value, label: roleLabels[value] }));
