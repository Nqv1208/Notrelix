import { redirect } from "@tanstack/react-router";
import { RouteGuardError } from "./errors";

const WORKSPACE_ID_PATTERN = /^[A-Za-z0-9][A-Za-z0-9_-]{1,127}$/;

export function requireWorkspaceId(params: { workspaceId?: string }): string {
  const workspaceId = params.workspaceId?.trim();
  if (!workspaceId || !WORKSPACE_ID_PATTERN.test(workspaceId)) {
    throw redirect({ to: "/home", replace: true });
  }
  return workspaceId;
}

export function requireWorkspaceMembership(input: {
  readonly workspaceId: string;
  readonly memberWorkspaceIds?: readonly string[];
}): void {
  if (input.memberWorkspaceIds?.includes(input.workspaceId)) return;
  throw new RouteGuardError(
    "not-member",
    "Workspace membership is required.",
    404,
  );
}
