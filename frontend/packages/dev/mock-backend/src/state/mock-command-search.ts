/**
 * The command palette's search (gap `CTR-GAP-WS-COMMAND-SEARCH`, see
 * docs/superpowers/be-blockers-2026-10.md, "BE requirement from FE: Command"):
 * what a typed word finds in Items, Docs and Boards beyond their names, and
 * what the viewer may see of it. Matching ignores case and marks ("hoa don"
 * finds "hoá đơn").
 */
import type { MockStore } from "./mock-store";
import type { MockCardRecord } from "./records";

/** Lower case, without combining marks, "đ" as "d". */
export function foldText(text: string): string {
  return text
    .normalize("NFD")
    .replace(/[̀-ͯ]/g, "")
    .replace(/đ/gi, "d")
    .toLowerCase();
}

function contains(text: string | undefined | null, needle: string): boolean {
  return Boolean(text) && foldText(text ?? "").includes(needle);
}

/** The text around a match: from its start, cut at a word, with "…" where cut. */
function snippet(
  text: string,
  needle: string,
  length: number,
  ellipsis: boolean,
): string {
  const folded = foldText(text);
  const at = folded.indexOf(needle);
  if (at < 0) return text.slice(0, length);
  let end = Math.min(text.length, at + length);
  if (end < text.length) {
    const space = text.lastIndexOf(" ", end);
    if (space > at + needle.length) end = space;
  }
  const body = text.slice(at, end).trim();
  if (!ellipsis) return body;
  return `${at > 0 ? "…" : ""}${body}${end < text.length ? "…" : ""}`;
}

export type WorkMatch =
  | { readonly kind: "title" }
  | { readonly kind: "child"; readonly snippet: string };

export interface CommandSearchResult {
  readonly work: readonly {
    readonly id: string;
    readonly key: string | null;
    readonly title: string;
    readonly boardId: string;
    readonly boardName: string;
    readonly assigneeName: string | null;
    readonly mine: boolean;
    readonly statusName: string | null;
    readonly statusCategory: string | null;
    readonly dueAt: string | null;
    readonly match: WorkMatch;
  }[];
  readonly docs: readonly {
    readonly id: string;
    readonly title: string;
    readonly spaceId: string | null;
    readonly location: string | null;
    readonly editedAt: string | null;
    readonly editedByName: string | null;
    readonly match:
      | { readonly kind: "title" }
      | { readonly kind: "content"; readonly snippet: string };
  }[];
  readonly boards: readonly {
    readonly id: string;
    readonly name: string;
    readonly spaceId: string | null;
    readonly matchedItems: number;
  }[];
}

/** Items shown at most; the rest is for the Items view. */
const WORK_LIMIT = 6;

export function commandSearch(
  store: MockStore,
  workspaceId: string,
  query: string,
  canReadBoard: (boardId: string) => boolean,
): CommandSearchResult {
  const needle = foldText(query.trim());
  if (needle.length < 2) return { work: [], docs: [], boards: [] };
  const me = store.getCurrentUser().id;

  const work: CommandSearchResult["work"][number][] = [];
  const boards: CommandSearchResult["boards"][number][] = [];
  for (const board of store.getBoards(workspaceId)) {
    if (board.isArchived || !canReadBoard(board.id)) continue;
    const cards = store
      .getLists(board.id)
      .flatMap((list) => store.getCards(list.id))
      .filter((card) => !card.isArchived);
    const tops = cards.filter((card) => !card.parentItemId);
    const statusColumn = store
      .getColumns(board.id)
      .find((column) => column.fieldType === "status");
    let matchedItems = 0;
    for (const card of tops) {
      const children = cards.filter((c) => c.parentItemId === card.id);
      const titleHit = contains(card.title, needle);
      const child = children.find((c) => contains(c.title, needle));
      if (titleHit || child || contains(card.description, needle))
        matchedItems += 1;
      if (!titleHit && !child) continue;
      const optionId = statusColumn
        ? store.getFieldValues(card.id)[statusColumn.id]
        : undefined;
      const option = statusColumn?.options?.find((o) => o.id === optionId);
      const assignee = card.assigneeUserIds?.[0];
      work.push({
        id: card.id,
        key: card.itemKey ?? null,
        title: card.title,
        boardId: board.id,
        boardName: board.title,
        assigneeName:
          assignee && assignee !== me
            ? (store.getUser(assignee)?.name ?? null)
            : null,
        mine:
          store.feed.assigned.has(card.id) ||
          card.assigneeUserIds?.includes(me) === true,
        statusName: option?.name ?? null,
        statusCategory: option?.category ?? null,
        dueAt: card.dueAt ?? null,
        match: titleHit
          ? { kind: "title" }
          : {
              kind: "child",
              snippet: snippet(child!.title, needle, 24, false),
            },
      });
    }
    // A Board is a result when its name matches, or when several of its Items
    // do (one Item is already a result of its own).
    if (contains(board.title, needle) || matchedItems >= 2) {
      boards.push({
        id: board.id,
        name: board.title,
        spaceId: board.spaceId ?? null,
        matchedItems,
      });
    }
  }
  const rank = (a: (typeof work)[number], b: (typeof work)[number]) =>
    Number(b.mine) - Number(a.mine) ||
    (a.match.kind === "title" ? 0 : 1) - (b.match.kind === "title" ? 0 : 1) ||
    (a.dueAt ?? "9").localeCompare(b.dueAt ?? "9");
  work.sort(rank);

  const viewer = store.spaceViewer(workspaceId);
  const docs: CommandSearchResult["docs"][number][] = [];
  for (const page of store.getPages(workspaceId)) {
    if (page.spaceId && store.spaces.accessFor(page.spaceId, viewer) === null)
      continue;
    const titles = [page.title, page.navTitle];
    const titleHit = titles.some((t) => contains(t, needle));
    let match: CommandSearchResult["docs"][number]["match"] | null = titleHit
      ? { kind: "title" }
      : null;
    if (!match) {
      for (const block of store.getPageBlocks(page.id)) {
        const text = String(
          (JSON.parse(block.properties || "{}") as { text?: string }).text ??
            "",
        ).replace(/\[\[task:[^\]]*\]\]/g, "");
        if (contains(text, needle)) {
          match = { kind: "content", snippet: snippet(text, needle, 44, true) };
          break;
        }
      }
    }
    if (!match) continue;
    const path: string[] = [];
    for (
      let parent = page.parentId ? store.getPage(page.parentId) : undefined;
      parent && path.length < 6;
      parent = parent.parentId ? store.getPage(parent.parentId) : undefined
    ) {
      path.unshift(parent.title);
    }
    const location = path.join(" / ");
    const last = [...store.getPageHistory(page.id)].sort((a, b) =>
      b.createdAt.localeCompare(a.createdAt),
    )[0];
    docs.push({
      id: page.id,
      title: page.title,
      spaceId: page.spaceId ?? null,
      location: location || null,
      editedAt: page.updatedAt ?? null,
      editedByName: last ? (store.getUser(last.actorId)?.name ?? null) : null,
      match,
    });
  }
  docs.sort(
    (a, b) =>
      (a.match.kind === "title" ? 0 : 1) - (b.match.kind === "title" ? 0 : 1),
  );

  return { work: work.slice(0, WORK_LIMIT), docs, boards };
}

export type { MockCardRecord };
