// Centralized permission matrix constants.
// No component should check raw role strings directly. Always use the permission matrix with useCan.

export const permissionValues = [
  "workspace.manage",
  "workspace.member.invite",
  "workspace.member.remove",
  "workspace.settings.update",

  "board.create",
  "board.update",
  "board.delete",
  "board.item.create",
  "board.item.update",
  "board.field.manage",
  "board.view.manage",

  "doc.create",
  "doc.update",
  "doc.delete",
  "doc.share",

  "comment.create",
  "comment.resolve",
  "attachment.upload",

  "billing.manage",
  "governance.role.manage",
  "automation.manage",
  "integration.manage",
] as const

export type Permission = (typeof permissionValues)[number]

type NestedRecord<T> = {
  [key: string]: T | NestedRecord<T>
}

export const permissions = {
  workspace: {
    manage: "workspace.manage" as Permission,
    member: {
      invite: "workspace.member.invite" as Permission,
      remove: "workspace.member.remove" as Permission,
    },
    settings: {
      update: "workspace.settings.update" as Permission,
    },
  },
  board: {
    create: "board.create" as Permission,
    update: "board.update" as Permission,
    delete: "board.delete" as Permission,
    item: {
      create: "board.item.create" as Permission,
      update: "board.item.update" as Permission,
    },
    field: {
      manage: "board.field.manage" as Permission,
    },
    view: {
      manage: "board.view.manage" as Permission,
    },
  },
  doc: {
    create: "doc.create" as Permission,
    update: "doc.update" as Permission,
    delete: "doc.delete" as Permission,
    share: "doc.share" as Permission,
  },
  comment: {
    create: "comment.create" as Permission,
    resolve: "comment.resolve" as Permission,
  },
  attachment: {
    upload: "attachment.upload" as Permission,
  },
  billing: {
    manage: "billing.manage" as Permission,
  },
  governance: {
    role: {
      manage: "governance.role.manage" as Permission,
    },
  },
  automation: {
    manage: "automation.manage" as Permission,
  },
  integration: {
    manage: "integration.manage" as Permission,
  },
} as const satisfies NestedRecord<Permission>
