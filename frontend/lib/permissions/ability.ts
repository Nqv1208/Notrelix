// Permission evaluator helper.
// Evaluates permissions based on the active member role in a workspace.

export type UserRole = "owner" | "admin" | "member" | "viewer"

const ROLE_PERMISSIONS: Record<UserRole, string[]> = {
  owner: [
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
    "integration.manage"
  ],
  admin: [
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
    "automation.manage",
    "integration.manage"
  ],
  member: [
    "board.item.create",
    "board.item.update",
    "board.view.manage",
    "doc.create",
    "doc.update",
    "doc.share",
    "comment.create",
    "comment.resolve",
    "attachment.upload"
  ],
  viewer: [
    "comment.create"
  ]
}

export function hasPermission(role: string | undefined, permission: string): boolean {
  if (!role) return false
  const normalizedRole = role.toLowerCase() as UserRole
  const permissions = ROLE_PERMISSIONS[normalizedRole]
  if (!permissions) return false
  return permissions.includes(permission)
}
