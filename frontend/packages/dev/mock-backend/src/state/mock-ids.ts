export const mockIds = {
  users: {
    owner: "mock-user-owner",
    admin: "mock-user-admin",
    member: "mock-user-member",
    viewer: "mock-user-viewer",
  },
  workspaces: {
    primary: "mock-workspace-primary",
    secondary: "mock-workspace-secondary",
  },
  views: {
    kanban: "mock-view-kanban",
    table: "mock-view-table",
  },
  boards: {
    roadmap: "mock-board-roadmap",
  },
  spaces: {
    engineering: "mock-space-engineering",
    platform: "mock-space-platform",
    accounting: "mock-space-accounting",
    marketing: "mock-space-marketing",
  },
  teams: {
    helpdesk: "mock-team-helpdesk",
  },
} as const;
