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

export type LoginResponseApi = {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
  user: User;
};

export type RegisterRequestApi = {
  name: string;
  email: string;
  password: string;
};

export type RegisterResponseApi = LoginResponseApi;

export type LogoutRequest = {
  refreshToken: string;
  accessToken?: string;
};

export type RefreshRequest = Pick<LoginResponseApi, "refreshToken">;

export type ForgotPasswordRequest = {
  email: string;
};

export type ResetPasswordRequest = {
  email: string;
  code: string;
  newPassword: string;
};
