import type { Editor } from "@tiptap/react";

export interface TextStats {
  readonly words: number;
  readonly characters: number;
  readonly charactersNoSpaces: number;
}

export interface DocStatsView {
  readonly page: TextStats & {
    readonly blocks: number;
    /** Minutes at 200 words a minute (rounded); 0 reads as "under a minute". */
    readonly readingMinutes: number;
  };
  /** The selected text, when some is selected. */
  readonly selection: TextStats | null;
}

const READING_WORDS_PER_MINUTE = 200;

export function statsOfText(text: string): TextStats {
  const trimmed = text.trim();
  return {
    words: trimmed === "" ? 0 : trimmed.split(/\s+/u).length,
    characters: Array.from(text.replace(/\n/g, "")).length,
    charactersNoSpaces: Array.from(text.replace(/\s/gu, "")).length,
  };
}

/** What the word-count dialog shows, read from the editor's document. */
export function docStats(editor: Editor): DocStatsView {
  const { doc, selection } = editor.state;
  const text = doc.textBetween(0, doc.content.size, "\n", "\n");
  const page = statsOfText(text);
  return {
    page: {
      ...page,
      blocks: doc.childCount,
      readingMinutes: Math.round(page.words / READING_WORDS_PER_MINUTE),
    },
    selection: selection.empty
      ? null
      : statsOfText(doc.textBetween(selection.from, selection.to, "\n", "\n")),
  };
}
