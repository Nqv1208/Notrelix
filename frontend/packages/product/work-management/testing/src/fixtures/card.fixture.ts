import type { Card } from "@notrelix/work-management-core";
import { FIXED_NOW } from "../support/fixed-clock";

export function cardFixture(overrides: Partial<Card> = {}): Card {
  return {
    id: "card-test",
    listId: "group-test",
    boardId: "board-test",
    workspaceId: "workspace-test",
    title: "Test card",
    position: 1,
    status: "todo",
    isArchived: false,
    isDeleted: false,
    members: [],
    labels: [],
    checklists: [],
    fieldValues: {},
    _count: { comments: 0, attachments: 0, checklistItems: 0 },
    createdAt: FIXED_NOW,
    updatedAt: FIXED_NOW,
    ...overrides,
  };
}
