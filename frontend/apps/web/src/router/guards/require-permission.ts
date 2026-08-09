import type {
  Permission,
  PermissionResourceContext,
} from "@notrelix/platform/permissions-core";
import { RouteGuardError } from "./errors";

export function requirePermission(input: {
  readonly permission: Permission;
  readonly context?: PermissionResourceContext;
  readonly can: (
    permission: Permission,
    context?: PermissionResourceContext,
  ) => boolean;
}): void {
  if (input.can(input.permission, input.context)) return;
  throw new RouteGuardError("forbidden", "Permission is required.", 403);
}
