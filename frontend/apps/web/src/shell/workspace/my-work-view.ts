import {
  groupMyWorkItems,
  type MyWorkItem,
} from "@notrelix/features-workspace/core";
import type {
  MyWorkDepartment,
  MyWorkGroupView,
} from "@notrelix/features-workspace/web";

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

/** The name a greeting uses: the given name (Vietnamese: last word; English: first). */
export function greetingNameFor(
  fullName: string | null | undefined,
  locale: string,
): string | null {
  const words = (fullName ?? "")
    .replace(/\s*\([^)]*\)\s*$/, "")
    .trim()
    .split(/\s+/)
    .filter(Boolean);
  if (words.length === 0) return null;
  return (locale === "vi" ? words[words.length - 1] : words[0]) ?? null;
}
