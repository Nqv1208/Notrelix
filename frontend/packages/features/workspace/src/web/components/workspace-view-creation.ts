import type {
  WorkspaceViewTarget,
  WorkspaceViewType,
} from "../../core/types/workspace";

const BOARD_BACKED_VIEW_TYPES = new Set<WorkspaceViewType>([
  "table",
  "kanban",
  "calendar",
  "timeline",
]);

export interface WorkspaceViewCreationResources {
  readonly boards: readonly { id: string }[];
  readonly pages: readonly { id: string; title: string }[];
}

export function getWorkspaceViewCreationState(
  type: WorkspaceViewType,
  resources: WorkspaceViewCreationResources,
  selectedPageId?: string,
): { disabled: boolean; reason?: string; target: WorkspaceViewTarget } {
  if (BOARD_BACKED_VIEW_TYPES.has(type) && resources.boards.length === 0) {
    return {
      disabled: true,
      reason: "Create a board first to use this view.",
      target: {},
    };
  }

  if (type === "doc") {
    if (resources.pages.length === 0) {
      return {
        disabled: true,
        reason: "Create a document first to use this view.",
        target: {},
      };
    }
    if (!selectedPageId) {
      return {
        disabled: true,
        reason: "Select a document before creating this view.",
        target: {},
      };
    }
    return { disabled: false, target: { pageId: selectedPageId } };
  }

  const boardId = resources.boards[0]?.id;
  return {
    disabled: false,
    target: BOARD_BACKED_VIEW_TYPES.has(type) && boardId ? { boardId } : {},
  };
}
