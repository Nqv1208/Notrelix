import {
  groupMyWorkItems,
  type MyWorkItem,
} from "@notrelix/features-workspace/core";
import type {
  MyWorkDepartment,
  MyWorkGroupView,
  MyWorkItemView,
} from "@notrelix/features-workspace/web";

/**
 * The member's open Items as the My work page draws them: the Board name and
 * department the member can see, and the relationship the read itself states
 * (it lists the Items assigned to the member). Nothing else is known.
 */
export function toMyWorkItems(
  items: readonly MyWorkItem[],
  boardNames: ReadonlyMap<string, string>,
  departments: ReadonlyMap<string, MyWorkDepartment> = new Map(),
): MyWorkItemView[] {
  return items.map((item) => ({
    ...item,
    boardName: boardNames.get(item.boardId) ?? null,
    department: departments.get(item.boardId) ?? null,
    reason: "mine",
  }));
}

/**
 * Assigned Items with the Board name and department the member can see. The
 * My work read carries nothing else (no key, Status, Priority or relationship),
 * so nothing else is shown.
 */
export function toMyWorkGroups(
  items: readonly MyWorkItem[],
  boardNames: ReadonlyMap<string, string>,
  now: number,
  departments: ReadonlyMap<string, MyWorkDepartment> = new Map(),
): MyWorkGroupView[] {
  return groupMyWorkItems(items, now).map((group) => ({
    range: group.range,
    items: group.items.map((item) => ({
      ...item,
      boardName: boardNames.get(item.boardId) ?? null,
      department: departments.get(item.boardId) ?? null,
    })),
  }));
}

/** The name a greeting uses: the given name, the last word, in every locale. */
export function greetingNameFor(
  fullName: string | null | undefined,
): string | null {
  const words = (fullName ?? "")
    .replace(/\s*\([^)]*\)\s*$/, "")
    .trim()
    .split(/\s+/)
    .filter(Boolean);
  if (words.length === 0) return null;
  return words[words.length - 1] ?? null;
}
