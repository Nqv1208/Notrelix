import { createRoute } from "@tanstack/react-router";
import { z } from "zod";
import { workspaceRoute } from "./base.routes";
import { HomePage } from "../../routes/workspaces/$workspaceId/home";
import { LibraryPage } from "../../routes/workspaces/$workspaceId/library";
import { SettingsPage } from "../../routes/workspaces/$workspaceId/settings";
import { MembersPage } from "../../routes/workspaces/$workspaceId/members";
import { TeamsPage } from "../../routes/workspaces/$workspaceId/teams";
import { TeamPage } from "../../routes/workspaces/$workspaceId/teams.$teamId";
import { MyWorkPage } from "../../routes/workspaces/$workspaceId/my-work";
import { InboxPage } from "../../routes/workspaces/$workspaceId/inbox";
import { ReportsPage } from "../../routes/workspaces/$workspaceId/reports";
import { SpacePage } from "../../routes/workspaces/$workspaceId/spaces/$spaceId";

export const workspaceIndexRoute = createRoute({
  getParentRoute: () => workspaceRoute,
  path: "/",
  component: HomePage,
});

/** The former dashboard path renders Home (WX-HOME-001). */
export const dashboardRoute = createRoute({
  getParentRoute: () => workspaceRoute,
  path: "/dashboard",
  component: HomePage,
});

/** Boards and Docs directory (WX-DIR-001). */
export const libraryRoute = createRoute({
  getParentRoute: () => workspaceRoute,
  path: "/library",
  component: LibraryPage,
});

const membersSearchSchema = z.object({
  tab: z.enum(["members", "invitations"]).optional(),
});

export const settingsRoute = createRoute({
  getParentRoute: () => workspaceRoute,
  path: "/settings",
  component: SettingsPage,
});

export const membersRoute = createRoute({
  getParentRoute: () => workspaceRoute,
  path: "/members",
  validateSearch: (search) => membersSearchSchema.parse(search),
  component: MembersPage,
});

const teamsSearchSchema = z.object({
  filter: z.enum(["active", "archived"]).optional(),
});

/** The workspace's Teams (WX-TEAM-001). */
export const teamsRoute = createRoute({
  getParentRoute: () => workspaceRoute,
  path: "/teams",
  validateSearch: (search) => teamsSearchSchema.parse(search),
  component: TeamsPage,
});

/** One Team (WX-TEAM-001). */
export const teamRoute = createRoute({
  getParentRoute: () => workspaceRoute,
  path: "/teams/$teamId",
  component: TeamPage,
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

/** Reports placeholder until the Reports phase (Final-Space sidebar). */
export const reportsRoute = createRoute({
  getParentRoute: () => workspaceRoute,
  path: "/reports",
  component: ReportsPage,
});

const spaceSearchSchema = z.object({
  tab: z.enum(["overview", "members", "settings"]).optional(),
});

/** A Space of the org tree (OT-5). */
export const spaceRoute = createRoute({
  getParentRoute: () => workspaceRoute,
  path: "/spaces/$spaceId",
  validateSearch: (search) => spaceSearchSchema.parse(search),
  component: SpacePage,
});
