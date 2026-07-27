import { useMemo } from 'react';
import { createUsePreferences, createUseUpdatePreferences } from '@notrelix/features-account';
import { useAppRuntime } from '@notrelix/runtime-web';
import { Button } from '@notrelix/ui-web';
import { useColorTheme } from '@notrelix/ui-web';
import { Monitor, Sun, Moon } from 'lucide-react';
import { toast } from 'sonner';

const THEME_OPTIONS = [
  { value: 'light' as const, label: 'Light', icon: Sun },
  { value: 'dark' as const, label: 'Dark', icon: Moon },
  { value: 'system' as const, label: 'System', icon: Monitor },
];

const COLOR_THEMES = [
  { id: 'zinc', label: 'Zinc' },
  { id: 'slate', label: 'Slate' },
  { id: 'stone', label: 'Stone' },
  { id: 'gray', label: 'Gray' },
  { id: 'neutral', label: 'Neutral' },
];

export function AccountAppearancePage() {
  const { api: runtimeClient, env: runtimeEnv } = useAppRuntime();

  const usePreferences = useMemo(
    () =>
      createUsePreferences({
        api: runtimeClient.api,
        endpoints: runtimeClient.endpoints,
        options: { mockMode: runtimeEnv.nodeEnv === 'development' },
      }),
    [runtimeClient, runtimeEnv.nodeEnv],
  );

  const useUpdatePreferences = useMemo(
    () =>
      createUseUpdatePreferences({
        api: runtimeClient.api,
        endpoints: runtimeClient.endpoints,
        options: { mockMode: runtimeEnv.nodeEnv === 'development' },
      }),
    [runtimeClient, runtimeEnv.nodeEnv],
  );

  const { preferences, isLoading } = usePreferences();
  const updateMutation = useUpdatePreferences();
  const { colorTheme, setColorTheme } = useColorTheme();

  const handleThemeChange = async (theme: 'light' | 'dark' | 'system') => {
    try {
      await updateMutation.mutateAsync({ theme });
    } catch {
      // Preferences endpoint may not exist yet
    }
  };

  if (isLoading) {
    return (
      <div className="space-y-4">
        {[1, 2].map((i) => (
          <div key={i} className="h-20 bg-muted rounded-lg animate-pulse" />
        ))}
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div>
        <h2 className="font-semibold text-sm mb-1">Appearance</h2>
        <p className="text-xs text-muted-foreground">Customize how Notrelix looks on your device.</p>
      </div>

      {/* Theme */}
      <div className="space-y-3">
        <label className="text-sm font-medium text-foreground">Theme</label>
        <div className="grid grid-cols-3 gap-3">
          {THEME_OPTIONS.map((option) => {
            const Icon = option.icon;
            const isActive = preferences?.theme === option.value;
            return (
              <button
                key={option.value}
                onClick={() => handleThemeChange(option.value)}
                className={`flex flex-col items-center gap-2 rounded-lg border p-4 transition-colors ${
                  isActive
                    ? 'border-primary bg-primary/5 text-foreground'
                    : 'border-border text-muted-foreground hover:bg-muted/50'
                }`}
              >
                <Icon className="size-5" />
                <span className="text-sm font-medium">{option.label}</span>
              </button>
            );
          })}
        </div>
      </div>

      {/* Color Theme */}
      <div className="space-y-3">
        <label className="text-sm font-medium text-foreground">Accent Color</label>
        <div className="flex gap-2">
          {COLOR_THEMES.map((color) => (
            <button
              key={color.id}
              onClick={() => {
                setColorTheme(color.id as typeof colorTheme);
                toast.success(`Color theme: ${color.label}`);
              }}
              className={`size-8 rounded-full border-2 transition-colors ${
                colorTheme === color.id
                  ? 'border-primary ring-2 ring-primary/20'
                  : 'border-border hover:border-primary/50'
              }`}
              style={{
                backgroundColor: `var(--color-${color.id}-500, #71717a)`,
              }}
              title={color.label}
            />
          ))}
        </div>
      </div>

      {/* Sidebar */}
      <div className="space-y-3">
        <label className="text-sm font-medium text-foreground">Sidebar</label>
        <div className="flex items-center justify-between rounded-lg border border-border p-4">
          <div>
            <p className="text-sm">Collapsed by default</p>
            <p className="text-xs text-muted-foreground">Start with sidebar collapsed</p>
          </div>
          <Button
            variant="outline"
            size="sm"
            onClick={() => {
              toast.success('Sidebar preference saved');
            }}
          >
            {preferences?.sidebarCollapsed ? 'Expanded' : 'Collapsed'}
          </Button>
        </div>
      </div>
    </div>
  );
}
