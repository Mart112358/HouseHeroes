import { useQuery } from '@apollo/client/react';
import { createContext, use, useEffect, useMemo, useState, type PropsWithChildren } from 'react';

import { MyTasksDocument } from '@/graphql/generated/graphql';
import {
  cancelReminders,
  ensureNotificationPermission,
  getRemindersEnabled,
  replaceReminders,
  saveRemindersEnabled,
} from './reminder-scheduler';
import { planReminders } from './reminders';
import { useViewer } from './use-viewer';

type RemindersContextValue = {
  enabled: boolean;
  // Reminders are on in the app but notifications are off in system settings
  blocked: boolean;
  setEnabled: (enabled: boolean) => Promise<void>;
};

const RemindersContext = createContext<RemindersContextValue | null>(null);

// Keeps this device's reminders in step with the viewer's tasks. Mounted for registered users only.
export function RemindersProvider({ children }: PropsWithChildren) {
  const viewer = useViewer();
  const { data } = useQuery(MyTasksDocument);
  const [enabled, setEnabledState] = useState<boolean | null>(null);
  const [blocked, setBlocked] = useState(false);

  useEffect(() => {
    getRemindersEnabled().then(setEnabledState);
  }, []);

  const reminders = useMemo(() => (data ? planReminders(data.myTasks, viewer) : null), [data, viewer]);

  useEffect(() => {
    if (enabled === null || reminders === null) return;
    let current = true;

    const sync = async () => {
      // Don't ask for permission until there's something to remind about
      if (!enabled || reminders.length === 0) return cancelReminders();

      const allowed = await ensureNotificationPermission();
      if (!current) return;
      setBlocked(!allowed);
      return allowed ? replaceReminders(reminders) : cancelReminders();
    };
    sync().catch(() => undefined);

    return () => {
      current = false;
    };
  }, [enabled, reminders]);

  const value = useMemo<RemindersContextValue>(
    () => ({
      enabled: enabled ?? true,
      blocked,
      setEnabled: async (on) => {
        await saveRemindersEnabled(on);
        setEnabledState(on);
        // Ask straight away when the user turns reminders on
        if (on) setBlocked(!(await ensureNotificationPermission()));
      },
    }),
    [enabled, blocked]
  );

  return <RemindersContext value={value}>{children}</RemindersContext>;
}

export function useReminders(): RemindersContextValue {
  const context = use(RemindersContext);
  if (!context) throw new Error('useReminders must be used within RemindersProvider');
  return context;
}
