import { useQuery } from "@tanstack/react-query";
import {
  createNotificationsService,
  type NotificationsApiClient,
  type NotificationsEndpoints,
} from "../../../core/api/notifications.service";
import { notificationsQueryKeys } from "../../../query/keys";

interface UseUnreadCountDeps {
  api: NotificationsApiClient;
  endpoints: NotificationsEndpoints;
}

export function createUseUnreadCount({ api, endpoints }: UseUnreadCountDeps) {
  const service = createNotificationsService(api, endpoints);
  return function useUnreadCount() {
    return useQuery({
      queryKey: notificationsQueryKeys.unreadCount,
      queryFn: () => service.getUnreadCount(),
      refetchInterval: 15000, // Poll count every 15s as fallback when realtime is disconnected
    });
  };
}
