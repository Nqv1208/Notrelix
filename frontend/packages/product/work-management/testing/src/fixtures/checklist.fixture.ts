import type { Checklist, ChecklistItem } from "@notrelix/work-management-core";

export function checklistItemFixture(
  overrides: Partial<ChecklistItem> = {},
): ChecklistItem {
  return {
    id: "checklist-item-test",
    title: "Confirm acceptance criteria",
    isDone: false,
    position: 1,
    ...overrides,
  };
}

export function checklistFixture(
  overrides: Partial<Checklist> = {},
): Checklist {
  return {
    id: "checklist-test",
    title: "Execution checklist",
    items: [checklistItemFixture()],
    position: 1,
    ...overrides,
  };
}
