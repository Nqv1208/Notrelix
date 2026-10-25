/**
 * @notrelix/feature-notifications — Notifications core types.
 *
 * Framework-neutral: no React, no DOM.
 */

export { NOTIFICATION_PREFERENCE_TYPES } from "./types/notifications";
export type {
  NotificationType,
  Notification,
  NotificationChannelChoice,
  NotificationPreferenceType,
  NotificationPreferences,
} from "./types/notifications";

export {
  createNotificationsService,
  type NotificationsApiClient,
  type NotificationsEndpoints,
  toPreferences,
} from "./api/notifications.service";
