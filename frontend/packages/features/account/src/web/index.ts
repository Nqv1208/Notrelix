/**
 * @notrelix/features-account — Web hooks and components.
 */

export { createUseUpdateProfile } from "./hooks/use-update-profile";
export { createUseProfile } from "./query/hooks/use-profile";
export {
  createUsePreferences,
  isPreferencesConflict,
} from "./query/hooks/use-preferences";
export { createUseSecurity } from "./query/hooks/use-security";
export { createUseAccountWorkspaces } from "./query/hooks/use-account-workspaces";
export { createUseProfilePhoto } from "./query/hooks/use-profile-photo";

export { accountProfileCatalog } from "./profile/account-profile.catalog";
export {
  accountProfileCopy,
  type AccountProfileCopy,
} from "./profile/account-profile.copy";
export { useAccountProfileCopy } from "./profile/use-account-profile-copy";
export {
  AccountProfileSurface,
  type AccountProfileData,
  type AccountProfileSurfaceProps,
  type AccountProfileValues,
} from "./profile/account-profile-surface";
export {
  pluralCopy,
  type AccountFormatters,
  type AccountSaveStatus,
  type AccountSection,
} from "./shared/account-surface-model";
export { accountNavCatalog } from "./nav/account-nav.catalog";
export { accountNavCopy, type AccountNavCopy } from "./nav/account-nav.copy";
export { useAccountNavCopy } from "./nav/use-account-nav-copy";

export { accountAppearanceCatalog } from "./appearance/account-appearance.catalog";
export {
  accountAppearanceCopy,
  type AccountAppearanceCopy,
} from "./appearance/account-appearance.copy";
export { useAccountAppearanceCopy } from "./appearance/use-account-appearance-copy";
export {
  AccountAppearanceSurface,
  type AccountAppearanceSurfaceProps,
  type AccountSidebarPreference,
} from "./appearance/account-appearance-surface";
export type { AccountLocale } from "./shared/account-surface-model";

export { accountSecurityCatalog } from "./security/account-security.catalog";
export {
  accountSecurityCopy,
  type AccountSecurityCopy,
} from "./security/account-security.copy";
export { useAccountSecurityCopy } from "./security/use-account-security-copy";
export {
  AccountSecuritySurface,
  relativeAge,
  type AccountActivityView,
  type AccountPasswordError,
  type AccountPasswordSubmission,
  type AccountSecurityData,
  type AccountSecurityFlow,
  type AccountSecuritySurfaceProps,
  type AccountSessionView,
} from "./security/account-security-surface";

export { accountNotificationsCatalog } from "./notifications/account-notifications.catalog";
export {
  accountNotificationsCopy,
  type AccountNotificationsCopy,
} from "./notifications/account-notifications.copy";
export { useAccountNotificationsCopy } from "./notifications/use-account-notifications-copy";
export {
  AccountNotificationsSurface,
  NOTIFICATION_CHANNELS,
  NOTIFICATION_EVENTS,
  type AccountChannelChoice,
  type AccountNotificationChannel,
  type AccountNotificationEvent,
  type AccountNotificationsState,
  type AccountNotificationsSurfaceProps,
  type AccountNotificationValues,
} from "./notifications/account-notifications-surface";

export { accountWorkspacesCatalog } from "./workspaces/account-workspaces.catalog";
export {
  accountWorkspacesCopy,
  type AccountWorkspacesCopy,
} from "./workspaces/account-workspaces.copy";
export { useAccountWorkspacesCopy } from "./workspaces/use-account-workspaces-copy";
export {
  AccountWorkspacesSurface,
  type AccountWorkspaceTone,
  type AccountWorkspaceView,
  type DeletedWorkspaceView,
  type AccountWorkspacesSurfaceProps,
} from "./workspaces/account-workspaces-surface";

export {
  AccountFrame,
  type AccountFrameLink,
  type AccountFrameProps,
  type AccountTab,
} from "./frame/account-frame";
