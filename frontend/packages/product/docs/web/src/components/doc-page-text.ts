import type { DocSegment, DocTaskResolution } from "./doc-page-models";

/**
 * Inline task references are stored inside a block's `text` as
 * `[[task:<id>]]` (a UI convention until Documents defines inline mentions).
 * These helpers move between the stored string, the segments the surfaces
 * render and the DOM of an editable block.
 */
const TASK_TOKEN = /\[\[task:([^\]\s]+)\]\]/g;

export function taskToken(taskId: string): string {
  return `[[task:${taskId}]]`;
}

/** The task ids a stored text refers to, in order. */
export function taskIdsOf(text: string): string[] {
  const ids: string[] = [];
  for (const match of text.matchAll(TASK_TOKEN)) {
    if (match[1]) ids.push(match[1]);
  }
  return ids;
}

/** Splits a stored text into text and task segments. */
export function parseSegments(
  text: string,
  resolve: (taskId: string) => DocTaskResolution,
): DocSegment[] {
  const segments: DocSegment[] = [];
  let cursor = 0;
  for (const match of text.matchAll(TASK_TOKEN)) {
    const index = match.index ?? 0;
    if (index > cursor) {
      segments.push({ kind: "text", text: text.slice(cursor, index) });
    }
    const id = match[1] ?? "";
    segments.push({ kind: "task", task: resolve(id) });
    cursor = index + match[0].length;
  }
  if (cursor < text.length) {
    segments.push({ kind: "text", text: text.slice(cursor) });
  }
  return segments;
}

/** The stored text a list of segments stands for (removed text is dropped). */
export function serializeSegments(segments: readonly DocSegment[]): string {
  return segments
    .map((segment) => {
      switch (segment.kind) {
        case "task":
          return taskToken(segment.task.id);
        case "del":
          return "";
        default:
          return segment.text;
      }
    })
    .join("");
}

/** Reads the stored text back from an editable element. */
export function serializeEditable(root: HTMLElement): string {
  let out = "";
  const visit = (node: Node) => {
    if (node.nodeType === Node.TEXT_NODE) {
      out += node.textContent ?? "";
      return;
    }
    if (!(node instanceof HTMLElement)) return;
    const taskId = node.dataset.taskId;
    if (taskId) {
      out += taskToken(taskId);
      return;
    }
    if (node.tagName === "BR") return;
    node.childNodes.forEach(visit);
  };
  root.childNodes.forEach(visit);
  return out.replace(/\u00a0/g, " ");
}

/** Highlights each anchor quote inside the text segments (first match only). */
export function markQuotes(
  segments: readonly DocSegment[],
  quotes: readonly string[],
): DocSegment[] {
  let result: DocSegment[] = [...segments];
  for (const quote of quotes) {
    if (!quote) continue;
    const next: DocSegment[] = [];
    let done = false;
    for (const segment of result) {
      if (done || segment.kind !== "text") {
        next.push(segment);
        continue;
      }
      const at = segment.text.indexOf(quote);
      if (at < 0) {
        next.push(segment);
        continue;
      }
      done = true;
      if (at > 0) next.push({ kind: "text", text: segment.text.slice(0, at) });
      next.push({ kind: "mark", text: quote });
      const rest = segment.text.slice(at + quote.length);
      if (rest) next.push({ kind: "text", text: rest });
    }
    result = next;
  }
  return result;
}

/** Plain text of a segment list (for names, TOC and labels). */
export function plainText(segments: readonly DocSegment[]): string {
  return segments
    .map((segment) =>
      segment.kind === "task"
        ? "restricted" in segment.task
          ? ""
          : segment.task.title
        : segment.kind === "del"
          ? ""
          : segment.text,
    )
    .join("");
}
