import { useCallback, useMemo, useState } from "react";
import {
  AccountNotificationsSurface,
  useAccountNotificationsCopy,
  type AccountNotificationsState,
  type AccountNotificationValues,
  type AccountSaveStatus,
} from "@notrelix/features-account";
import { createUseNotificationPreferences } from "@notrelix/features-notifications";
import { useAppRuntime } from "@notrelix/runtime-web";

/** Shown when the saved preferences cannot be read. */
const DEFAULT_PREFERENCES: AccountNotificationValues = {
  emailEnabled: true,
  pushEnabled: true,
};

export function AccountNotificationsPage() {
  const { api: runtimeClient } = useAppRuntime();
  const copy = useAccountNotificationsCopy();

  const useNotificationPreferences = useMemo(
    () =>
      createUseNotificationPreferences({
        api: runtimeClient.api,
        endpoints: runtimeClient.endpoints,
      }),
    [runtimeClient],
  );

  const { preferences, isError, refetch, updatePreferences } =
    useNotificationPreferences();
  const [save, setSave] = useState<AccountSaveStatus>({ state: "idle" });

  const state = useMemo<AccountNotificationsState>(
    () =>
      preferences
        ? {
            status: "ready",
            values: {
              emailEnabled: preferences.emailEnabled,
              pushEnabled: preferences.pushEnabled,
            },
          }
        : isError
          ? { status: "error", fallback: DEFAULT_PREFERENCES }
          : { status: "loading" },
    [preferences, isError],
  );

  const handleSave = useCallback(
    (values: AccountNotificationValues) => {
      setSave({ state: "saving" });
      updatePreferences(values, {
        onSuccess: () => setSave({ state: "saved" }),
        onError: () => setSave({ state: "error" }),
      });
    },
    [updatePreferences],
  );

  return (
    <AccountNotificationsSurface
      copy={copy}
      preferences={state}
      save={save}
      onSave={handleSave}
      onRetry={() => void refetch()}
    />
  );
}
