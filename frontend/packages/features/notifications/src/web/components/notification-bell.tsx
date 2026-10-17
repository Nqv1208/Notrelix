import { useFormatters } from "@notrelix/i18n";
import { createUseNotifications } from "../query/hooks/use-notifications";
import { createUseUnreadCount } from "../query/hooks/use-unread-count";
import { createUseMarkRead } from "../query/hooks/use-mark-read";
import { createUseArchiveNotification } from "../query/hooks/use-archive-notification";
import type {
  NotificationsApiClient,
  NotificationsEndpoints,
} from "../../core/api/notifications.service";
import { NotificationBellSurface } from "./notification-bell-surface";
import { useNotificationBellCopy } from "./use-notification-bell-copy";

interface CreateNotificationBellDeps {
  api: NotificationsApiClient;
  endpoints: NotificationsEndpoints;
}

export function createNotificationBell(deps: CreateNotificationBellDeps) {
  const useNotifications = createUseNotifications(deps);
  const useUnreadCount = createUseUnreadCount(deps);
  const useMarkRead = createUseMarkRead(deps);
  const useArchiveNotification = createUseArchiveNotification(deps);

  return function NotificationBell() {
    const { data: notifications = [], isLoading: listLoading } =
      useNotifications();
    const { data: countData } = useUnreadCount();
    const { markRead, markAllRead } = useMarkRead();
    const archiveMutation = useArchiveNotification();
    const copy = useNotificationBellCopy();
    const formatters = useFormatters();

    return (
      <NotificationBellSurface
        notifications={notifications}
        unreadCount={countData?.count ?? 0}
        status={listLoading ? "loading" : "idle"}
        copy={copy}
        formatters={formatters}
        onMarkAllRead={() => markAllRead()}
        onMarkRead={(id) => markRead(id)}
        onArchive={(id) => archiveMutation.mutate(id)}
      />
    );
  };
}
