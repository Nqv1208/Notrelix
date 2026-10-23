import { useAccountId } from "@notrelix/query";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import {
  createNotificationsService,
  type NotificationsApiClient,
  type NotificationsEndpoints,
} from "../../../core/api/notifications.service";
import { notificationsQueryKeys } from "../../../query/keys";
import type { NotificationPreferences } from "../../../core/types/notifications";

interface UseNotificationPreferencesDeps {
  api: NotificationsApiClient;
  endpoints: NotificationsEndpoints;
}

export function createUseNotificationPreferences({
  api,
  endpoints,
}: UseNotificationPreferencesDeps) {
  const service = createNotificationsService(api, endpoints);

  return function useNotificationPreferences() {
    const accountId = useAccountId();
    const queryClient = useQueryClient();

    const query = useQuery({
      queryKey: notificationsQueryKeys.preferences(accountId),
      queryFn: () => service.getPreferences(),
    });

    const mutation = useMutation({
      mutationFn: (prefs: Partial<NotificationPreferences>) =>
        service.updatePreferences(prefs),
      onSuccess: () => {
        queryClient.invalidateQueries({
          queryKey: notificationsQueryKeys.preferences(accountId),
        });
      },
    });

    return {
      preferences: query.data,
      isLoading: query.isLoading,
      isError: query.isError,
      refetch: query.refetch,
      updatePreferences: mutation.mutate,
      isUpdating: mutation.isPending,
    };
  };
}
