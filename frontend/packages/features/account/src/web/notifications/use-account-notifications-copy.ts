import { useLocale } from "@notrelix/i18n";
import {
  accountNotificationsCopy,
  type AccountNotificationsCopy,
} from "./account-notifications.copy";

/** Notification preferences copy for the active locale (requires `I18nProvider`). */
export function useAccountNotificationsCopy(): AccountNotificationsCopy {
  return accountNotificationsCopy(useLocale().locale);
}
