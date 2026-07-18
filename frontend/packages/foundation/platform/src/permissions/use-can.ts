import React, { useContext } from "react"
import { hasPermission, type PermissionResourceContext } from "./ability"
import type { Permission } from "./permissions"
import { PermissionContext } from "./permission-context"

export function PermissionProvider({
  role,
  children,
}: {
  role: string | undefined
  children: React.ReactNode
}) {
  return React.createElement(PermissionContext.Provider, { value: role }, children)
}

export function useCan(permission: Permission, context?: PermissionResourceContext): boolean {
  const role = useContext(PermissionContext)
  return hasPermission(role, permission, context)
}
