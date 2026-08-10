import type { Card } from "@notrelix/work-management-core";

export function itemFixture(overrides?: Partial<Card>): Card {
  return {
    id: "card-test",
    listId: "group-test",
    boardId: "board-test",
    workspaceId: "workspace-test",
    title: "Test Item",
    descriptionMd: "Test description",
    position: 1,
    priority: "medium",
    status: "status-todo",
    dueDate: "2026-06-01T00:00:00.000Z",
    isArchived: false,
    isDeleted: false,
    members: [],
    labels: [],
    checklists: [],
    fieldValues: {},
    _count: { comments: 0, attachments: 0, checklistItems: 0 },
    createdAt: "2026-01-01T00:00:00.000Z",
    updatedAt: "2026-01-01T00:00:00.000Z",
    ...overrides,
  };
}
