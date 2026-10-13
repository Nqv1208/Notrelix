import type { BoardAccessLevel } from "@notrelix/work-management-core";
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
