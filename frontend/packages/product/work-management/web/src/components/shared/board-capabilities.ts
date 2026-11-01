import { useBoardAccess, useFullBoard } from "@notrelix/work-management-state";
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
 * Read-only because the board, or the item's group, is archived (GOV-033): the
 * server refuses every write on it except a person's own preference and follow,
 * so only remind, which a plain viewer keeps off too, is switched off here.
 */
const FROZEN: TaskDetailCapabilities = { ...READ_ONLY, frozen: true };

/**
 * What the viewer may do on the board, from their effective level on the
 * board access read. Editors and managers edit; a commenter only comments;
 * a viewer, and anyone while the level is not known yet, is read-only. There
 * is no edit default: the server decides every write, and this only shapes
 * the controls.
 */
export function boardCapabilities(
  level: BoardAccessLevel | null | undefined,
  archived = false,
): TaskDetailCapabilities {
  // An archived board is read-only for everyone, a manager included: its
  // writes are refused until it is unarchived, so no write control is offered.
  if (archived) return FROZEN;
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

/**
 * The board's access read and what the viewer may do on it, with the board's
 * archived state applied. The one place the views, the item sheet and the
 * item page take their write controls from.
 */
export function useBoardCapabilities(boardId?: string, workspaceId?: string) {
  const { access } = useBoardAccess(boardId, workspaceId);
  const { board } = useFullBoard(boardId, workspaceId);
  return {
    access,
    capabilities: boardCapabilities(
      access?.myLevel,
      board?.isArchived === true,
    ),
  };
}

/**
 * What the viewer may do on one item: the board's capabilities, and none while
 * the item's group is archived (the item stays readable, the server refuses
 * every write on it). An item whose group is not in the board read yet keeps
 * the board's capabilities; the server decides every write either way.
 */
export function useItemCapabilities(
  boardCapabilitiesOfViewer: TaskDetailCapabilities,
  boardId: string,
  workspaceId: string,
  groupId: string | undefined,
): TaskDetailCapabilities {
  const { archivedGroupIds } = useFullBoard(boardId, workspaceId);
  const groupArchived =
    groupId !== undefined && archivedGroupIds.includes(groupId);
  return groupArchived ? FROZEN : boardCapabilitiesOfViewer;
}
