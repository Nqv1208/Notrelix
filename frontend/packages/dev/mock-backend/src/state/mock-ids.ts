export const mockIds = {
  users: {
    owner: "mock-user-owner",
    admin: "mock-user-admin",
    member: "mock-user-member",
    viewer: "mock-user-viewer",
    /** Đỗ Khánh Linh: comments on the ERP board (persona `commenter`). */
    commenter: "mock-user-commenter",
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
    erp: "mock-board-erp",
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
