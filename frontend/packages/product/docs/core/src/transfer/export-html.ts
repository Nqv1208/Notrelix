import type { Block } from "../types/block";
import { draftOfBlock } from "../types/block-content";
import {
  isSafeHref,
  normalizeMarks,
  plainTextOfRuns,
  type Run,
} from "../types/rich-text";
import { orderedBlocks } from "./ordered-blocks";

export interface HtmlExportOptions {
  /** The document language (`lang` attribute); defaults to `en`. */
  readonly lang?: string;
  /** Maps the text of every run before it is written. */
  readonly mapText?: (text: string) => string;
}

const ENTITIES: Readonly<Record<string, string>> = {
  "&": "&amp;",
  "<": "&lt;",
  ">": "&gt;",
  '"': "&quot;",
  "'": "&#39;",
};

/** Escapes text for element content and quoted attribute values. */
export function escapeHtml(value: string): string {
  return value.replace(/[&<>"']/g, (char) => ENTITIES[char] ?? char);
}

function runHtml(run: Run, mapText?: (text: string) => string): string {
  const text = mapText ? mapText(run.text) : run.text;
  if (!text) return "";
  const marks = normalizeMarks(run.marks);
  const has = (type: string) => marks.some((mark) => mark.type === type);
  const link = marks.find((mark) => mark.type === "link");
  const highlight = marks.find(
    (mark) => mark.type === "highlight" && mark.token !== "none",
  );
  let html = escapeHtml(text).replace(/\n/g, "<br>");
  if (has("code")) html = `<code>${html}</code>`;
  if (has("bold")) html = `<strong>${html}</strong>`;
  if (has("italic")) html = `<em>${html}</em>`;
  if (has("underline")) html = `<u>${html}</u>`;
  if (has("strike")) html = `<s>${html}</s>`;
  if (highlight) html = `<mark>${html}</mark>`;
  if (link && link.type === "link" && isSafeHref(link.href)) {
    html = `<a href="${escapeHtml(link.href.trim())}" rel="noopener noreferrer">${html}</a>`;
  }
  return html;
}

function inline(
  runs: readonly Run[],
  mapText?: (text: string) => string,
): string {
  return runs.map((run) => runHtml(run, mapText)).join("");
}

const STYLE =
  "body{font-family:system-ui,sans-serif;line-height:1.6;max-width:46rem;margin:2rem auto;padding:0 1rem;color:#1f2328}" +
  "pre{background:#f3f4f6;padding:.75rem;overflow:auto;border-radius:6px}" +
  "code{font-family:ui-monospace,monospace}blockquote{margin:1rem 0;padding-left:1rem;border-left:3px solid #d0d7de;color:#57606a}" +
  "table{border-collapse:collapse}td,th{border:1px solid #d0d7de;padding:.35rem .6rem;text-align:left}";

/** A language class is only written when it is a plain identifier. */
function languageClass(language: string | undefined): string {
  return language && /^[A-Za-z0-9+#._-]{1,32}$/.test(language)
    ? ` class="language-${language}"`
    : "";
}

/**
 * The page as one self-contained HTML document. Every piece of text is
 * escaped, links keep only http, https and mailto addresses, and the document
 * carries no script, event attribute or external resource.
 */
export function exportHtml(
  title: string,
  blocks: readonly Block[],
  options: HtmlExportOptions = {},
): string {
  const { mapText } = options;
  const body: string[] = [`<h1>${escapeHtml(title.trim())}</h1>`];
  let list: "ul" | "ol" | null = null;
  const closeList = () => {
    if (list) body.push(`</${list}>`);
    list = null;
  };
  const openList = (kind: "ul" | "ol") => {
    if (list === kind) return;
    closeList();
    body.push(`<${kind}>`);
    list = kind;
  };
  for (const { block } of orderedBlocks(blocks)) {
    const draft = draftOfBlock(block);
    const runs = draft.content.runs;
    const text = inline(runs, mapText);
    if (block.type === "bulleted_list") {
      openList("ul");
      body.push(`<li>${text}</li>`);
      continue;
    }
    if (block.type === "numbered_list") {
      openList("ol");
      body.push(`<li>${text}</li>`);
      continue;
    }
    if (block.type === "todo") {
      openList("ul");
      body.push(
        `<li>${block.properties.checked ? "&#9745;" : "&#9744;"} ${text}</li>`,
      );
      continue;
    }
    closeList();
    switch (block.type) {
      case "heading_1":
        body.push(`<h2>${text}</h2>`);
        break;
      case "heading_2":
        body.push(`<h3>${text}</h3>`);
        break;
      case "heading_3":
        body.push(`<h4>${text}</h4>`);
        break;
      case "quote":
      case "callout":
        body.push(`<blockquote>${text}</blockquote>`);
        break;
      case "code":
        body.push(
          `<pre><code${languageClass(block.properties.language)}>${escapeHtml(plainTextOfRuns(runs))}</code></pre>`,
        );
        break;
      case "divider":
        body.push("<hr>");
        break;
      case "table": {
        const table = block.properties.table;
        if (!table || table.rows.length === 0) break;
        const rows = table.rows.map((row, index) => {
          const tag = table.headerRow && index === 0 ? "th" : "td";
          return `<tr>${row
            .map((entry) => `<${tag}>${inline(entry.runs, mapText)}</${tag}>`)
            .join("")}</tr>`;
        });
        body.push(`<table>${rows.join("")}</table>`);
        break;
      }
      case "image":
      case "file": {
        const caption = block.properties.caption?.trim();
        if (caption) body.push(`<p><em>${escapeHtml(caption)}</em></p>`);
        break;
      }
      case "board_reference":
      case "page_reference":
        break;
      default:
        if (text) body.push(`<p>${text}</p>`);
    }
  }
  closeList();
  return [
    "<!doctype html>",
    `<html lang="${escapeHtml(options.lang ?? "en")}">`,
    "<head>",
    '<meta charset="utf-8">',
    '<meta name="viewport" content="width=device-width, initial-scale=1">',
    `<title>${escapeHtml(title.trim())}</title>`,
    `<style>${STYLE}</style>`,
    "</head>",
    "<body>",
    ...body,
    "</body>",
    "</html>",
    "",
  ].join("\n");
}
