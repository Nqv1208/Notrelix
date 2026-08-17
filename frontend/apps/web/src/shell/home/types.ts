import type { WorkspaceSummary } from "@notrelix/features-workspace/core";

export interface HomeSidebarResource {
  readonly id: string;
  readonly title: string;
  readonly workspaceId: string;
}

export interface HomeSidebarData {
  readonly workspaces: readonly WorkspaceSummary[];
  readonly favoriteDocs: readonly HomeSidebarResource[];
  readonly recentDocs: readonly HomeSidebarResource[];
  readonly recentBoards: readonly HomeSidebarResource[];
}
