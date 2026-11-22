import type {
  ForgotPasswordRequest,
  LoginRequestApi,
  LoginResponseApi,
  LogoutRequest,
  RefreshRequest,
  ResetPasswordRequest,
  User,
} from "../types/auth";

export interface AuthApiClient {
  get<T>(url: string): Promise<T>;
  post<T>(url: string, body: unknown): Promise<T>;
}

export interface AuthEndpoints {
  auth: {
    login: string;
    logout: string;
    refresh: string;
    forgotPassword: string;
    resetPassword: string;
    profile: string;
    bootstrap: string;
  };
}

/** The part of `GET /auth/bootstrap` that names the member's own Account (null: they belong to none). */
export interface BootstrapAccount {
  readonly accountId: string | null;
}

export function createAuthService(
  api: AuthApiClient,
  endpoints: AuthEndpoints,
) {
  return {
    login(data: LoginRequestApi) {
      return api.post<LoginResponseApi>(endpoints.auth.login, data);
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

    async bootstrapAccount(): Promise<BootstrapAccount> {
      const bootstrap = await api.get<{ accountId?: string | null }>(
        endpoints.auth.bootstrap,
      );
      return { accountId: bootstrap?.accountId || null };
    },
  };
}
