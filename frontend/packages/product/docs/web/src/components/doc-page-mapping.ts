import type {
  Block,
  BlockType,
  PageActivity,
  PageComment,
} from "@notrelix/docs-core";
import { fillDocCopy, type DocPageCopy } from "./doc-page.copy";
import type {
  DocBlockView,
  DocCommentView,
  DocPerson,
  DocTaskResolution,
  DocTocEntry,
  DocVersionView,
} from "./doc-page-models";
import { markQuotes, parseSegments, taskIdsOf } from "./doc-page-text";
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
      markQuotes(parseSegments(text, context.resolveTask), context.quotes);
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
        };
      case "callout":
        return { id: block.id, kind: "callout", text };
      case "code":
        return { id: block.id, kind: "code", text };
      case "divider":
        return { id: block.id, kind: "divider" };
      case "board_reference":
        return {
          id: block.id,
          kind: "liveTable",
          boardName: context.board?.name ?? props.title ?? "",
          filterLabel: props.caption ?? "",
          boardHref: context.board?.href,
          tasks: (props.items ?? []).map(context.resolveTask),
        };
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
      const { quote, text } = splitQuote(comment.body);
      const date = new Date(comment.createdAt);
      const day = relativeDay(date, now);
      return {
        id: comment.id,
        author: personFor(comment.authorId),
        quote,
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
  history: readonly PageActivity[],
  copy: DocPageCopy,
  now: Date,
  personFor: (userId: string) => DocPerson,
): DocVersionView[] {
  return [...history]
    .sort((a, b) => b.createdAt.localeCompare(a.createdAt))
    .map((entry, index) => {
      const date = new Date(entry.createdAt);
      const day = relativeDay(date, now);
      const action = copy.versions.actions[entry.action];
      return {
        id: entry.id,
        author: personFor(entry.actorId),
        time: clock(date),
        day:
          day === "today"
            ? copy.versions.today
            : day === "yesterday"
              ? copy.versions.yesterday
              : dayMonth(date),
        stamp: `${dayMonth(date)} ${clock(date)}`,
        summary: entry.targetLabel || action,
        current: index === 0,
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

/** Markdown of the page for the download action. */
export function toMarkdown(
  title: string,
  blocks: readonly Block[],
  resolveTask: (taskId: string) => DocTaskResolution,
): string {
  const lines: string[] = [`# ${title}`, ""];
  let number = 0;
  const text = (block: Block) =>
    parseSegments(blockText(block), resolveTask)
      .map((segment) =>
        segment.kind === "task"
          ? "restricted" in segment.task
            ? ""
            : `${segment.task.key} ${segment.task.title}`
          : segment.kind === "del"
            ? ""
            : segment.text,
      )
      .join("");
  for (const block of [...blocks].sort((a, b) => a.position - b.position)) {
    if (block.type !== "numbered_list") number = 0;
    switch (block.type) {
      case "heading_1":
        lines.push(`## ${blockText(block)}`, "");
        break;
      case "heading_2":
        lines.push(`## ${blockText(block)}`, "");
        break;
      case "heading_3":
        lines.push(`### ${blockText(block)}`, "");
        break;
      case "numbered_list":
        number += 1;
        lines.push(`${number}. ${text(block)}`);
        break;
      case "bulleted_list":
        lines.push(`- ${text(block)}`);
        break;
      case "todo":
        lines.push(
          `- [${block.properties.checked ? "x" : " "}] ${blockText(block)}`,
        );
        break;
      case "quote":
      case "callout":
        lines.push(`> ${text(block)}`, "");
        break;
      case "code":
        lines.push("```", blockText(block), "```", "");
        break;
      case "divider":
        lines.push("---", "");
        break;
      case "board_reference":
        for (const id of block.properties.items ?? []) {
          const task = resolveTask(id);
          if (!("restricted" in task))
            lines.push(`- ${task.key} ${task.title}`);
        }
        lines.push("");
        break;
      default:
        lines.push(text(block), "");
    }
  }
  return `${lines
    .join("\n")
    .replace(/\n{3,}/g, "\n\n")
    .trim()}\n`;
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
