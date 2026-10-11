import { IDENTITY_TONES, identityToneFor } from "../../core/model/navigation";
import type { WorkspaceSummary } from "../../core/types/workspace";
import { IDENTITY_MARK_CLASS } from "../shell/shell-models";

/** Identity mark classes for a stack of avatars, one tone per position. */
export const avatarColors: readonly string[] = IDENTITY_TONES.map(
  (tone) => IDENTITY_MARK_CLASS[tone],
);

/** Identity mark classes for a workspace tile; stable for a workspace id. */
export function markClassForWorkspace(id: string): string {
  return IDENTITY_MARK_CLASS[identityToneFor(id)];
}

export function formatWorkspacePlan(workspace: WorkspaceSummary) {
  if (workspace.isPersonal) return "Personal";
  return workspace.plan.charAt(0).toUpperCase() + workspace.plan.slice(1);
}

export function getInitials(name: string) {
  return name
    .split(/\s+/)
    .map((part) => part[0])
    .join("")
    .toUpperCase()
    .slice(0, 2);
}

export function formatDateLabel(value: string): string {
  return value.slice(0, 10);
}
