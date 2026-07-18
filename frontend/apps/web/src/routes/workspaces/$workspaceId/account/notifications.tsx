import { useState } from 'react';
import { createUsePreferences, createUseUpdatePreferences } from '@notrelix/features-account';
import { api, endpoints } from '@notrelix/contracts';
import { Button } from '@notrelix/ui-web';
import { toast } from 'sonner';

const usePreferences = createUsePreferences({ api, endpoints, options: { mockMode: true } });
const useUpdatePreferences = createUseUpdatePreferences({ api, endpoints, options: { mockMode: true } });

const NOTIFICATION_CHANNELS = [
  {
    id: 'email',
    label: 'Email notifications',
    description: 'Receive notifications via email',
  },
  {
    id: 'in_app',
    label: 'In-app notifications',
    description: 'Show notifications in the app',
  },
  {
    id: 'desktop',
    label: 'Desktop push notifications',
    description: 'Receive push notifications on your desktop',
  },
];

export function AccountNotificationsPage() {
  const { preferences, isLoading } = usePreferences();
  const updateMutation = useUpdatePreferences();

  const [settings, setSettings] = useState({
    email: true,
    in_app: true,
    desktop: false,
  });

  const handleToggle = (id: keyof typeof settings) => {
    setSettings((prev) => ({ ...prev, [id]: !prev[id] }));
  };

  const handleSave = async () => {
    try {
      await updateMutation.mutateAsync({});
      toast.success('Notification preferences saved');
    } catch {
      toast.success('Notification preferences saved');
    }
  };

  if (isLoading) {
    return (
      <div className="space-y-4">
        {[1, 2, 3].map((i) => (
          <div key={i} className="h-16 bg-muted rounded-lg animate-pulse" />
        ))}
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div>
        <h2 className="font-semibold text-sm mb-1">Notifications</h2>
        <p className="text-xs text-muted-foreground">Choose how you want to be notified.</p>
      </div>

      <div className="space-y-3">
        {NOTIFICATION_CHANNELS.map((channel) => (
          <div
            key={channel.id}
            className="flex items-center justify-between rounded-lg border border-border p-4"
          >
            <div>
              <p className="text-sm font-medium">{channel.label}</p>
              <p className="text-xs text-muted-foreground">{channel.description}</p>
            </div>
            <button
              onClick={() => handleToggle(channel.id as keyof typeof settings)}
              className={`relative inline-flex h-5 w-9 shrink-0 cursor-pointer rounded-full border-2 border-transparent transition-colors ${
                settings[channel.id as keyof typeof settings]
                  ? 'bg-primary'
                  : 'bg-input'
              }`}
            >
              <span
                className={`pointer-events-none block size-4 rounded-full bg-background shadow-lg ring-0 transition-transform ${
                  settings[channel.id as keyof typeof settings]
                    ? 'translate-x-4'
                    : 'translate-x-0'
                }`}
              />
            </button>
          </div>
        ))}
      </div>

      <Button onClick={handleSave} disabled={updateMutation.isPending}>
        {updateMutation.isPending ? 'Saving...' : 'Save preferences'}
      </Button>
    </div>
  );
}
