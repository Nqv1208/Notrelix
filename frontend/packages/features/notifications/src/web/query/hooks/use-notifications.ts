import { useQuery } from "@tanstack/react-query";
import {
  createNotificationsService,
  type NotificationsApiClient,
  type NotificationsEndpoints,
} from "../../../core/api/notifications.service";
import { notificationsQueryKeys } from "../../../query/keys";

interface UseNotificationsDeps {
  api: NotificationsApiClient;
  endpoints: NotificationsEndpoints;
}

export function createUseNotifications({
  api,
  endpoints,
}: UseNotificationsDeps) {
  const service = createNotificationsService(api, endpoints);
  return function useNotifications() {
    return useQuery({
      queryKey: notificationsQueryKeys.all,
      queryFn: () => service.getList(),
    });
  };
}
