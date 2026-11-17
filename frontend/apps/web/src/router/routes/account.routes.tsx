import { createRoute } from "@tanstack/react-router";
import { rootRoute, workspaceRoute } from "./base.routes";
import { AccountLayout } from "../../routes/workspaces/$workspaceId/account";
import { AccountProfilePage } from "../../routes/workspaces/$workspaceId/account/profile";
import { AccountSecurityPage } from "../../routes/workspaces/$workspaceId/account/security";
import { AccountProvidersPage } from "../../routes/workspaces/$workspaceId/account/providers";
import { AccountEmailPage } from "../../routes/workspaces/$workspaceId/account/email";
import { AccountTokensPage } from "../../routes/workspaces/$workspaceId/account/tokens";
import { AccountAppearancePage } from "../../routes/workspaces/$workspaceId/account/appearance";
import { AccountNotificationsPage } from "../../routes/workspaces/$workspaceId/account/notifications";
import { AccountOutsideWorkspacePage } from "../../routes/account/workspaces";
import { AccountWorkspacesPage } from "../../routes/workspaces/$workspaceId/account/workspaces";

export const accountRoute = createRoute({
  getParentRoute: () => workspaceRoute,
  path: "/account",
  component: AccountLayout,
});

export const accountProfileRoute = createRoute({
  getParentRoute: () => accountRoute,
  path: "/profile",
  component: AccountProfilePage,
});

export const accountSecurityRoute = createRoute({
  getParentRoute: () => accountRoute,
  path: "/security",
  // Back from a provider re-authentication: which action asked for it, and whether the provider refused.
  validateSearch: (
    search: Record<string, unknown>,
  ): { stepUp?: string; stepUpResult?: "failed" } => ({
    ...(typeof search.stepUp === "string" ? { stepUp: search.stepUp } : {}),
    ...(search.stepUpResult === "failed" ? { stepUpResult: "failed" } : {}),
  }),
  component: AccountSecurityPage,
});

export const accountEmailRoute = createRoute({
  getParentRoute: () => accountRoute,
  path: "/email",
  // Back from a provider re-authentication: the address being confirmed (not a secret), and whether the provider refused.
  validateSearch: (
    search: Record<string, unknown>,
  ): { stepUp?: string; stepUpResult?: "failed"; email?: string } => ({
    ...(typeof search.stepUp === "string" ? { stepUp: search.stepUp } : {}),
    ...(search.stepUpResult === "failed" ? { stepUpResult: "failed" } : {}),
    ...(typeof search.email === "string" ? { email: search.email } : {}),
  }),
  component: AccountEmailPage,
});

export const accountProvidersRoute = createRoute({
  getParentRoute: () => accountRoute,
  path: "/providers",
  // Back from a provider: its re-authentication (`stepUp`, `provider`, `stepUpResult`) or a link's end (`link`).
  validateSearch: (
    search: Record<string, unknown>,
  ): {
    stepUp?: string;
    stepUpResult?: "failed";
    provider?: string;
    link?: string;
  } => ({
    ...(typeof search.stepUp === "string" ? { stepUp: search.stepUp } : {}),
    ...(search.stepUpResult === "failed" ? { stepUpResult: "failed" } : {}),
    ...(typeof search.provider === "string"
      ? { provider: search.provider }
      : {}),
    ...(typeof search.link === "string" ? { link: search.link } : {}),
  }),
  component: AccountProvidersPage,
});

export const accountAppearanceRoute = createRoute({
  getParentRoute: () => accountRoute,
  path: "/appearance",
  component: AccountAppearancePage,
});

export const accountNotificationsRoute = createRoute({
  getParentRoute: () => accountRoute,
  path: "/notifications",
  component: AccountNotificationsPage,
});

export const accountTokensRoute = createRoute({
  getParentRoute: () => accountRoute,
  path: "/tokens",
  // Back from a provider re-authentication: the token being issued, and whether the provider refused.
  validateSearch: (
    search: Record<string, unknown>,
  ): {
    stepUp?: string;
    stepUpResult?: "failed";
    name?: string;
    days?: number;
  } => ({
    ...(typeof search.stepUp === "string" ? { stepUp: search.stepUp } : {}),
    ...(search.stepUpResult === "failed" ? { stepUpResult: "failed" } : {}),
    ...(typeof search.name === "string" ? { name: search.name } : {}),
    ...(Number.isFinite(Number(search.days))
      ? { days: Number(search.days) }
      : {}),
  }),
  component: AccountTokensPage,
});

export const accountWorkspacesRoute = createRoute({
  getParentRoute: () => accountRoute,
  path: "/workspaces",
  component: AccountWorkspacesPage,
});

/** The same page outside a workspace, for a person who has none left. */
export const accountOutsideWorkspaceRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/account/workspaces",
  component: AccountOutsideWorkspacePage,
});
