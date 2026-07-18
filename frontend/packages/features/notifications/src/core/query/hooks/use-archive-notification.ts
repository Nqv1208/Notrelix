import { useMutation, useQueryClient } from '@tanstack/react-query';
import { createNotificationsService, type NotificationsApiClient, type NotificationsEndpoints } from '~/core/api/notifications.service';
import { notificationsQueryKeys } from '../keys';

interface UseArchiveNotificationDeps {
  api: NotificationsApiClient;
  endpoints: NotificationsEndpoints;
  options?: {
    mockMode?: boolean;
  };

}

export function createUseArchiveNotification({ api, endpoints, options }: UseArchiveNotificationDeps) {
  const service = createNotificationsService(api, endpoints, options);

  return function useArchiveNotification() {
    const queryClient = useQueryClient();

    return useMutation({
      mutationFn: (id: string) => service.archive(id),
      onSuccess: () => {
        queryClient.invalidateQueries({ queryKey: notificationsQueryKeys.all });
        queryClient.invalidateQueries({ queryKey: notificationsQueryKeys.unreadCount });
      },
    });
  };
}
