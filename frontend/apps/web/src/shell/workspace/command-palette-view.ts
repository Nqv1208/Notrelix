import {
  NAVIGATION_RESOURCE_KIND,
  type MyWorkItem,
  type NavigationEntry,
} from "@notrelix/features-workspace/core";
import type {
  PaletteBoardEntry,
  PaletteDepartment,
  PaletteDocEntry,
  PaletteRecentEntry,
  PaletteWorkEntry,
  WorkspaceCommandCopy,
} from "@notrelix/features-workspace/web";
import type { CommandSearchDto } from "./use-command-search";
import type { WorkspaceResource } from "./use-workspace-resources";

/**
 * The lists the command palette searches, as the palette draws them. Each
 * mapper keeps to what its read states: the My work read has a name, a Board
 * and a due time (no key, status or assignee), a Doc has a title, a parent and
 * a change time (no editor), a Board has a name and a place. Nothing else is
 * shown.
 */

function pad(value: number): string {
  return String(value).padStart(2, "0");
}

function startOfDay(time: number): number {
  const date = new Date(time);
  date.setHours(0, 0, 0, 0);
  return date.getTime();
}

/** The due label: overdue, "Hôm nay 17:00" today, "20/10" later. */
export function paletteDue(
  dueAt: string | null,
  now: number,
  copy: WorkspaceCommandCopy,
): PaletteWorkEntry["due"] {
  if (!dueAt) return null;
  const due = Date.parse(dueAt);
  if (Number.isNaN(due)) return null;
  const today = startOfDay(now);
  const date = new Date(due);
  if (due < now && startOfDay(due) < today) {
    return { tone: "overdue", label: copy.due.overdue };
  }
  if (startOfDay(due) === today) {
    const hasTime = date.getHours() !== 0 || date.getMinutes() !== 0;
    return {
      tone: due < now ? "overdue" : "today",
      label:
        due < now
          ? copy.due.overdue
          : hasTime
            ? copy.due.today.replace(
                "{{time}}",
                `${pad(date.getHours())}:${pad(date.getMinutes())}`,
              )
            : copy.due.today.replace("{{time}}", "").trim(),
    };
  }
  return {
    tone: "later",
    label: `${pad(date.getDate())}/${pad(date.getMonth() + 1)}`,
  };
}

/** The Items assigned to the member, searchable by name and Board. */
export function toPaletteWork(
  items: readonly MyWorkItem[],
  boardNames: ReadonlyMap<string, string>,
  departments: ReadonlyMap<string, PaletteDepartment>,
  now: number,
  copy: WorkspaceCommandCopy,
): PaletteWorkEntry[] {
  return items.map((item) => ({
    id: item.id,
    title: item.name,
    boardName: boardNames.get(item.boardId) ?? null,
    department: departments.get(item.boardId) ?? null,
    // The read lists the Items assigned to the member.
    mine: true,
    due: paletteDue(item.dueAt, now, copy),
    destination: {
      section: "board" as const,
      boardId: item.boardId,
      item: item.id,
    },
  }));
}

export function toPaletteDocs(
  docs: readonly WorkspaceResource[],
  departmentOfSpace: (spaceId: string | null) => PaletteDepartment | null,
  relativeTime: (value: string, now: number) => string,
  now: number,
): PaletteDocEntry[] {
  return docs.map((doc) => ({
    id: doc.id,
    title: doc.name,
    department: departmentOfSpace(doc.spaceId),
    location: doc.parentName,
    editedLabel: doc.updatedAt ? relativeTime(doc.updatedAt, now) : null,
  }));
}

export function toPaletteBoards(
  boards: readonly WorkspaceResource[],
  departments: ReadonlyMap<string, PaletteDepartment>,
): PaletteBoardEntry[] {
  return boards.map((board) => ({
    id: board.id,
    name: board.name,
    department: departments.get(board.id) ?? null,
  }));
}

/** The member's recently opened Boards and Docs, newest first. */
export function toPaletteRecents(
  entries: readonly NavigationEntry[],
  departments: ReadonlyMap<string, PaletteDepartment>,
  relativeTime: (value: string, now: number) => string,
  now: number,
  limit = 3,
): PaletteRecentEntry[] {
  return entries.slice(0, limit).map((entry) => {
    const isBoard = entry.kind === NAVIGATION_RESOURCE_KIND.board;
    return {
      kind: isBoard ? "board" : "doc",
      id: entry.resourceId,
      name: entry.name,
      // The design marks a recent Board with its department, not a Doc.
      department: isBoard ? (departments.get(entry.resourceId) ?? null) : null,
      whenLabel: entry.at ? relativeTime(entry.at, now) : null,
    };
  });
}

function fill(
  message: string,
  values: Readonly<Record<string, string | number>>,
): string {
  return message.replace(/\{\{(\w+)\}\}/g, (match, key: string) =>
    key in values ? String(values[key]) : match,
  );
}

const BUCKET: Readonly<Record<string, "todo" | "doing" | "review" | "done">> = {
  todo: "todo",
  in_progress: "doing",
  review: "review",
  done: "done",
};

/** Items the search found: by name or by a sub-item, assigned to the member or not. */
export function toSearchWork(
  items: CommandSearchDto["work"],
  departments: ReadonlyMap<string, PaletteDepartment>,
  now: number,
  copy: WorkspaceCommandCopy,
): PaletteWorkEntry[] {
  return items.map((item) => ({
    id: item.id,
    title: item.title,
    key: item.key,
    // Where it matched replaces the Board name, as in the design.
    boardName: item.match.kind === "title" ? item.boardName : null,
    department: departments.get(item.boardId) ?? null,
    mine: item.mine,
    assignee: item.match.kind === "title" ? item.assigneeName : null,
    matchNote:
      item.match.kind === "child"
        ? fill(copy.notes.child, { text: item.match.snippet })
        : null,
    status: item.statusName
      ? {
          bucket: BUCKET[item.statusCategory ?? "todo"] ?? "todo",
          label: item.statusName,
        }
      : null,
    due: paletteDue(item.dueAt, now, copy),
    destination: {
      section: "board" as const,
      boardId: item.boardId,
      item: item.id,
    },
  }));
}

export function toSearchDocs(
  docs: CommandSearchDto["docs"],
  departmentOfSpace: (spaceId: string | null) => PaletteDepartment | null,
  relativeTime: (value: string, now: number) => string,
  now: number,
  copy: WorkspaceCommandCopy,
): PaletteDocEntry[] {
  return docs.map((doc) => ({
    id: doc.id,
    title: doc.title,
    department: departmentOfSpace(doc.spaceId),
    location: doc.location,
    // A match inside the text is told by its note; the edit stays out of the row.
    editedLabel:
      doc.editedAt && doc.match.kind === "title"
        ? relativeTime(doc.editedAt, now)
        : null,
    editedBy: doc.match.kind === "title" ? doc.editedByName : null,
    matchNote:
      doc.match.kind === "content"
        ? fill(copy.notes.content, { text: doc.match.snippet })
        : null,
  }));
}

export function toSearchBoards(
  boards: CommandSearchDto["boards"],
  departments: ReadonlyMap<string, PaletteDepartment>,
  term: string,
  copy: WorkspaceCommandCopy,
): PaletteBoardEntry[] {
  return boards.map((board) => ({
    id: board.id,
    name: board.name,
    department: departments.get(board.id) ?? null,
    matchNote: fill(
      board.matchedItems === 1
        ? copy.notes.boardItems_one
        : copy.notes.boardItems_other,
      { count: board.matchedItems, term },
    ),
  }));
}
