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

const choice = (inApp: boolean, email: boolean) => ({ inApp, email });

export function notificationValuesFixture(
  overrides: Partial<AccountNotificationValues> = {},
): AccountNotificationValues {
  return {
    version: 3,
    rows: {
      assigned: choice(true, true),
      mention: choice(true, true),
      approval: choice(true, true),
      overdue: choice(true, false),
      watching: choice(true, false),
      invitation: choice(true, true),
      digest: choice(false, false),
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

/** Every channel off for one event, a long Vietnamese name. */
export function notificationsEdgeDataScenario(): NotificationsScenario {
  const values = notificationValuesFixture();
  return {
    ...notificationsDefaultScenario("vi"),
    preferences: {
      status: "ready",
      values: {
        ...values,
        rows: { ...values.rows, assigned: choice(false, false) },
      },
      personName: "Nguyễn Thị Phương Anh Trần Hoàng Bảo Ngọc Lê Văn Đức Minh",
    },
    save: { state: "error" },
  };
}
