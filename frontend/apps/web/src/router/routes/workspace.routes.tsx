import { createRoute } from "@tanstack/react-router";
import { z } from "zod";
import { workspaceRoute } from "./base.routes";
import { HomePage } from "../../routes/workspaces/$workspaceId/home";
import { LibraryPage } from "../../routes/workspaces/$workspaceId/library";
import { SettingsPage } from "../../routes/workspaces/$workspaceId/settings";
import { MembersPage } from "../../routes/workspaces/$workspaceId/members";
import { MyWorkPage } from "../../routes/workspaces/$workspaceId/my-work";
import { InboxPage } from "../../routes/workspaces/$workspaceId/inbox";

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
