/**
 * @notrelix/feature-notifications — Notifications core types.
 *
 * Framework-neutral: no React, no DOM.
 */

export type {
  NotificationType,
  Notification,
  NotificationPreferences,
} from './types/notifications';

export { createNotificationsService, type NotificationsApiClient, type NotificationsEndpoints } from './api/notifications.service';
export * from './query';
