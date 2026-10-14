import type {
  BoardAccessLevel,
  KanbanStatusManageRole,
} from "@notrelix/work-management-core";
import type { TaskDetailCapabilities } from "../card-detail/task-detail-types";

const FULL: TaskDetailCapabilities = {
  canCreateCard: true,
  canMoveCard: true,
  canRenameGroup: true,
  canDelete: true,
  canEditFields: true,
  canComment: true,
};

const READ_ONLY: TaskDetailCapabilities = {
  canCreateCard: false,
  canMoveCard: false,
  canRenameGroup: false,
  canDelete: false,
  canEditFields: false,
  canComment: false,
};

/**
 * What the viewer may do on the board, from their effective level on the
 * board access read. Editors and managers edit; a commenter only comments;
 * a viewer, and anyone while the level is not known yet, is read-only. There
 * is no edit default: the server decides every write, and this only shapes
 * the controls.
 */
export function boardCapabilities(
  level: BoardAccessLevel | null | undefined,
): TaskDetailCapabilities {
  if (level === "editor" || level === "manager") return FULL;
  if (level === "commenter") return { ...READ_ONLY, canComment: true };
  return READ_ONLY;
}

/**
 * Whether the Kanban shows the add / edit status controls: the viewer can edit
 * the board AND their level is one of the view's `statusManageRoles`. This only
 * shapes the controls; the server still decides every write, so a hidden
 * control is not a permission and a shown one is not a grant.
 */
export function canManageStatuses(
  level: BoardAccessLevel | null | undefined,
  roles: readonly KanbanStatusManageRole[],
): boolean {
  if (!boardCapabilities(level).canEditFields) return false;
  return (
    (level === "manager" && roles.includes("manager")) ||
    (level === "editor" && roles.includes("editor"))
  );
}
