import type {
  ForgotPasswordRequest,
  LoginRequestApi,
  LoginResponseApi,
  LogoutRequest,
  RefreshRequest,
  RegisterRequestApi,
  RegisterResponseApi,
  ResetPasswordRequest,
  User,
} from '../types/auth';

export interface AuthApiClient {
  get<T>(url: string): Promise<T>;
  post<T>(url: string, body: unknown): Promise<T>;
}

export interface AuthEndpoints {
  auth: {
    login: string;
    register: string;
    logout: string;
    refresh: string;
    forgotPassword: string;
    resetPassword: string;
    profile: string;
  };
}

export function createAuthService(
  api: AuthApiClient,
  endpoints: AuthEndpoints,
) {
  return {
    login(data: LoginRequestApi) {
      return api.post<LoginResponseApi>(endpoints.auth.login, data);
    },

    register(data: RegisterRequestApi) {
      return api.post<RegisterResponseApi>(endpoints.auth.register, data);
    },

    logout(data: LogoutRequest) {
      return api.post<void>(endpoints.auth.logout, data);
    },

    refresh(data: RefreshRequest) {
      return api.post<LoginResponseApi>(endpoints.auth.refresh, data);
    },

    forgotPassword(data: ForgotPasswordRequest) {
      return api.post<void>(endpoints.auth.forgotPassword, data);
    },

    resetPassword(data: ResetPasswordRequest) {
      return api.post<void>(endpoints.auth.resetPassword, data);
    },

    profile() {
      return api.get<User>(endpoints.auth.profile);
    },
  };
}
