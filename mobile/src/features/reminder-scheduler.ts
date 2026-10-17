import * as Notifications from 'expo-notifications';
import * as SecureStore from 'expo-secure-store';
import { Platform } from 'react-native';

import type { Reminder } from './reminders';

const channelId = 'reminders';
const preferenceKey = 'househeroes.reminders.enabled';

// Show reminders that fire while the app is open, too
Notifications.setNotificationHandler({
  handleNotification: async () => ({
    shouldShowBanner: true,
    shouldShowList: true,
    shouldPlaySound: false,
    shouldSetBadge: false,
  }),
});

export async function getRemindersEnabled(): Promise<boolean> {
  return (await SecureStore.getItemAsync(preferenceKey)) !== 'off';
}

export async function saveRemindersEnabled(enabled: boolean): Promise<void> {
  await SecureStore.setItemAsync(preferenceKey, enabled ? 'on' : 'off');
}

// Whether reminders can be shown, asking the user if they haven't decided yet
export async function ensureNotificationPermission(): Promise<boolean> {
  // Android 13+ only shows the permission prompt once a channel exists
  if (Platform.OS === 'android') {
    await Notifications.setNotificationChannelAsync(channelId, {
      name: 'Task reminders',
      // eslint-plugin-import can't parse how the compiled enum is exported; it exists at runtime
      // eslint-disable-next-line import/namespace
      importance: Notifications.AndroidImportance.DEFAULT,
    });
  }

  const current = await Notifications.getPermissionsAsync();
  if (current.granted) return true;
  if (!current.canAskAgain) return false;
  return (await Notifications.requestPermissionsAsync()).granted;
}

// Changes run one at a time, so an older sync can't add reminders after a newer one
let pending: Promise<void> = Promise.resolve();
const enqueue = (change: () => Promise<void>) => (pending = pending.then(change, change));

// Replaces every scheduled reminder (the app schedules nothing else)
export function replaceReminders(reminders: readonly Reminder[]): Promise<void> {
  return enqueue(async () => {
    await Notifications.cancelAllScheduledNotificationsAsync();
    for (const reminder of reminders) {
      await Notifications.scheduleNotificationAsync({
        identifier: reminder.identifier,
        content: { title: reminder.title, body: reminder.body, data: { taskId: reminder.taskId } },
        trigger: { type: Notifications.SchedulableTriggerInputTypes.DATE, date: reminder.date, channelId },
      });
    }
  });
}

export function cancelReminders(): Promise<void> {
  return replaceReminders([]);
}
