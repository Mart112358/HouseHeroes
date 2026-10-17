import { InMemoryCache } from '@apollo/client';
import { MockedProvider } from '@apollo/client/testing/react';
import { render, waitFor } from '@testing-library/react-native';
import * as Notifications from 'expo-notifications';
import { Text } from 'react-native';

import { RemindersProvider, useReminders } from '@/features/reminders-provider';
import { CurrentUserDocument, MyTasksDocument } from '@/graphql/generated/graphql';
import { child, task } from '../test-support/factories';

const mockStorage = new Map<string, string>();
jest.mock('expo-secure-store', () => ({
  getItemAsync: jest.fn(async (key: string) => mockStorage.get(key) ?? null),
  setItemAsync: jest.fn(async (key: string, value: string) => void mockStorage.set(key, value)),
}));

jest.mock('expo-notifications', () => ({
  setNotificationHandler: jest.fn(),
  setNotificationChannelAsync: jest.fn(async () => null),
  getPermissionsAsync: jest.fn(),
  requestPermissionsAsync: jest.fn(),
  cancelAllScheduledNotificationsAsync: jest.fn(async () => undefined),
  scheduleNotificationAsync: jest.fn(async () => 'id'),
  SchedulableTriggerInputTypes: { DATE: 'date' },
  AndroidImportance: { DEFAULT: 3 },
}));

const notifications = jest.mocked(Notifications);

const inThreeDays = () => {
  const date = new Date();
  return new Date(date.getFullYear(), date.getMonth(), date.getDate() + 3, 23, 59).toISOString();
};

function Probe() {
  const { blocked } = useReminders();
  return <Text>{blocked ? 'blocked' : 'ok'}</Text>;
}

async function renderWithTasks(tasks: ReturnType<typeof task>[]) {
  const cache = new InMemoryCache({ typePolicies: { TaskAssignment: { keyFields: ['taskId', 'userId'] } } });
  cache.writeQuery({
    query: CurrentUserDocument,
    data: { currentUser: { id: child.id, firstName: 'Emma', lastName: 'T', email: 'e@t', role: 'CHILD', familyId: 'f' } },
  });
  const mocks = [{ request: { query: MyTasksDocument }, result: { data: { myTasks: tasks } } }];
  return render(
    <MockedProvider mocks={mocks} cache={cache}>
      <RemindersProvider>
        <Probe />
      </RemindersProvider>
    </MockedProvider>
  );
}

const permission = (granted: boolean, canAskAgain = true) => ({ granted, canAskAgain, status: granted ? 'granted' : 'denied', expires: 'never' });

beforeEach(() => {
  mockStorage.clear();
  jest.clearAllMocks();
});

it("schedules reminders for the viewer's due tasks once notifications are allowed", async () => {
  notifications.getPermissionsAsync.mockResolvedValue(permission(true) as never);

  await renderWithTasks([task({ id: 'dishes', title: 'Empty the dishwasher', dueDate: inThreeDays(), assignedTo: [child.id] })]);

  await waitFor(() => expect(notifications.scheduleNotificationAsync).toHaveBeenCalledTimes(2));
  const identifiers = notifications.scheduleNotificationAsync.mock.calls.map(([request]) => request.identifier);
  expect(identifiers).toEqual(['task-dishes-due-soon', 'task-dishes-overdue']);
  expect(notifications.cancelAllScheduledNotificationsAsync).toHaveBeenCalled();
});

it('does not ask for permission when there is nothing to remind about', async () => {
  await renderWithTasks([task({ assignedTo: [child.id] })]);

  await waitFor(() => expect(notifications.cancelAllScheduledNotificationsAsync).toHaveBeenCalled());
  expect(notifications.getPermissionsAsync).not.toHaveBeenCalled();
  expect(notifications.requestPermissionsAsync).not.toHaveBeenCalled();
});

it('schedules nothing when the user turned reminders off', async () => {
  mockStorage.set('househeroes.reminders.enabled', 'off');

  await renderWithTasks([task({ dueDate: inThreeDays(), assignedTo: [child.id] })]);

  await waitFor(() => expect(notifications.cancelAllScheduledNotificationsAsync).toHaveBeenCalled());
  expect(notifications.scheduleNotificationAsync).not.toHaveBeenCalled();
});

it('reports blocked and schedules nothing when notifications are denied', async () => {
  notifications.getPermissionsAsync.mockResolvedValue(permission(false, false) as never);

  const screen = await renderWithTasks([task({ dueDate: inThreeDays(), assignedTo: [child.id] })]);

  expect(await screen.findByText('blocked')).toBeOnTheScreen();
  expect(notifications.requestPermissionsAsync).not.toHaveBeenCalled();
  expect(notifications.scheduleNotificationAsync).not.toHaveBeenCalled();
});
