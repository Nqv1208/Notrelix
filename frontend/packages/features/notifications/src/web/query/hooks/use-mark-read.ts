import { useAccountId } from "@notrelix/query";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import {
  createNotificationsService,
  type NotificationsApiClient,
  type NotificationsEndpoints,
} from "../../../core/api/notifications.service";
import { notificationsQueryKeys } from "../../../query/keys";

interface UseMarkReadDeps {
  api: NotificationsApiClient;
  endpoints: NotificationsEndpoints;
}

export function createUseMarkRead({ api, endpoints }: UseMarkReadDeps) {
  const service = createNotificationsService(api, endpoints);

  return function useMarkRead() {
    const accountId = useAccountId();
    const queryClient = useQueryClient();

    const markReadMutation = useMutation({
      mutationFn: (id: string) => service.markAsRead(id),
      onSuccess: () => {
        queryClient.invalidateQueries({
          queryKey: notificationsQueryKeys.all(accountId),
        });
        queryClient.invalidateQueries({
          queryKey: notificationsQueryKeys.unreadCount(accountId),
        });
      },
    });

    const markAllReadMutation = useMutation({
      mutationFn: () => service.markAllAsRead(),
      onSuccess: () => {
        queryClient.invalidateQueries({
          queryKey: notificationsQueryKeys.all(accountId),
        });
        queryClient.invalidateQueries({
          queryKey: notificationsQueryKeys.unreadCount(accountId),
        });
      },
    });

    return {
      markRead: markReadMutation.mutate,
      markAllRead: markAllReadMutation.mutate,
      isPending: markReadMutation.isPending || markAllReadMutation.isPending,
    };
  };
}
