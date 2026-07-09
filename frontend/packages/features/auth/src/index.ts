/**
 * @notrelix/feature-auth — Auth feature package.
 *
 * Core types, schemas, and utilities are framework-neutral.
 * Web components and hooks use React.
 */

// Core
export type {
  User,
  LoginRequestApi,
  LoginResponseApi,
  RegisterRequestApi,
  RegisterResponseApi,
  LogoutRequest,
  RefreshRequest,
  ForgotPasswordRequest,
  ResetPasswordRequest,
} from './core';

export { createAuthService } from './core';
export type { AuthApiClient, AuthEndpoints } from './core';

export {
  loginSchema,
  registerSchema,
  forgotPasswordSchema,
  resetPasswordSchema,
  AUTH_ERROR_KEYS,
} from './core';

export { parseAuthError, resolveErrorDisplay } from './core';

// Web
export {
  createUseLogin,
  createUseAuthUser,
  createUseLogout,
  createUseRegister,
  createUseForgotPassword,
  createUseResetPassword,
  createLoginForm,
  createAuthProvider,
  useAuth,
  useCurrentUser,
} from './web';
