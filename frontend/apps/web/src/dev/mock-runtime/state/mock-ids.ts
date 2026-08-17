export const mockIds = {
  users: {
    owner: "mock-user-owner",
    admin: "mock-user-admin",
    member: "mock-user-member",
    viewer: "mock-user-viewer",
  },
  workspaces: {
    primary: "mock-workspace-primary",
  },
  boards: { roadmap: "mock-board-roadmap" },
  groups: { todo: "mock-group-todo", doing: "mock-group-doing", done: "mock-group-done" },
  cards: { launch: "mock-card-launch", research: "mock-card-research" },
  documents: { productSpec: "mock-doc-product-spec", meetingNotes: "mock-doc-meeting-notes" },
  blocks: { intro: "mock-block-intro", goals: "mock-block-goals" },
  notifications: { mention: "mock-notification-mention", assignment: "mock-notification-assignment" },
  invitations: { primary: "mock-invitation-primary" },
} as const;
