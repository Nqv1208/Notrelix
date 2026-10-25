/**
 * @notrelix/feature-account — Account core types.
 *
 * Framework-neutral: no React, no DOM.
 */

export type {
  AccountActivityEntry,
  AccountActivityPage,
  AccountActivityType,
  AccountLeaveFailure,
  AccountPhotoFailure,
  AccountWorkspaceEntry,
  AccountWorkspaceRole,
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
  AVATAR_MAX_BYTES,
  checkAvatarFile,
  createAccountService,
  leaveFailureOf,
  photoFailureOf,
  toWorkspaceEntry,
  describeDevice,
  type AccountApiClient,
  type AccountEndpoints,
  type AccountService,
} from "./api/account.service";

export {
  PHONE_MAX_DIGITS,
  PHONE_MIN_DIGITS,
  checkPhone,
  phoneForSave,
  type PhoneCheck,
} from "./phone";

export {
  PASSWORD_MIN_LENGTH,
  checkNewPassword,
  passwordChangeFailureOf,
  type NewPasswordProblem,
  type PasswordChangeFailure,
} from "./password";
