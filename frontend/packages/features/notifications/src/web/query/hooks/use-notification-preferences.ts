import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  createNotificationsService,
  type NotificationsApiClient,
  type NotificationsEndpoints,
} from "../../../core/api/notifications.service";
import { notificationsQueryKeys } from "../../../query/keys";
import {
  NOTIFICATION_PREFERENCE_TYPES,
  type NotificationPreferences,
} from "../../../core/types/notifications";

interface UseNotificationPreferencesDeps {
  api: NotificationsApiClient;
  endpoints: NotificationsEndpoints;
}

/** A save that lost the race to another device: the version it quoted is old. */
export function isVersionConflict(error: unknown): boolean {
  return (
    typeof error === "object" &&
    error !== null &&
    (error as { status?: unknown }).status === 412
  );
}

/**
 * One member's notification choices in one workspace. A save sends the cells
 * that changed with the version they were read at; a 412 refetches the
 * authoritative choices and surfaces the failure so the page can say so.
 */
export function createUseNotificationPreferences({
  api,
  endpoints,
}: UseNotificationPreferencesDeps) {
  const service = createNotificationsService(api, endpoints);

  return function useNotificationPreferences(workspaceId: string) {
    const queryClient = useQueryClient();
    const key = notificationsQueryKeys.preferences(workspaceId);

    const query = useQuery({
      queryKey: key,
      queryFn: () => service.getPreferences(workspaceId),
      retry: false,
    });

    const mutation = useMutation({
      mutationFn: (next: NotificationPreferences) => {
        const current = queryClient.getQueryData<NotificationPreferences>(key);
        const changed = NOTIFICATION_PREFERENCE_TYPES.flatMap((type) => {
          const before = current?.rows[type];
          const after = next.rows[type];
          if (before?.inApp === after.inApp && before?.email === after.email)
            return [];
          return [
            {
              type,
              ...(before?.inApp === after.inApp ? {} : { inApp: after.inApp }),
              ...(before?.email === after.email ? {} : { email: after.email }),
            },
          ];
        });
        return service.updatePreferences(workspaceId, {
          expectedVersion: current?.version ?? next.version,
          rows: changed,
        });
      },
      onSuccess: (saved) => queryClient.setQueryData(key, saved),
      onError: (error) => {
        if (isVersionConflict(error))
          void queryClient.invalidateQueries({ queryKey: key });
      },
    });

    return {
      preferences: query.data,
      isLoading: query.isLoading,
      isError: query.isError,
      refetch: query.refetch,
      save: mutation.mutateAsync,
      isSaving: mutation.isPending,
    };
  };
}
