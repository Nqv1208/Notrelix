import {
  exportHtml,
  exportMarkdown,
  type Block,
  type BlockType,
  type PageVersion,
  type PageComment,
} from "@notrelix/docs-core";
import { fillDocCopy, type DocPageCopy } from "./doc-page.copy";
import type {
  DocBlockView,
  DocBoardEmbed,
  DocCommentView,
  DocPerson,
  DocTaskResolution,
  DocTocEntry,
  DocVersionView,
} from "./doc-page-models";
import {
  diffSegments,
  markQuotes,
  parseSegments,
  runsToSegments,
  taskIdsOf,
} from "./doc-page-text";
import type { DocToolbarCurrent } from "./doc-toolbar-surface";

function pad(value: number): string {
  return String(value).padStart(2, "0");
}

export function clock(date: Date): string {
  return `${pad(date.getHours())}:${pad(date.getMinutes())}`;
}

export function dayMonth(date: Date): string {
  return `${pad(date.getDate())}/${pad(date.getMonth() + 1)}`;
}

function startOfDay(date: Date): number {
  return new Date(
    date.getFullYear(),
    date.getMonth(),
    date.getDate(),
  ).getTime();
}

/** "today" | "yesterday" | null by calendar day against `now`. */
export function relativeDay(
  value: Date,
  now: Date,
): "today" | "yesterday" | null {
  const diff = Math.round((startOfDay(now) - startOfDay(value)) / 86_400_000);
  if (diff === 0) return "today";
  if (diff === 1) return "yesterday";
  return null;
}

/** "16:40 hôm qua" / "16:40 hôm nay" / "16:40 10/10". */
export function editedAtLabel(
  copy: DocPageCopy,
  iso: string,
  now: Date,
): string {
  const date = new Date(iso);
  const day = relativeDay(date, now);
  if (day === "today") return `${clock(date)} ${copy.header.today}`;
  if (day === "yesterday") return `${clock(date)} ${copy.header.yesterday}`;
  return `${clock(date)} ${dayMonth(date)}`;
}

function numeric(value: string | number | undefined): number {
  return typeof value === "number" ? value : 0;
}

export function blockText(block: Block): string {
  return block.properties.text ?? "";
}

/** Task ids and linked board ids a page's blocks refer to, in reading order. */
export function referencesOf(blocks: readonly Block[]): {
  readonly taskIds: string[];
  readonly boardIds: string[];
} {
  const taskIds: string[] = [];
  const boardIds: string[] = [];
  const addTask = (id: string) => {
    if (!taskIds.includes(id)) taskIds.push(id);
  };
  for (const block of blocks) {
    for (const id of taskIdsOf(blockText(block))) addTask(id);
    if (block.properties.linkedTaskId) addTask(block.properties.linkedTaskId);
    if (block.type === "board_reference") {
      for (const id of block.properties.items ?? []) addTask(id);
      const board = block.properties.linkedBoardId;
      if (board && !boardIds.includes(board)) boardIds.push(board);
    }
  }
  return { taskIds, boardIds };
}

export interface BlockMappingContext {
  readonly resolveTask: (taskId: string) => DocTaskResolution;
  readonly board: { readonly name: string; readonly href?: string } | null;
  /** Quotes of the comments to mark in text (comments tab only). */
  readonly quotes: readonly string[];
  /** The pages the viewer can read, for page references; omitted reads none. */
  readonly pages?: ReadonlyMap<
    string,
    { readonly title: string; readonly icon: string | null }
  >;
  readonly pageHref?: (pageId: string) => string;
  /** The boards board references show, by id. */
  readonly boards?: ReadonlyMap<string, DocBoardEmbed>;
}

/** Stored blocks to the view model the article renders. */
export function toBlockViews(
  blocks: readonly Block[],
  context: BlockMappingContext,
): DocBlockView[] {
  const sorted = [...blocks].sort((a, b) => a.position - b.position);
  return sorted.map((block): DocBlockView => {
    const text = blockText(block);
    const props = block.properties;
    const format = {
      bold: props.bold,
      italic: props.italic,
      underline: props.underline,
      strike: props.strike,
      align: props.align,
    };
    const segments = () =>
      markQuotes(
        block.content
          ? runsToSegments(block.content.runs, context.resolveTask)
          : parseSegments(text, context.resolveTask),
        context.quotes,
      );
    switch (block.type) {
      case "heading_1":
        return { id: block.id, kind: "heading", level: 1, text, format };
      case "heading_2":
        return { id: block.id, kind: "heading", level: 2, text, format };
      case "heading_3":
        return { id: block.id, kind: "heading", level: 3, text, format };
      case "numbered_list":
        return {
          id: block.id,
          kind: "numbered",
          type: block.type,
          segments: segments(),
          format,
        };
      case "bulleted_list":
        return {
          id: block.id,
          kind: "bulleted",
          type: block.type,
          segments: segments(),
          format,
        };
      case "quote":
        return {
          id: block.id,
          kind: "quote",
          type: block.type,
          segments: segments(),
          format,
        };
      case "todo":
        return {
          id: block.id,
          kind: "todo",
          text,
          checked: Boolean(props.checked),
          ...(props.linkedTaskId ? { linkedTaskId: props.linkedTaskId } : {}),
        };
      case "callout":
        return { id: block.id, kind: "callout", text };
      case "code":
        return { id: block.id, kind: "code", text };
      case "divider":
        return { id: block.id, kind: "divider" };
      case "page_reference": {
        const pageId = props.linkedPageId ?? null;
        const target = pageId ? context.pages?.get(pageId) : undefined;
        return {
          id: block.id,
          kind: "pageReference",
          pageId,
          title: target?.title ?? null,
          icon: target?.icon ?? null,
          ...(pageId && target && context.pageHref
            ? { href: context.pageHref(pageId) }
            : {}),
          unavailable: Boolean(pageId) && !target,
        };
      }
      case "board_reference": {
        const boardId = props.linkedBoardId ?? null;
        const embed = boardId ? context.boards?.get(boardId) : undefined;
        return {
          id: block.id,
          kind: "liveTable",
          boardId,
          state: !boardId ? "unset" : (embed?.state ?? "loading"),
          boardName: embed?.name || context.board?.name || props.title || "",
          filterLabel: props.caption ?? "",
          boardHref: embed?.href ?? context.board?.href,
          ...(embed && embed.state === "ready"
            ? { counts: embed.counts, total: embed.total }
            : {}),
          tasks: embed
            ? embed.tasks
            : (props.items ?? []).map(context.resolveTask),
        };
      }
      default:
        return {
          id: block.id,
          kind: "paragraph",
          type: block.type,
          segments: segments(),
          format,
        };
    }
  });
}

/** Contents entries: the page title, then every heading as a second level. */
export function toTocEntries(
  title: string,
  blocks: readonly Block[],
): DocTocEntry[] {
  const headings = [...blocks]
    .sort((a, b) => a.position - b.position)
    .filter(
      (block) =>
        block.type === "heading_1" ||
        block.type === "heading_2" ||
        block.type === "heading_3",
    )
    .map((block): DocTocEntry => ({
      id: block.id,
      level: 2,
      text: blockText(block),
    }));
  return [{ id: "title", level: 1, text: title }, ...headings];
}

/** A comment body split into its quoted anchor ("> text") and the rest. */
export function splitQuote(body: string): {
  readonly quote: string | null;
  readonly text: string;
} {
  const lines = body.split("\n");
  const quoted: string[] = [];
  while (lines[0]?.startsWith("> "))
    quoted.push((lines.shift() ?? "").slice(2));
  return {
    quote: quoted.length > 0 ? quoted.join(" ") : null,
    text: lines.join("\n").trim(),
  };
}

export function toCommentViews(
  comments: readonly PageComment[],
  copy: DocPageCopy,
  now: Date,
  personFor: (userId: string) => DocPerson,
): DocCommentView[] {
  return [...comments]
    .sort((a, b) => a.createdAt.localeCompare(b.createdAt))
    .map((comment) => {
      // A stored anchor wins; a "> quote" body is the older way to say it.
      const { quote: legacyQuote, text } = comment.anchor
        ? { quote: null, text: comment.body.trim() }
        : splitQuote(comment.body);
      const quote = comment.anchor?.quote ?? legacyQuote;
      const date = new Date(comment.createdAt);
      const day = relativeDay(date, now);
      return {
        id: comment.id,
        author: personFor(comment.authorId),
        quote,
        ...(comment.anchor ? { anchored: true } : {}),
        body: text,
        when:
          day === "today"
            ? copy.comments.today
            : day === "yesterday"
              ? copy.comments.yesterday
              : dayMonth(date),
      };
    });
}

/** Version entries from the page's history, newest first; the first is current. */
export function toVersionViews(
  versions: readonly PageVersion[],
  copy: DocPageCopy,
  now: Date,
  personFor: (userId: string) => DocPerson,
): DocVersionView[] {
  return [...versions]
    .sort((a, b) => b.createdAt.localeCompare(a.createdAt))
    .map((entry) => {
      const date = new Date(entry.createdAt);
      const day = relativeDay(date, now);
      return {
        id: entry.id,
        kind: entry.kind,
        author: personFor(entry.createdBy ?? ""),
        time: clock(date),
        day:
          day === "today"
            ? copy.versions.today
            : day === "yesterday"
              ? copy.versions.yesterday
              : dayMonth(date),
        stamp: `${dayMonth(date)} ${clock(date)}`,
        // Only a version a person named carries that name; the others read by kind.
        summary: copy.versions.kinds[entry.kind],
        ...(entry.kind === "named" && entry.name ? { name: entry.name } : {}),
      };
    });
}

const FONT_SIZE: Readonly<Partial<Record<BlockType, number>>> = {
  heading_1: 24,
  heading_2: 20,
  heading_3: 16,
  paragraph: 16,
  numbered_list: 16,
  bulleted_list: 16,
  quote: 16,
  todo: 15,
  callout: 15,
  code: 13,
};

export function toolbarCurrent(block: Block | null): DocToolbarCurrent {
  if (!block) {
    return {
      blockType: null,
      fontSize: 16,
      bold: false,
      italic: false,
      underline: false,
      strike: false,
      align: "left",
    };
  }
  const props = block.properties;
  return {
    blockType: block.type,
    fontSize: FONT_SIZE[block.type] ?? 16,
    bold: Boolean(props.bold),
    italic: Boolean(props.italic),
    underline: Boolean(props.underline),
    strike: Boolean(props.strike),
    align: props.align ?? "left",
  };
}

/** The text of a task a page names: its key and title, nothing when the viewer cannot read it. */
function taskLabel(task: DocTaskResolution): string {
  return "restricted" in task ? "" : `${task.key} ${task.title}`;
}

const TASK_TOKEN = /\[\[task:([^\]]+)\]\]/g;

/** What a download writes for the page: the core export with the page's task references resolved. */
function exportOptions(resolveTask: (taskId: string) => DocTaskResolution) {
  return {
    mapText: (text: string) =>
      text.replace(TASK_TOKEN, (_match, id: string) =>
        taskLabel(resolveTask(id)),
      ),
    boardReferenceLines: (block: Block) =>
      (block.properties.items ?? [])
        .map((id) => taskLabel(resolveTask(id)))
        .filter((label) => label !== "")
        .map((label) => `- ${label}`),
  };
}

/** Markdown of the page for the download action. */
export function toMarkdown(
  title: string,
  blocks: readonly Block[],
  resolveTask: (taskId: string) => DocTaskResolution,
): string {
  return exportMarkdown(title, blocks, exportOptions(resolveTask));
}

/** One self-contained, sanitised HTML document of the page for the download action. */
export function toHtml(
  title: string,
  blocks: readonly Block[],
  resolveTask: (taskId: string) => DocTaskResolution,
  lang: string,
): string {
  return exportHtml(title, blocks, {
    lang,
    mapText: exportOptions(resolveTask).mapText,
  });
}

/** The position that puts a new block right after `after` (or at the end). */
export function positionAfter(
  blocks: readonly Block[],
  afterId: string | null,
): number {
  const sorted = [...blocks].sort((a, b) => a.position - b.position);
  const index = afterId
    ? sorted.findIndex((block) => block.id === afterId)
    : -1;
  if (index < 0) {
    const last = sorted[sorted.length - 1];
    return last ? numeric(last.position) + 1000 : 1000;
  }
  const current = sorted[index];
  const next = sorted[index + 1];
  const here = numeric(current?.position);
  return next ? (here + numeric(next.position)) / 2 : here + 1000;
}

export function fillPlural(
  one: string,
  other: string,
  count: number,
  values: Readonly<Record<string, string | number>> = {},
): string {
  return fillDocCopy(count === 1 ? one : other, { count, ...values });
}

/**
 * The page against one of its versions: blocks the version did not have are
 * `inserted`, and the text that changed carries `ins` and `del` segments.
 */
export function markVersionDiff(
  views: readonly DocBlockView[],
  current: readonly Block[],
  snapshot: readonly Block[],
): DocBlockView[] {
  const before = new Map(snapshot.map((block) => [block.id, block]));
  const after = new Map(current.map((block) => [block.id, block]));
  return views.map((view): DocBlockView => {
    const old = before.get(view.id);
    if (!old) return { ...view, inserted: true };
    const now = after.get(view.id);
    if (
      now &&
      (view.kind === "paragraph" ||
        view.kind === "numbered" ||
        view.kind === "bulleted" ||
        view.kind === "quote") &&
      blockText(old) !== blockText(now) &&
      !blockText(old).includes("[[task:") &&
      !blockText(now).includes("[[task:")
    ) {
      return {
        ...view,
        segments: diffSegments(blockText(old), blockText(now)),
      };
    }
    return view;
  });
}
