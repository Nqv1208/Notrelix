// Component exports
export { NotificationBell } from "./components/notification-bell"

// Hook exports
export {
  useNotifications,
  useMarkNotificationRead,
  useMarkAllNotificationsRead,
} from "./hooks/use-notifications"

// Service and Type exports
export { notificationsService } from "./api/notifications.service"
export type { UserNotification } from "./api/notifications.service"
