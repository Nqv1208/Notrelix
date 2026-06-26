// Centralized permission matrix constants.
// No component should check raw role strings directly. Always use the permission matrix with useCan.

export const permissions = {
  workspace: {
    manage: "workspace.manage",
    member: {
      invite: "workspace.member.invite",
      remove: "workspace.member.remove",
    },
    settings: {
      update: "workspace.settings.update",
    },
  },
  board: {
    create: "board.create",
    update: "board.update",
    delete: "board.delete",
    item: {
      create: "board.item.create",
      update: "board.item.update",
    },
    field: {
      manage: "board.field.manage",
    },
    view: {
      manage: "board.view.manage",
    },
  },
  doc: {
    create: "doc.create",
    update: "doc.update",
    delete: "doc.delete",
    share: "doc.share",
  },
  comment: {
    create: "comment.create",
    resolve: "comment.resolve",
  },
  attachment: {
    upload: "attachment.upload",
  },
  billing: {
    manage: "billing.manage",
  },
  governance: {
    role: {
      manage: "governance.role.manage",
    },
  },
  automation: {
    manage: "automation.manage",
  },
  integration: {
    manage: "integration.manage",
  },
} as const

export type Permission = typeof permissions[keyof typeof permissions]
