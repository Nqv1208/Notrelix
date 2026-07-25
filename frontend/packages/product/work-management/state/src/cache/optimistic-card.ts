import type { Card, FullBoardResponse, CreateCardInput } from "@notrelix/work-management-core";

export function createOptimisticCard(
  fullBoard: FullBoardResponse,
  payload: CreateCardInput,
  id: string,
): Card {
  const targetGroup = fullBoard.groups.find((group) => group.id === payload.listId);
  const title = payload.title.trim();
  const position = payload.position ?? ((targetGroup?.cards.at(-1)?.position ?? 0) + 1);
  const status = statusForGroup(targetGroup?.title ?? "");
  const now = new Date().toISOString();

  return {
    id,
    listId: payload.listId,
    boardId: fullBoard.board.id,
    workspaceId: fullBoard.board.workspaceId,
    title,
    descriptionMd: "",
    position,
    priority: "medium",
    status,
    dueDate: undefined,
    startDate: undefined,
    completedAt: status === "status-done" || status === "status-completed" ? now : undefined,
    isArchived: false,
    isDeleted: false,
    members: [],
    labels: [],
    checklists: [],
    fieldValues: {
      [`${fullBoard.board.id}-field-title`]: title,
      [`${fullBoard.board.id}-field-person`]: [],
      [`${fullBoard.board.id}-field-status`]: status,
      [`${fullBoard.board.id}-field-priority`]: "medium",
      [`${fullBoard.board.id}-field-due-date`]: undefined,
      [`${fullBoard.board.id}-field-linked-page`]: undefined,
      [`${fullBoard.board.id}-field-progress`]: 0,
    },
    _count: { comments: 0, attachments: 0, checklistItems: 0 },
    createdAt: now,
    updatedAt: now,
  };
}

export function statusForGroup(groupTitle: string): string {
  const normalized = groupTitle.toLowerCase();
  if (normalized.includes("working")) return "status-working";
  if (normalized.includes("stuck")) return "status-stuck";
  if (normalized.includes("completed")) return "status-completed";
  if (normalized.includes("done")) return "status-done";
  return "status-not-started";
}
