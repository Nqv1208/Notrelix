export const mockIds = {
  users: {
    owner: "mock-user-owner",
    admin: "mock-user-admin",
    member: "mock-user-member",
    viewer: "mock-user-viewer",
  },
  workspaces: {
    primary: "dev-workspace",
    secondary: "dev-workspace-secondary",
  },
  views: {
    kanban: "dev-view-kanban",
    table: "dev-view-table",
  },
  boards: {
    roadmap: "dev-board-roadmap",
  },
} as const;
