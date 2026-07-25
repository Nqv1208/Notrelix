import { useQuery } from '@tanstack/react-query';
import { createNotificationsService, type NotificationsApiClient, type NotificationsEndpoints } from '../../api/notifications.service';
import { notificationsQueryKeys } from '../keys';

interface UseUnreadCountDeps {
  api: NotificationsApiClient;
  endpoints: NotificationsEndpoints;
  options?: {
    mockMode?: boolean;
  };

}

export function createUseUnreadCount({ api, endpoints, options }: UseUnreadCountDeps) {
  const service = createNotificationsService(api, endpoints, options);
  return function useUnreadCount() {
    return useQuery({
      queryKey: notificationsQueryKeys.unreadCount,
      queryFn: () => service.getUnreadCount(),
      refetchInterval: 15000, // Poll count every 15s as fallback when realtime is disconnected
    });
  };
}
