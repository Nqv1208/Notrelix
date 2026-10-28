import { useEditorState, type Editor } from "@tiptap/react";
import type { Node as PmNode } from "@tiptap/pm/model";

export interface OutlineEntry {
  /** The heading block's id. */
  readonly id: string;
  /** 1 to 3, the heading level. */
  readonly level: 1 | 2 | 3;
  readonly text: string;
}

export interface DocStats {
  readonly words: number;
  readonly characters: number;
}

export interface DocSummary {
  readonly outline: readonly OutlineEntry[];
  readonly stats: DocStats;
}

const cache = new WeakMap<PmNode, DocSummary>();

/** The page's headings (H1 to H3) and its word and character counts. */
export function summarize(doc: PmNode): DocSummary {
  const known = cache.get(doc);
  if (known) return known;
  const outline: OutlineEntry[] = [];
  let words = 0;
  let characters = 0;
  doc.descendants((node) => {
    if (!node.isTextblock) return true;
    const text = node.textContent;
    if (node.type.name === "heading" && node.attrs.blockId) {
      const level = Number(node.attrs.level);
      if (level >= 1 && level <= 3 && text.trim()) {
        outline.push({
          id: String(node.attrs.blockId),
          level: level as 1 | 2 | 3,
          text: text.trim(),
        });
      }
    }
    words += text.match(/\S+/gu)?.length ?? 0;
    characters += Array.from(text).length;
    return false;
  });
  const summary = { outline, stats: { words, characters } };
  cache.set(doc, summary);
  return summary;
}

/** Follows the editor: changes whenever a heading or the text changes. */
export function useDocSummary(editor: Editor | null): DocSummary | null {
  return useEditorState({
    editor,
    selector: ({ editor: current }) =>
      current && !current.isDestroyed ? summarize(current.state.doc) : null,
  });
}

/** Moves the caret into a heading and scrolls it to the top of the page. */
export function revealHeading(editor: Editor, blockId: string): boolean {
  let found = -1;
  editor.state.doc.forEach((node, offset) => {
    if (found < 0 && node.attrs.blockId === blockId) found = offset;
  });
  if (found < 0) return false;
  editor
    .chain()
    .focus()
    .setTextSelection(found + 1)
    .run();
  const dom = editor.view.nodeDOM(found);
  (dom instanceof HTMLElement ? dom : null)?.scrollIntoView?.({
    block: "start",
    behavior: "smooth",
  });
  return true;
}
