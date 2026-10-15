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
    accounting: "mock-board-accounting",
    /** Kế toán: accounts payable (the Command mockup's KT-91). */
    payables: "mock-board-payables",
    marketing: "mock-board-marketing",
  },
  spaces: {
    engineering: "mock-space-engineering",
    accounting: "mock-space-accounting",
    marketing: "mock-space-marketing",
    hr: "mock-space-hr",
    itInfra: "mock-space-it-infra",
    itApps: "mock-space-it-apps",
    executive: "mock-space-executive",
  },
  teams: {
    helpdesk: "mock-team-helpdesk",
    erpEngineers: "mock-team-erp-engineers",
    executive: "mock-team-executive",
    qa: "mock-team-qa",
  },
} as const;
