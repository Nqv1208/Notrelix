/**
 * @notrelix/features-account — Web hooks and components.
 */

export { createUseUpdateProfile } from "./hooks/use-update-profile";
export { createUseUpdatePreferences } from "./hooks/use-update-preferences";
export { createUseProfile } from "./query/hooks/use-profile";
export { createUsePreferences } from "./query/hooks/use-preferences";
export { createUseSecuritySettings } from "./query/hooks/use-security-settings";

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
export type {
  AccountFormatters,
  AccountSaveStatus,
  AccountSection,
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
