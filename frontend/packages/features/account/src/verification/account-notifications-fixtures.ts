/**
 * Deterministic fixtures for the account notification preferences page.
 */
import {
  accountNotificationsCopy,
  type AccountNotificationsCopy,
} from "../web/notifications/account-notifications.copy";
import type {
  AccountNotificationValues,
  AccountNotificationsSurfaceProps,
} from "../web/notifications/account-notifications-surface";

export function notificationsCopyFixture(
  locale: "en" | "vi" = "en",
): AccountNotificationsCopy {
  return accountNotificationsCopy(locale);
}

export const NOTIFICATION_DEFAULTS: AccountNotificationValues = {
  emailEnabled: true,
  pushEnabled: true,
};

export type NotificationsScenario = Omit<
  AccountNotificationsSurfaceProps,
  "onSave" | "onRetry"
>;

export function notificationsDefaultScenario(
  locale: "en" | "vi" = "en",
): NotificationsScenario {
  return {
    copy: notificationsCopyFixture(locale),
    preferences: {
      status: "ready",
      values: { emailEnabled: true, pushEnabled: false },
    },
    save: { state: "idle" },
  };
}

export function notificationsLoadingScenario(
  locale: "en" | "vi" = "en",
): NotificationsScenario {
  return {
    ...notificationsDefaultScenario(locale),
    preferences: { status: "loading" },
  };
}

/** The saved preferences could not be read: the defaults are shown. */
export function notificationsErrorScenario(
  locale: "en" | "vi" = "en",
): NotificationsScenario {
  return {
    ...notificationsDefaultScenario(locale),
    preferences: { status: "error", fallback: NOTIFICATION_DEFAULTS },
  };
}

export function notificationsSavingScenario(
  locale: "en" | "vi" = "en",
): NotificationsScenario {
  return { ...notificationsDefaultScenario(locale), save: { state: "saving" } };
}

export function notificationsSavedScenario(
  locale: "en" | "vi" = "en",
): NotificationsScenario {
  return { ...notificationsDefaultScenario(locale), save: { state: "saved" } };
}

export function notificationsSaveErrorScenario(
  locale: "en" | "vi" = "en",
): NotificationsScenario {
  return { ...notificationsDefaultScenario(locale), save: { state: "error" } };
}

/** Vietnamese with every channel off and a failed save. */
export function notificationsEdgeDataScenario(): NotificationsScenario {
  return {
    ...notificationsDefaultScenario("vi"),
    preferences: {
      status: "ready",
      values: { emailEnabled: false, pushEnabled: false },
    },
    save: { state: "error" },
  };
}
