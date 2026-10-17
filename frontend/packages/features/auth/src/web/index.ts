/**
 * @notrelix/feature-auth/web — Auth web components and hooks.
 *
 * Uses React hooks and UI components.
 * Depends on auth/core for types and schemas.
 */

export { createUseLogin, type NavigationDeps } from "./hooks/use-login";
export { createUseAuthUser } from "./hooks/use-auth-user";
export { createUseLogout } from "./hooks/use-logout";
export { createUseRegister } from "./hooks/use-register";
export { createUseForgotPassword } from "./hooks/use-forgot-password";
export { createUseResetPassword } from "./hooks/use-reset-password";
export { createRegisterForm } from "./components/register-form";
export {
  createAuthProvider,
  useAuth,
  useCurrentUser,
} from "./components/auth-provider";

// Sign-in pages (the Auth mockup)
export { AuthShell, type AuthShellProps } from "./components/auth-shell";
export {
  AuthSignInSurface,
  type AuthSignInStep,
  type AuthSignInSurfaceProps,
} from "./components/auth-sign-in-surface";
export {
  AuthForgotSurface,
  type AuthForgotStep,
  type AuthForgotSurfaceProps,
} from "./components/auth-forgot-surface";
export {
  AuthInviteSurface,
  type AuthInvite,
  type AuthInviteView,
  type AuthInviteSurfaceProps,
} from "./components/auth-invite-surface";
export { SignInFlow } from "./containers/sign-in-flow";
export { ForgotFlow } from "./containers/forgot-flow";
export { InviteFlow } from "./containers/invite-flow";
export type { AuthFlowNavigation } from "./containers/flow-navigation";
export { authCatalog } from "./copy/auth.catalog";
export { authCopy, type AuthCopy } from "./copy/auth.copy";
export { useAuthCopy } from "./copy/use-auth-copy";
