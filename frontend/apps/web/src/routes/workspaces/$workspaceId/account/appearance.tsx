import { useCallback, useEffect, useMemo, useState } from "react";
import { useQuery } from "@tanstack/react-query";
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
import {
  accountPagesKeys,
  useAccountPagesApi,
  useToast,
} from "./use-account-pages";

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

  const accountApi = useAccountPagesApi();
  const details = useQuery({
    queryKey: accountPagesKeys.details,
    queryFn: () => accountApi.getDetails(),
    retry: false,
  });
  const [toast, showToast] = useToast();
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
      showToast(copy.sidebar.saved);
      if (mode === "high-contrast") return;
      try {
        await updateMutation.mutateAsync({ theme: mode });
      } catch {
        // The device mode is already applied; the account mirror is best effort.
      }
    },
    [theme, updateMutation, showToast, copy.sidebar.saved],
  );

  const handleSidebarCollapsedChange = useCallback(
    async (collapsed: boolean) => {
      setPendingCollapsed(collapsed);
      setSidebarSave({ state: "saving" });
      try {
        await updateMutation.mutateAsync({ sidebarCollapsed: collapsed });
        setSidebarSave({ state: "saved" });
        showToast(copy.sidebar.saved);
      } catch {
        setPendingCollapsed(null);
        setSidebarSave({ state: "error" });
      }
    },
    [updateMutation, showToast, copy.sidebar.saved],
  );

  return (
    <AccountAppearanceSurface
      copy={copy}
      mode={theme.mode}
      accent={theme.accent}
      locale={locale}
      workspaceName={details.data?.workspaceName ?? ""}
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
      }}
      onSidebarCollapsedChange={(collapsed) =>
        void handleSidebarCollapsedChange(collapsed)
      }
      onRetryPreferences={() => void refetch()}
    />
  );
}
