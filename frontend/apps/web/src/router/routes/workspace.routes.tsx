import { createRoute } from "@tanstack/react-router";
import { z } from "zod";
import { workspaceRoute } from "./base.routes";
import { HomePage } from "../../routes/workspaces/$workspaceId/home";
import { LibraryPage } from "../../routes/workspaces/$workspaceId/library";
import { ArchivedBoardsPage } from "../../routes/workspaces/$workspaceId/archived-boards";
import { SettingsPage } from "../../routes/workspaces/$workspaceId/settings";
import { MembersPage } from "../../routes/workspaces/$workspaceId/members";
import { TeamsPage } from "../../routes/workspaces/$workspaceId/teams";
import { TeamPage } from "../../routes/workspaces/$workspaceId/teams.$teamId";
import { MyWorkPage } from "../../routes/workspaces/$workspaceId/my-work";
import { myWorkSearchSchema } from "../schemas/my-work-search-schema";
import { InboxPage } from "../../routes/workspaces/$workspaceId/inbox";
import { reportsSearchSchema } from "../schemas/reports-search-schema";
import { ReportsPage } from "../../routes/workspaces/$workspaceId/reports";
import { SpacePage } from "../../routes/workspaces/$workspaceId/spaces/$spaceId";
import { SecurityPage } from "../../routes/workspaces/$workspaceId/security";

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

const archivedBoardsSearchSchema = z.object({
  tab: z.enum(["archived", "deleted"]).optional(),
});

/** Archived boards (unarchive) and deleted boards inside their 30-day window (restore). */
export const archivedBoardsRoute = createRoute({
  getParentRoute: () => workspaceRoute,
  path: "/archived-boards",
  validateSearch: (search) => archivedBoardsSearchSchema.parse(search),
  component: function ArchivedBoardsRouteComponent() {
    const { tab } = archivedBoardsRoute.useSearch();
    return <ArchivedBoardsPage tab={tab} />;
  },
});

const membersSearchSchema = z.object({
  tab: z.enum(["members", "invitations"]).optional(),
  invite: z.literal("seats").optional(),
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
  validateSearch: (search) => myWorkSearchSchema.parse(search),
  component: MyWorkPage,
});

export const inboxRoute = createRoute({
  getParentRoute: () => workspaceRoute,
  path: "/inbox",
  component: InboxPage,
});

/** Reports (Final-Reports): the list, or one report named by `report`. */
export const reportsRoute = createRoute({
  getParentRoute: () => workspaceRoute,
  path: "/reports",
  validateSearch: (search) => reportsSearchSchema.parse(search),
  component: ReportsPage,
});

const spaceSearchSchema = z.object({
  tab: z.enum(["overview", "members", "settings"]).optional(),
  /** `member`: a manager previews the page as a department member. */
  as: z.enum(["member"]).optional(),
});

/** A Space of the org tree (OT-5). */
export const spaceRoute = createRoute({
  getParentRoute: () => workspaceRoute,
  path: "/spaces/$spaceId",
  validateSearch: (search) => spaceSearchSchema.parse(search),
  component: SpacePage,
});

/** Sharing and security of the workspace (Final-Teams). */
export const securityRoute = createRoute({
  getParentRoute: () => workspaceRoute,
  path: "/security",
  component: SecurityPage,
});
