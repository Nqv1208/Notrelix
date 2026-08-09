import { createRoute } from "@tanstack/react-router";
import { workspaceRoute } from "./base.routes";
import { WorkspaceHomePage } from "../../routes/workspaces/$workspaceId/index";
import { DashboardPage } from "../../routes/workspaces/$workspaceId/dashboard";
import { SettingsPage } from "../../routes/workspaces/$workspaceId/settings";
import { MembersPage } from "../../routes/workspaces/$workspaceId/members";

export const workspaceIndexRoute = createRoute({
  getParentRoute: () => workspaceRoute,
  path: "/",
  component: WorkspaceHomePage,
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
