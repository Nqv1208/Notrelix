import { useCallback, useEffect, useMemo, useState } from "react";
import {
  AccountAppearanceSurface,
  createUsePreferences,
  createUseUpdatePreferences,
  useAccountAppearanceCopy,
  type AccountSaveStatus,
  type AccountSection,
  type AccountSidebarPreference,
} from "@notrelix/features-account";
import { useLocale } from "@notrelix/i18n";
import { useAppRuntime } from "@notrelix/runtime-web";
import { useTheme, type ThemeMode } from "@notrelix/ui-web";

export function AccountAppearancePage() {
  const { api: runtimeClient } = useAppRuntime();
  const copy = useAccountAppearanceCopy();
  const theme = useTheme();
  const { locale, setLocale } = useLocale();

  const usePreferences = useMemo(
    () =>
      createUsePreferences({
        api: runtimeClient.api,
        endpoints: runtimeClient.endpoints,
      }),
    [runtimeClient],
  );

  const useUpdatePreferences = useMemo(
    () =>
      createUseUpdatePreferences({
        api: runtimeClient.api,
        endpoints: runtimeClient.endpoints,
      }),
    [runtimeClient],
  );

  const { preferences, isError, refetch } = usePreferences();
  const updateMutation = useUpdatePreferences();
  const [sidebarSave, setSidebarSave] = useState<AccountSaveStatus>({
    state: "idle",
  });
  // The value just sent, shown until the refetched preference confirms it.
  const [pendingCollapsed, setPendingCollapsed] = useState<boolean | null>(
    null,
  );

  useEffect(() => {
    if (
      pendingCollapsed !== null &&
      preferences?.sidebarCollapsed === pendingCollapsed
    ) {
      setPendingCollapsed(null);
    }
  }, [preferences?.sidebarCollapsed, pendingCollapsed]);

  const sidebar = useMemo<AccountSection<AccountSidebarPreference>>(
    () =>
      preferences
        ? {
            status: "ready",
            data: {
              collapsed: pendingCollapsed ?? preferences.sidebarCollapsed,
            },
          }
        : isError
          ? { status: "error" }
          : { status: "loading" },
    [preferences, isError, pendingCollapsed],
  );

  // Mirror the device mode to account preferences; the preference contract
  // only knows light/dark/system, so high contrast stays device-local.
  const handleModeChange = useCallback(
    async (mode: ThemeMode) => {
      theme.setMode(mode);
      if (mode === "high-contrast") return;
      try {
        await updateMutation.mutateAsync({ theme: mode });
      } catch {
        // The device mode is already applied; the account mirror is best effort.
      }
    },
    [theme, updateMutation],
  );

  const handleSidebarCollapsedChange = useCallback(
    async (collapsed: boolean) => {
      setPendingCollapsed(collapsed);
      setSidebarSave({ state: "saving" });
      try {
        await updateMutation.mutateAsync({ sidebarCollapsed: collapsed });
        setSidebarSave({ state: "saved" });
      } catch {
        setPendingCollapsed(null);
        setSidebarSave({ state: "error" });
      }
    },
    [updateMutation],
  );

  return (
    <AccountAppearanceSurface
      copy={copy}
      mode={theme.mode}
      accent={theme.accent}
      locale={locale}
      sidebar={sidebar}
      sidebarSave={sidebarSave}
      onModeChange={(mode) => void handleModeChange(mode)}
      onAccentChange={theme.setAccent}
      onLocaleChange={setLocale}
      onSidebarCollapsedChange={(collapsed) =>
        void handleSidebarCollapsedChange(collapsed)
      }
      onRetryPreferences={() => void refetch()}
    />
  );
}
