/**
 * @notrelix/feature-auth/web — Auth web components and hooks.
 *
 * Uses React hooks and UI components.
 * Depends on auth/core for types and schemas.
 */

export { createUseLogin } from './hooks/use-login';
export { createUseAuthUser } from './hooks/use-auth-user';
export { createUseLogout } from './hooks/use-logout';
export { createUseRegister } from './hooks/use-register';
export { createUseForgotPassword } from './hooks/use-forgot-password';
export { createUseResetPassword } from './hooks/use-reset-password';
export { createLoginForm } from './components/login-form';
export { createAuthProvider, useAuth, useCurrentUser } from './components/auth-provider';
