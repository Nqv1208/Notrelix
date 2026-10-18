import { useLocale } from "@notrelix/i18n";
import {
  notificationBellCopy,
  type NotificationBellCopy,
} from "./notification-bell.copy";

/** Notification bell copy for the active locale (requires the host `I18nProvider`). */
export function useNotificationBellCopy(): NotificationBellCopy {
  const { locale } = useLocale();
  return notificationBellCopy(locale);
}
