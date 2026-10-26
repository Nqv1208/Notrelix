import { useCallback, useMemo, useState } from "react";
import {
  AccountAppearanceSurface,
  createUsePreferences,
  useAccountAppearanceCopy,
  type AccountSaveStatus,
  type AccountSection,
  type AccountSidebarPreference,
} from "@notrelix/features-account";
import { useLocale } from "@notrelix/i18n";
import { useAppRuntime } from "@notrelix/runtime-web";
import { useTheme, type ThemeMode } from "@notrelix/ui-web";
import { useRouteWorkspaceName, useToast } from "./use-account-pages";
import { useClientPreference } from "./use-client-preference";

/**
 * The look of the app. The theme mode is the device's and is mirrored to the
 * account preference (`light` / `dark` / `system`; one value, no separate
 * colour theme); the sidebar default is this browser's alone.
 */
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

  const workspaceName = useRouteWorkspaceName();
  const [toast, showToast] = useToast();
  const { save } = usePreferences();
  const [collapsed, setCollapsed] = useClientPreference<boolean>(
    "notrelix-sidebar-collapsed",
    false,
  );
  const [sidebarSave, setSidebarSave] = useState<AccountSaveStatus>({
    state: "idle",
  });

  const sidebar = useMemo<AccountSection<AccountSidebarPreference>>(
    () => ({ status: "ready", data: { collapsed } }),
    [collapsed],
  );

  // Mirror the device mode to account preferences; the preference contract
  // only knows light/dark/system, so high contrast stays device-local.
  const handleModeChange = useCallback(
    async (mode: ThemeMode) => {
      theme.setMode(mode);
      showToast(copy.sidebar.saved);
      if (mode === "high-contrast") return;
      try {
        await save({ theme: mode });
      } catch {
        // The device mode is already applied; the account mirror is best effort.
      }
    },
    [theme, save, showToast, copy.sidebar.saved],
  );

  const handleSidebarCollapsedChange = useCallback(
    (next: boolean) => {
      setCollapsed(next);
      setSidebarSave({ state: "saved" });
      showToast(copy.sidebar.saved);
    },
    [setCollapsed, showToast, copy.sidebar.saved],
  );

  return (
    <AccountAppearanceSurface
      copy={copy}
      mode={theme.mode}
      accent={theme.accent}
      locale={locale}
      workspaceName={workspaceName}
      toast={toast !== null}
      sidebar={sidebar}
      sidebarSave={sidebarSave}
      onModeChange={(mode) => void handleModeChange(mode)}
      onAccentChange={(accent) => {
        theme.setAccent(accent);
        showToast(copy.sidebar.saved);
      }}
      onLocaleChange={(next) => {
        setLocale(next);
        showToast(copy.sidebar.saved);
        // The account keeps the language too; the device already switched.
        void save({ locale: next }).catch(() => undefined);
      }}
      onSidebarCollapsedChange={handleSidebarCollapsedChange}
      onRetryPreferences={() => undefined}
    />
  );
}
