import type { CardSummaryDtoApi, FullBoardDtoApi } from "@notrelix/work-management-core";
import type { MockDatabase } from "../state/mock-database";

function toSummary(card: MockDatabase["cards"][number]): CardSummaryDtoApi {
  return {
    id: card.id,
    title: card.title,
    priority: card.priority,
    status: card.status,
    dueDate: card.dueDate,
    cover: card.cover,
    memberCount: card.members.length,
    members: card.members,
    labels: card.labels,
    checklistProgress: 0,
    checklistTotal: card.checklists.length,
    commentCount: card.commentCount,
    attachmentCount: card.attachmentCount,
    position: card.position,
    fieldValues: card.fieldValues,
  };
}

export function projectFullBoard(database: MockDatabase, boardId: string): FullBoardDtoApi | undefined {
  const board = database.boards.find((candidate) => candidate.id === boardId);
  if (!board) return undefined;
  return {
    id: board.id,
    title: board.title,
    description: board.description,
    background: board.background,
    visibility: board.visibility,
    columns: database.columns.filter((column) => column.boardId === boardId),
    members: [],
    lists: database.lists
      .filter((list) => list.boardId === boardId)
      .map((list) => ({ ...list, cards: database.cards.filter((card) => card.listId === list.id).map(toSummary) })),
  };
}
