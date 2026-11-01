import {
  plainTextOf,
  plainTextOfRuns,
  type BlockDraft,
  type Run,
} from "@notrelix/docs-core";
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

/**
 * Word-level compare of the text of a version against the current text: what
 * the current text added is `ins`, what it dropped is `del`. A single shared
 * word between two changes reads as part of one change ("kế toán trưởng" to
 * "trưởng nhóm"), not as an anchor.
 */
export function diffSegments(before: string, after: string): DocSegment[] {
  const a = before.split(/(\s+)/).filter((token) => token !== "");
  const b = after.split(/(\s+)/).filter((token) => token !== "");
  // Longest common subsequence of the tokens.
  const table: number[][] = Array.from({ length: a.length + 1 }, () =>
    new Array<number>(b.length + 1).fill(0),
  );
  for (let i = a.length - 1; i >= 0; i -= 1) {
    for (let j = b.length - 1; j >= 0; j -= 1) {
      table[i]![j] =
        a[i] === b[j]
          ? table[i + 1]![j + 1]! + 1
          : Math.max(table[i + 1]![j]!, table[i]![j + 1]!);
    }
  }
  type Op = { kind: "eq" | "del" | "ins"; text: string };
  const ops: Op[] = [];
  let i = 0;
  let j = 0;
  while (i < a.length && j < b.length) {
    if (a[i] === b[j]) {
      ops.push({ kind: "eq", text: a[i]! });
      i += 1;
      j += 1;
    } else if (table[i + 1]![j]! >= table[i]![j + 1]!) {
      ops.push({ kind: "del", text: a[i]! });
      i += 1;
    } else {
      ops.push({ kind: "ins", text: b[j]! });
      j += 1;
    }
  }
  for (; i < a.length; i += 1) ops.push({ kind: "del", text: a[i]! });
  for (; j < b.length; j += 1) ops.push({ kind: "ins", text: b[j]! });

  // Group the ops into equal runs and change runs.
  type Run = { equal: boolean; ops: Op[] };
  const runs: Run[] = [];
  for (const op of ops) {
    const equal = op.kind === "eq";
    const last = runs[runs.length - 1];
    if (last && last.equal === equal) last.ops.push(op);
    else runs.push({ equal, ops: [op] });
  }
  // An equal run of one word (and spaces) between two changes joins them.
  for (let r = 1; r < runs.length - 1; r += 1) {
    const run = runs[r]!;
    if (!run.equal) continue;
    const words = run.ops.filter((op) => op.text.trim() !== "");
    if (words.length !== 1) continue;
    run.equal = false;
    run.ops = run.ops.flatMap((op) => [
      { kind: "del" as const, text: op.text },
      { kind: "ins" as const, text: op.text },
    ]);
  }
  const segments: DocSegment[] = [];
  const push = (kind: DocSegment["kind"], text: string) => {
    if (!text) return;
    const last = segments[segments.length - 1];
    if (last && last.kind === kind && "text" in last) {
      segments[segments.length - 1] = {
        ...last,
        text: last.text + text,
      } as DocSegment;
    } else segments.push({ kind, text } as DocSegment);
  };
  for (const run of runs) {
    if (run.equal) {
      push("text", run.ops.map((op) => op.text).join(""));
    } else {
      const removed = run.ops
        .filter((op) => op.kind === "del")
        .map((op) => op.text)
        .join("");
      const added = run.ops
        .filter((op) => op.kind === "ins")
        .map((op) => op.text)
        .join("");
      push("del", removed);
      // What replaced the removed text reads after a space, as in the mockup.
      if (removed && added) push("text", " ");
      push("ins", added);
    }
  }
  // A change does not carry the space after it: the text that follows does.
  const out: DocSegment[] = [];
  let carry = "";
  for (const segment of segments) {
    if (segment.kind === "del" || segment.kind === "ins") {
      const trailing = segment.text.match(/\s+$/)?.[0] ?? "";
      out.push({
        ...segment,
        text: segment.text.slice(0, segment.text.length - trailing.length),
      });
      carry = trailing;
    } else if (segment.kind === "text") {
      const lead = carry && !/^\s/.test(segment.text) ? carry : "";
      out.push({ ...segment, text: lead + segment.text });
      carry = "";
    } else out.push(segment);
  }
  if (carry) out.push({ kind: "text", text: carry });
  return out;
}

/** Splits rich text runs into text (with their marks) and task segments. */
export function runsToSegments(
  runs: readonly Run[],
  resolve: (taskId: string) => DocTaskResolution,
): DocSegment[] {
  const segments: DocSegment[] = [];
  for (const run of runs) {
    let cursor = 0;
    const text = run.text;
    const push = (value: string) => {
      if (value) {
        segments.push({
          kind: "text",
          text: value,
          ...(run.marks.length ? { marks: run.marks } : {}),
        });
      }
    };
    for (const match of text.matchAll(TASK_TOKEN)) {
      const index = match.index ?? 0;
      push(text.slice(cursor, index));
      segments.push({ kind: "task", task: resolve(match[1] ?? "") });
      cursor = index + match[0].length;
    }
    push(text.slice(cursor));
  }
  return segments;
}

/**
 * The text a member's unsaved block holds, to keep, copy or re-apply after a
 * stored change replaced it: a table lists its cells row by row (tab-separated),
 * since its text lives in the cells and not in the block's own runs.
 */
export function draftRecoveryText(draft: BlockDraft): string {
  const table = draft.type === "table" ? draft.properties.table : undefined;
  if (!table) return plainTextOf(draft.content);
  return table.rows
    .map((row) => row.map((cell) => plainTextOfRuns(cell.runs)).join("\t"))
    .join("\n");
}
