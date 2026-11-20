import {
  createRoute,
  createRootRouteWithContext,
  notFound,
} from "@tanstack/react-router";
import type { AppRouterContext } from "../context";
import { requireWorkspaceId } from "../guards/require-workspace-membership";
import { RootLayout } from "../../routes/__root";
import { IndexPage } from "../../routes/index";
import { SignInPage } from "../../routes/sign-in";
import { SignUpPage } from "../../routes/sign-up";
import { AuthCallbackPage } from "../../routes/auth-callback";
import { ForgotPasswordPage } from "../../routes/forgot-password";
import { ResetPasswordPage } from "../../routes/reset-password";
import { VerifyEmailPage } from "../../routes/verify-email";
import { HomePage } from "../../routes/home";
import { InvitePage } from "../../routes/invite/$token";
import { InviteLinkPage } from "../../routes/invite";
import { OnboardingPage } from "../../routes/onboarding/onboarding-page";
import { WelcomePage } from "../../routes/onboarding/welcome-page";
import { WorkspaceAddressPage } from "../../routes/workspace-address";
import { WorkspaceLayout } from "../../routes/workspaces/$workspaceId/route";

export const rootRoute = createRootRouteWithContext<AppRouterContext>()({
  component: RootLayout,
});

export const indexRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/",
  component: IndexPage,
});

export const signInRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/sign-in",
  component: SignInPage,
});

export const authCallbackRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/auth/callback",
  component: AuthCallbackPage,
});

export const signUpRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/sign-up",
  component: SignUpPage,
});

export const forgotPasswordRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/forgot-password",
  component: ForgotPasswordPage,
});

export const resetPasswordRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/reset-password",
  component: ResetPasswordPage,
});

export const verifyEmailRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/verify-email",
  component: VerifyEmailPage,
});

export const homeRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/home",
  // `open=default` is set only by a finished sign-in: the launcher then opens the default workspace.
  validateSearch: (search: Record<string, unknown>): { open?: "default" } =>
    search.open === "default" ? { open: "default" } : {},
  component: HomePage,
});

export const inviteLinkRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/invite",
  component: InviteLinkPage,
});

export const inviteRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/invite/$token",
  component: InvitePage,
});

export const onboardingRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/onboarding",
  component: OnboardingPage,
});

export const welcomeRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/welcome/$workspaceId",
  component: WelcomePage,
});

export const workspaceRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/workspaces/$workspaceId",
  component: WorkspaceLayout,
  beforeLoad: ({ params, context }) => {
    const workspaceId = requireWorkspaceId(params);
    context?.services?.lifecycle?.prepareWorkspaceTransition(workspaceId);
  },
});

const ACCOUNT_ID_PATTERN =
  /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

/**
 * A workspace address at the root. TanStack ranks every static route above this one, so
 * `/invite`, `/onboarding`, `/workspaces/...` and the other product routes (the reserved
 * addresses of the Workspaces rules) keep their pages. A path without an Account id is
 * not an address, so any other unknown path keeps the router's page-not-found screen.
 */
export const workspaceAddressRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/$slug",
  validateSearch: (search: Record<string, unknown>): { account: string } => ({
    account: typeof search.account === "string" ? search.account : "",
  }),
  beforeLoad: ({ search }) => {
    if (!ACCOUNT_ID_PATTERN.test(search.account)) throw notFound();
  },
  component: WorkspaceAddressPage,
});
