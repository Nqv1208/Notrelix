/**
 * @notrelix/feature-auth — Auth core types, schemas, API contracts, and utilities.
 *
 * Framework-neutral: no React, no DOM, no Next.js.
 */

// Types
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
} from './types/auth';

// API
export { createAuthService } from './api/auth.service';
export type { AuthApiClient, AuthEndpoints } from './api/auth.service';

// Schemas
export {
  loginSchema,
  registerSchema,
  forgotPasswordSchema,
  resetPasswordSchema,
  AUTH_ERROR_KEYS,
} from './schemas';
export type { LoginRequest } from './schemas/login.schema';
export type { RegisterRequest } from './schemas/register.schema';
export type { ForgotPasswordRequest as ForgotPasswordFormRequest } from './schemas/forgot-password.schema';
export type { ResetPasswordRequest as ResetPasswordFormRequest } from './schemas/reset-password.schema';

// Utils
export { parseAuthError, resolveErrorDisplay } from './utils';
export type { ParsedAuthError } from './utils/parse-auth-error';
