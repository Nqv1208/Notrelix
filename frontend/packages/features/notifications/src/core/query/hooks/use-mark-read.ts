import { useMutation, useQueryClient } from '@tanstack/react-query';
import { createNotificationsService, type NotificationsApiClient, type NotificationsEndpoints } from '../../api/notifications.service';
import { notificationsQueryKeys } from '../keys';

interface UseMarkReadDeps {
  api: NotificationsApiClient;
  endpoints: NotificationsEndpoints;
}

export function createUseMarkRead({ api, endpoints }: UseMarkReadDeps) {
  const service = createNotificationsService(api, endpoints);

  return function useMarkRead() {
    const queryClient = useQueryClient();

    const markReadMutation = useMutation({
      mutationFn: (id: string) => service.markAsRead(id),
      onSuccess: () => {
        queryClient.invalidateQueries({ queryKey: notificationsQueryKeys.all });
        queryClient.invalidateQueries({ queryKey: notificationsQueryKeys.unreadCount });
      },
    });

    const markAllReadMutation = useMutation({
      mutationFn: () => service.markAllAsRead(),
      onSuccess: () => {
        queryClient.invalidateQueries({ queryKey: notificationsQueryKeys.all });
        queryClient.invalidateQueries({ queryKey: notificationsQueryKeys.unreadCount });
      },
    });

    return {
      markRead: markReadMutation.mutate,
      markAllRead: markAllReadMutation.mutate,
      isPending: markReadMutation.isPending || markAllReadMutation.isPending,
    };
  };
}
