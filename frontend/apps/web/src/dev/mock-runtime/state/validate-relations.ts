import type { MockDatabase } from "./mock-database";

export function validateMockRelations(database: MockDatabase): void {
  const userIds = new Set(database.users.map(({ id }) => id));
  const workspaceIds = new Set(database.workspaces.map(({ id }) => id));
  for (const membership of database.memberships) {
    if (!userIds.has(membership.userId)) {
      throw new Error(`[Mock Runtime] Missing user ${membership.userId}`);
    }
    if (!workspaceIds.has(membership.workspaceId)) {
      throw new Error(`[Mock Runtime] Missing workspace ${membership.workspaceId}`);
    }
  }
  const boardIds = new Set(database.boards.map(({ id }) => id));
  const listIds = new Set(database.lists.map(({ id }) => id));
  const pageIds = new Set(database.pages.map(({ id }) => id));
  for (const board of database.boards) if (!workspaceIds.has(board.workspaceId)) throw new Error(`[Mock Runtime] Missing board workspace ${board.workspaceId}`);
  for (const list of database.lists) if (!boardIds.has(list.boardId)) throw new Error(`[Mock Runtime] Missing list board ${list.boardId}`);
  for (const card of database.cards) {
    if (!boardIds.has(card.boardId)) throw new Error(`[Mock Runtime] Missing card board ${card.boardId}`);
    if (!listIds.has(card.listId)) throw new Error(`[Mock Runtime] Missing card list ${card.listId}`);
  }
  for (const page of database.pages) if (!workspaceIds.has(page.workspaceId)) throw new Error(`[Mock Runtime] Missing page workspace ${page.workspaceId}`);
  for (const block of database.blocks) if (!pageIds.has(block.pageId)) throw new Error(`[Mock Runtime] Missing block page ${block.pageId}`);
  for (const notification of database.notifications) if (!userIds.has(notification.userId)) throw new Error(`[Mock Runtime] Missing notification user ${notification.userId}`);
}
