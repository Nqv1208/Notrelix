import React from "react"
import { useCan } from "./use-can"
import type { Permission } from "./permissions"
import type { PermissionResourceContext } from "./ability"

interface PermissionGuardProps {
  permission: Permission
  context?: PermissionResourceContext
  fallback?: React.ReactNode
  children: React.ReactNode
}

export function PermissionGuard({
  permission,
  context,
  fallback = null,
  children,
}: PermissionGuardProps) {
  const hasAccess = useCan(permission, context)

  if (!hasAccess) {
    return React.createElement(React.Fragment, null, fallback)
  }

  return React.createElement(React.Fragment, null, children)
}
