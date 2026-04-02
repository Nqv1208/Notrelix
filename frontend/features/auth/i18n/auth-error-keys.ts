export const AUTH_ERROR_KEYS = {
  EMAIL_REQUIRED: "auth.validation.email.required",
  EMAIL_INVALID: "auth.validation.email.invalid",
  PASSWORD_REQUIRED: "auth.validation.password.required",
  PASSWORD_MIN: "auth.validation.password.min",
  CONFIRM_PASSWORD_REQUIRED: "auth.validation.confirmPassword.required",
  CONFIRM_PASSWORD_MISMATCH: "auth.validation.confirmPassword.mismatch",
  FIRST_NAME_REQUIRED: "auth.validation.firstName.required",
  FIRST_NAME_MAX: "auth.validation.firstName.max",
  LAST_NAME_REQUIRED: "auth.validation.lastName.required",
  LAST_NAME_MAX: "auth.validation.lastName.max",
  SERVER_GENERIC: "auth.server.generic",
  LOGIN_INVALID_CREDENTIALS: "auth.server.login.invalidCredentials",
  REGISTER_EMAIL_TAKEN: "auth.server.register.emailTaken",
  ACCOUNT_INACTIVE: "auth.server.account.inactive",
  ACCOUNT_SUSPENDED: "auth.server.account.suspended",
  REFRESH_INVALID: "auth.server.refresh.invalid",
  UNAUTHORIZED: "auth.server.unauthorized",
} as const;

export type AuthErrorKey = (typeof AUTH_ERROR_KEYS)[keyof typeof AUTH_ERROR_KEYS];

export function isAuthErrorKey(value: string): value is AuthErrorKey {
  return Object.values(AUTH_ERROR_KEYS).includes(value as AuthErrorKey);
}
