import { useCallback, useMemo, useState } from "react";
import { useParams } from "@tanstack/react-router";
import {
  AccountNotificationsSurface,
  createUseProfile,
  useAccountNotificationsCopy,
  type AccountNotificationsState,
  type AccountNotificationValues,
  type AccountSaveStatus,
} from "@notrelix/features-account";
import { createUseNotificationPreferences } from "@notrelix/features-notifications";
import { useAppRuntime } from "@notrelix/runtime-web";

/**
 * The member's notification choices in the workspace of the route
 * (`Collaboration.Preferences.*`: workspace-scoped, saved with the version
 * they were read at). Mobile and quiet hours have no producer and are not
 * offered.
 */
export function AccountNotificationsPage() {
  const { workspaceId = "" } = useParams({ strict: false }) as {
    workspaceId?: string;
  };
  const { api: runtimeClient } = useAppRuntime();
  const copy = useAccountNotificationsCopy();
  const [save, setSave] = useState<AccountSaveStatus>({ state: "idle" });

  const useProfile = useMemo(
    () =>
      createUseProfile({
        api: runtimeClient.api,
        endpoints: runtimeClient.endpoints,
      }),
    [runtimeClient],
  );
  const useNotificationPreferences = useMemo(
    () =>
      createUseNotificationPreferences({
        api: runtimeClient.api,
        endpoints: runtimeClient.endpoints,
      }),
    [runtimeClient],
  );
  const { profile } = useProfile();
  const preferences = useNotificationPreferences(workspaceId);

  const state = useMemo<AccountNotificationsState>(
    () =>
      preferences.preferences
        ? {
            status: "ready",
            values: preferences.preferences,
            personName: profile?.name ?? "",
          }
        : preferences.isError
          ? { status: "error" }
          : { status: "loading" },
    [preferences.preferences, preferences.isError, profile?.name],
  );

  const { save: persist } = preferences;
  const handleSave = useCallback(
    async (values: AccountNotificationValues) => {
      setSave({ state: "saving" });
      try {
        await persist(values);
        setSave({ state: "saved" });
      } catch {
        // A stale version has already refetched the choices; the page says it failed.
        setSave({ state: "error" });
      }
    },
    [persist],
  );

  return (
    <AccountNotificationsSurface
      copy={copy}
      preferences={state}
      save={save}
      onSave={(values) => void handleSave(values)}
      onRetry={() => void preferences.refetch()}
    />
  );
}
