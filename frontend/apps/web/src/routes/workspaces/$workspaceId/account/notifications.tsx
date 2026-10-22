import { useCallback, useMemo, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  AccountNotificationsSurface,
  createUseProfile,
  useAccountNotificationsCopy,
  type AccountNotificationsState,
  type AccountNotificationValues,
  type AccountSaveStatus,
} from "@notrelix/features-account";
import { useAppRuntime } from "@notrelix/runtime-web";
import { useAccountPagesKeys, useAccountPagesApi } from "./use-account-pages";

const GMT: Record<string, string> = {
  UTC: "GMT",
  "Asia/Ho_Chi_Minh": "GMT+7",
  "Asia/Tokyo": "GMT+9",
  "Europe/Paris": "GMT+1",
};

export function AccountNotificationsPage() {
  const accountPagesKeys = useAccountPagesKeys();
  const { api: runtimeClient } = useAppRuntime();
  const copy = useAccountNotificationsCopy();
  const api = useAccountPagesApi();
  const queryClient = useQueryClient();
  const [save, setSave] = useState<AccountSaveStatus>({ state: "idle" });

  const useProfile = useMemo(
    () =>
      createUseProfile({
        api: runtimeClient.api,
        endpoints: runtimeClient.endpoints,
      }),
    [runtimeClient],
  );
  const { profile } = useProfile();
  const matrix = useQuery({
    queryKey: accountPagesKeys.notifications,
    queryFn: () => api.getNotificationMatrix(),
    retry: false,
  });
  const details = useQuery({
    queryKey: accountPagesKeys.details,
    queryFn: () => api.getDetails(),
    retry: false,
  });
  const write = useMutation({
    mutationFn: (values: AccountNotificationValues) =>
      api.saveNotificationMatrix(values),
    onSuccess: () =>
      queryClient.invalidateQueries({
        queryKey: accountPagesKeys.notifications,
      }),
  });

  const state = useMemo<AccountNotificationsState>(
    () =>
      matrix.data
        ? {
            status: "ready",
            values: matrix.data,
            personName: profile?.name ?? "",
            zoneLabel: GMT[details.data?.timezone ?? ""] ?? "GMT+7",
          }
        : matrix.isError
          ? { status: "error" }
          : { status: "loading" },
    [matrix.data, matrix.isError, profile?.name, details.data?.timezone],
  );

  const handleSave = useCallback(
    async (values: AccountNotificationValues) => {
      setSave({ state: "saving" });
      try {
        await write.mutateAsync(values);
        setSave({ state: "saved" });
      } catch {
        setSave({ state: "error" });
      }
    },
    [write],
  );

  return (
    <AccountNotificationsSurface
      copy={copy}
      preferences={state}
      save={save}
      onSave={(values) => void handleSave(values)}
      onRetry={() => void matrix.refetch()}
    />
  );
}
