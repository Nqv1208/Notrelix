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
import { ForgotPasswordPage } from "../../routes/forgot-password";
import { HomePage } from "../../routes/home";
import { InvitePage } from "../../routes/invite/$token";
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

export const workspaceRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/workspaces/$workspaceId",
  component: WorkspaceLayout,
  beforeLoad: ({ params, context }) => {
    const workspaceId = requireWorkspaceId(params);
    context?.services?.lifecycle?.prepareWorkspaceTransition(workspaceId);
  },
});
