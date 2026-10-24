/**
 * @notrelix/feature-account — Account core types.
 *
 * Framework-neutral: no React, no DOM.
 */

export type {
  AccountActivityEntry,
  AccountActivityPage,
  AccountActivityType,
  AccountDeviceKind,
  AccountPreferences,
  AccountPreferencesPatch,
  AccountSecuritySummary,
  AccountSession,
  AccountTheme,
  MyMemberProfile,
  StepUpFactor,
  StepUpPurpose,
  StepUpRequirement,
  TwoFactorEnrollment,
  UserProfile,
} from "./types/account";

export {
  createAccountService,
  describeDevice,
  type AccountApiClient,
  type AccountEndpoints,
  type AccountService,
} from "./api/account.service";
