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
} from "./core";

export { createAuthService } from "./core";
export type { AuthApiClient, AuthEndpoints } from "./core";

export {
  loginSchema,
  registerSchema,
  forgotPasswordSchema,
  resetPasswordSchema,
  AUTH_ERROR_KEYS,
} from "./core";

export { parseAuthError, resolveErrorDisplay } from "./core";

// Web
export {
  createUseLogin,
  createUseAuthUser,
  createUseLogout,
  createUseRegister,
  createUseForgotPassword,
  createUseResetPassword,
  createRegisterForm,
  createAuthProvider,
  useAuth,
  useCurrentUser,
} from "./web";

// Sign-in pages (the Auth mockup)
export {
  AuthShell,
  AuthSignInSurface,
  AuthForgotSurface,
  AuthInviteSurface,
  SignInFlow,
  ForgotFlow,
  InviteFlow,
  authCatalog,
  authCopy,
  useAuthCopy,
} from "./web";
export type {
  AuthShellProps,
  AuthSignInStep,
  AuthSignInSurfaceProps,
  AuthForgotStep,
  AuthForgotSurfaceProps,
  AuthInvite,
  AuthInviteView,
  AuthInviteSurfaceProps,
  AuthFlowNavigation,
  AuthCopy,
} from "./web";
export { createAuthFlowService, AUTH_FLOW_PATHS, readFlowError } from "./core";
export type {
  AuthFlowApiClient,
  AuthFlowService,
  IdentifyResult,
  InvitePreview,
  SessionResult,
  LoginResult,
  FlowError,
} from "./core";
