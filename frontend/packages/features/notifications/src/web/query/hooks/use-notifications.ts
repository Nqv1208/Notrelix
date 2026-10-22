import { useAccountId } from "@notrelix/query";
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
    const accountId = useAccountId();
    return useQuery({
      queryKey: notificationsQueryKeys.all(accountId),
      queryFn: () => service.getList(),
    });
  };
}
