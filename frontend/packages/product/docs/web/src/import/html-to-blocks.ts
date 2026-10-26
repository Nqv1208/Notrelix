import {
  isSafeHref,
  richText,
  type BlockType,
  type ImportedBlock,
  type ImportedPage,
  type Mark,
  type Run,
  type TableCell,
} from "@notrelix/docs-core";

const SKIPPED = new Set(["script", "style", "head", "template", "noscript"]);

interface Walk {
  blocks: ImportedBlock[];
  imagesDropped: number;
}

function trimRuns(runs: Run[]): Run[] {
  const out = runs.map((run) => ({ ...run }));
  const first = out[0];
  if (first) first.text = first.text.replace(/^\s+/, "");
  const last = out[out.length - 1];
  if (last) last.text = last.text.replace(/\s+$/, "");
  return out.filter((run) => run.text !== "");
}

/** The inline content of an element as runs; images are counted and left out. */
function inlineRuns(node: Node, marks: Mark[], walk: Walk): Run[] {
  const runs: Run[] = [];
  node.childNodes.forEach((child) => {
    if (child.nodeType === 3) {
      const text = (child.textContent ?? "").replace(/\s+/g, " ");
      if (text) runs.push({ text, marks });
      return;
    }
    if (child.nodeType !== 1) return;
    const element = child as Element;
    const tag = element.tagName.toLowerCase();
    if (SKIPPED.has(tag)) return;
    if (tag === "br") {
      runs.push({ text: "\n", marks });
      return;
    }
    if (tag === "img") {
      walk.imagesDropped += 1;
      return;
    }
    let next = marks;
    if (tag === "strong" || tag === "b") next = [...marks, { type: "bold" }];
    else if (tag === "em" || tag === "i") {
      next = [...marks, { type: "italic" }];
    } else if (tag === "u") next = [...marks, { type: "underline" }];
    else if (tag === "s" || tag === "strike" || tag === "del") {
      next = [...marks, { type: "strike" }];
    } else if (tag === "code") next = [...marks, { type: "code" }];
    else if (tag === "a") {
      const href = element.getAttribute("href") ?? "";
      if (isSafeHref(href)) next = [...marks, { type: "link", href }];
    }
    runs.push(...inlineRuns(element, next, walk));
  });
  return runs;
}

function push(
  walk: Walk,
  type: BlockType,
  runs: Run[],
  properties: ImportedBlock["properties"] = {},
): void {
  walk.blocks.push({ type, content: richText(trimRuns(runs)), properties });
}

function listItems(list: Element, walk: Walk): void {
  const type: BlockType =
    list.tagName.toLowerCase() === "ol" ? "numbered_list" : "bulleted_list";
  for (const item of Array.from(list.children)) {
    if (item.tagName.toLowerCase() !== "li") continue;
    // The item's own text, then any nested list as further items of the same list.
    const own = document.createDocumentFragment();
    const nested: Element[] = [];
    item.childNodes.forEach((child) => {
      const tag =
        child.nodeType === 1 ? (child as Element).tagName.toLowerCase() : "";
      if (tag === "ul" || tag === "ol") nested.push(child as Element);
      else own.appendChild(child.cloneNode(true));
    });
    push(walk, type, inlineRuns(own, [], walk));
    for (const inner of nested) listItems(inner, walk);
  }
}

function tableOf(table: Element, walk: Walk): void {
  const rows = Array.from(table.querySelectorAll("tr"));
  if (rows.length === 0) return;
  const cells: TableCell[][] = rows.map((row) =>
    Array.from(row.children)
      .filter((cell) => /^t[dh]$/i.test(cell.tagName))
      .map((cell) => ({ runs: trimRuns(inlineRuns(cell, [], walk)) })),
  );
  const width = Math.max(...cells.map((row) => row.length));
  if (width === 0) return;
  const headerRow =
    !!table.querySelector("thead") ||
    (rows[0]?.querySelector("th") !== null && rows[0] !== undefined);
  walk.blocks.push({
    type: "table",
    content: richText(),
    properties: {
      table: {
        rows: cells.map((row) => [
          ...row,
          ...Array.from({ length: width - row.length }, (): TableCell => ({
            runs: [],
          })),
        ]),
        headerRow,
        colWidths: Array.from({ length: width }, () => null),
      },
    },
  });
}

function blocksOf(parent: Node, walk: Walk): void {
  let loose: Run[] = [];
  const flushLoose = () => {
    if (trimRuns(loose).length) push(walk, "paragraph", loose);
    loose = [];
  };
  parent.childNodes.forEach((child) => {
    if (child.nodeType === 3) {
      const text = (child.textContent ?? "").replace(/\s+/g, " ");
      if (text.trim()) loose.push({ text, marks: [] });
      return;
    }
    if (child.nodeType !== 1) return;
    const element = child as Element;
    const tag = element.tagName.toLowerCase();
    if (SKIPPED.has(tag)) return;
    if (/^h[1-6]$/.test(tag)) {
      flushLoose();
      const level = Number(tag.slice(1));
      push(
        walk,
        level === 1 ? "heading_1" : level === 2 ? "heading_2" : "heading_3",
        inlineRuns(element, [], walk),
      );
    } else if (tag === "p") {
      flushLoose();
      push(walk, "paragraph", inlineRuns(element, [], walk));
    } else if (tag === "ul" || tag === "ol") {
      flushLoose();
      listItems(element, walk);
    } else if (tag === "blockquote") {
      flushLoose();
      const paragraphs = Array.from(element.children).filter(
        (inner) => inner.tagName.toLowerCase() === "p",
      );
      if (paragraphs.length === 0) {
        push(walk, "quote", inlineRuns(element, [], walk));
      } else {
        for (const inner of paragraphs) {
          push(walk, "quote", inlineRuns(inner, [], walk));
        }
      }
    } else if (tag === "pre") {
      flushLoose();
      const text = element.textContent ?? "";
      push(walk, "code", text ? [{ text, marks: [] }] : []);
    } else if (tag === "hr") {
      flushLoose();
      walk.blocks.push({
        type: "divider",
        content: richText(),
        properties: {},
      });
    } else if (tag === "table") {
      flushLoose();
      tableOf(element, walk);
    } else if (tag === "img") {
      walk.imagesDropped += 1;
    } else if (
      tag === "div" ||
      tag === "section" ||
      tag === "article" ||
      tag === "main" ||
      tag === "body" ||
      tag === "figure"
    ) {
      flushLoose();
      blocksOf(element, walk);
    } else {
      // Inline content that sits directly in the body.
      const holder = document.createDocumentFragment();
      holder.appendChild(element.cloneNode(true));
      loose.push(...inlineRuns(holder, [], walk));
    }
  });
  flushLoose();
}

/**
 * Reads an HTML document (what a Word file converts to) into blocks. The HTML
 * is parsed into an inert document and only the structure listed here is
 * read: headings, paragraphs, lists, quotes, code, rules and tables with bold,
 * italic, underline, strike, code and http(s)/mailto links. Scripts, styles,
 * attributes and everything else are dropped; images are counted, not kept.
 */
export function htmlToImport(html: string): ImportedPage {
  const doc = new DOMParser().parseFromString(html, "text/html");
  const walk: Walk = { blocks: [], imagesDropped: 0 };
  blocksOf(doc.body, walk);
  return {
    title: null,
    blocks: walk.blocks,
    imagesDropped: walk.imagesDropped,
  };
}
