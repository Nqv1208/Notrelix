export type User = {
  id: string;
  email: string;
  name: string;
  avatarUrl: string | null;
};

export type LoginRequestApi = {
  email: string;
  password: string;
};

/**
 * `AuthSessionResponse`: the session tokens live only in httpOnly cookies, so
 * the body carries the user and, while a second factor is pending, the
 * challenge to answer (`mfaRequired`, `mfaChallengeToken`, `mfaMethod`,
 * `mfaExpiresAt`).
 */
export type LoginResponseApi = {
  expiresAt?: string | null;
  user?: User;
  mfaRequired?: boolean;
  mfaChallengeToken?: string | null;
  mfaMethod?: string | null;
  mfaExpiresAt?: string | null;
};

export type RegisterRequestApi = {
  name: string;
  email: string;
  password: string;
};

export type RegisterResponseApi = LoginResponseApi;

/** Logout and refresh read the cookies; neither sends a token. */
export type LogoutRequest = Record<string, never>;

export type RefreshRequest = Record<string, never>;

export type ForgotPasswordRequest = {
  email: string;
};

export type ResetPasswordRequest = {
  email: string;
  code: string;
  newPassword: string;
};
