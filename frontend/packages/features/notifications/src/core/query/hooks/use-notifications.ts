import { useQuery } from '@tanstack/react-query';
import { createNotificationsService, type NotificationsApiClient, type NotificationsEndpoints } from '~/core/api/notifications.service';
import { notificationsQueryKeys } from '../keys';

interface UseNotificationsDeps {
  api: NotificationsApiClient;
  endpoints: NotificationsEndpoints;
  options?: {
    mockMode?: boolean;
  };

}

export function createUseNotifications({ api, endpoints, options }: UseNotificationsDeps) {
  const service = createNotificationsService(api, endpoints, options);
  return function useNotifications() {
    return useQuery({
      queryKey: notificationsQueryKeys.all,
      queryFn: () => service.getList(),
    });
  };
}
