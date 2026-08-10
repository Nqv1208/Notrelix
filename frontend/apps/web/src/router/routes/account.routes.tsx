import { createRoute } from "@tanstack/react-router";
import { workspaceRoute } from "./base.routes";
import { AccountLayout } from "../../routes/workspaces/$workspaceId/account";
import { AccountProfilePage } from "../../routes/workspaces/$workspaceId/account/profile";
import { AccountSecurityPage } from "../../routes/workspaces/$workspaceId/account/security";
import { AccountAppearancePage } from "../../routes/workspaces/$workspaceId/account/appearance";
import { AccountNotificationsPage } from "../../routes/workspaces/$workspaceId/account/notifications";

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
  component: AccountSecurityPage,
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
