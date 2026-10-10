import { createRoute } from "@tanstack/react-router";
import { workspaceRoute } from "./base.routes";
import { DashboardPage } from "../../routes/workspaces/$workspaceId/dashboard";
import { SettingsPage } from "../../routes/workspaces/$workspaceId/settings";
import { MembersPage } from "../../routes/workspaces/$workspaceId/members";
import { MyWorkPage } from "../../routes/workspaces/$workspaceId/my-work";
import { InboxPage } from "../../routes/workspaces/$workspaceId/inbox";

export const workspaceIndexRoute = createRoute({
  getParentRoute: () => workspaceRoute,
  path: "/",
  component: DashboardPage,
});

export const dashboardRoute = createRoute({
  getParentRoute: () => workspaceRoute,
  path: "/dashboard",
  component: DashboardPage,
});

export const settingsRoute = createRoute({
  getParentRoute: () => workspaceRoute,
  path: "/settings",
  component: SettingsPage,
});

export const membersRoute = createRoute({
  getParentRoute: () => workspaceRoute,
  path: "/members",
  component: MembersPage,
});

export const myWorkRoute = createRoute({
  getParentRoute: () => workspaceRoute,
  path: "/my-work",
  component: MyWorkPage,
});

export const inboxRoute = createRoute({
  getParentRoute: () => workspaceRoute,
  path: "/inbox",
  component: InboxPage,
});
