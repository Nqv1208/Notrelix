import type { Permission } from "./permissions"

export type UserRole = "owner" | "admin" | "member" | "viewer"

export type PermissionResourceContext = {
  workspaceId?: string
  resourceType?:
    | "workspace"
    | "board"
    | "item"
    | "field"
    | "doc"
    | "comment"
    | "billing"
    | "automation"
    | "integration"
  resourceId?: string
  targetUserId?: string
}

const ROLE_PERMISSIONS: Record<UserRole, Permission[]> = {
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

export function hasPermission(
  role: string | undefined,
  permission: Permission,
  context?: PermissionResourceContext
): boolean {
  if (!role) return false
  
  // Normalize roles: guest role maps to viewer permissions
  let normalizedRole = role.toLowerCase().trim()
  if (normalizedRole === "guest") {
    normalizedRole = "viewer"
  }
  
  const permissionsList = ROLE_PERMISSIONS[normalizedRole as UserRole]
  if (!permissionsList) return false
  
  const baseAllowed = permissionsList.includes(permission)
  if (!baseAllowed) return false

  // Resource context rules (hardening)
  if (context) {
    // Last-owner rules: cannot remove owner
    if (permission === "workspace.member.remove" && context.targetUserId === "owner") {
      return false
    }
  }

  return true
}
