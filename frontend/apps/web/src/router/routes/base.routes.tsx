import {
  createRoute,
  createRootRouteWithContext,
} from "@tanstack/react-router";
import type { AppRouterContext } from "../context";
import { requireWorkspaceId } from "../guards/require-workspace-membership";
import { RootLayout } from "../../routes/__root";
import { IndexPage } from "../../routes/index";
import { SignInPage } from "../../routes/sign-in";
import { SignUpPage } from "../../routes/sign-up";
import { AuthCallbackPage } from "../../routes/auth-callback";
import { ForgotPasswordPage } from "../../routes/forgot-password";
import { HomePage } from "../../routes/home";
import { InvitePage } from "../../routes/invite/$token";
import { OnboardingPage } from "../../routes/onboarding/onboarding-page";
import { WelcomePage } from "../../routes/onboarding/welcome-page";
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

export const homeRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/home",
  component: HomePage,
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
