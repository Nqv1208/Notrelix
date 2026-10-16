/**
 * Deterministic fixtures for the account notifications page.
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

const choice = (app: boolean, email: boolean, mobile: boolean) => ({
  app,
  email,
  mobile,
});

export function notificationValuesFixture(
  overrides: Partial<AccountNotificationValues> = {},
): AccountNotificationValues {
  return {
    rows: {
      assigned: choice(true, true, true),
      mention: choice(true, true, true),
      approval: choice(true, true, true),
      overdue: choice(true, true, false),
      watching: choice(true, false, false),
      invitation: choice(true, true, false),
      digest: choice(false, true, false),
    },
    quietHours: {
      enabled: true,
      from: "21:00",
      to: "07:00",
      days: "weekdays",
    },
    ...overrides,
  };
}

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
      values: notificationValuesFixture(),
      personName: "Ada Lovelace",
      zoneLabel: "GMT+7",
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

export function notificationsErrorScenario(
  locale: "en" | "vi" = "en",
): NotificationsScenario {
  return {
    ...notificationsDefaultScenario(locale),
    preferences: { status: "error" },
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

/** Quiet hours off, every channel off for one event, a long Vietnamese name. */
export function notificationsEdgeDataScenario(): NotificationsScenario {
  const values = notificationValuesFixture();
  return {
    ...notificationsDefaultScenario("vi"),
    preferences: {
      status: "ready",
      values: {
        rows: { ...values.rows, assigned: choice(false, false, false) },
        quietHours: { ...values.quietHours, enabled: false },
      },
      personName: "Nguyễn Thị Phương Anh Trần Hoàng Bảo Ngọc Lê Văn Đức Minh",
      zoneLabel: "GMT+7",
    },
    save: { state: "error" },
  };
}
