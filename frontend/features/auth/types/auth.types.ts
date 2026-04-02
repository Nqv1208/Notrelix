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

export type LogoutRequest = Pick<LoginResponseApi, "refreshToken">;

export type RefreshRequest = Pick<LoginResponseApi, "refreshToken">;