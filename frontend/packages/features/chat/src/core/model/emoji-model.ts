import { EMOJI_ENTRIES, type EmojiEntry } from "./emoji-data";
import { foldText } from "./mention-model";

/** The recent row holds this many emoji. */
export const EMOJI_RECENT_MAX = 16;
/** Most suggestions the `:shortcode` list shows. */
export const SHORTCODE_MAX = 8;
/** A shortcode needs this many characters before it suggests. */
export const SHORTCODE_MIN_QUERY = 2;

interface Indexed {
  readonly entry: EmojiEntry;
  readonly id: string;
  readonly words: readonly string[];
}

const INDEX: readonly Indexed[] = EMOJI_ENTRIES.map((entry) => ({
  entry,
  id: entry.id.replace(/_/g, " "),
  words: foldText(`${entry.id.replace(/_/g, " ")} ${entry.en} ${entry.vi}`)
    .split(/\s+/u)
    .filter(Boolean),
}));

/**
 * The emoji matching a search, best first: a name that starts with the text, then a word that starts with
 * it, then a word that contains it. English and Vietnamese words are both searched, accents ignored.
 */
export function searchEmoji(query: string): readonly EmojiEntry[] {
  const needle = foldText(query.trim());
  if (needle === "") return EMOJI_ENTRIES;
  const parts = needle.split(/\s+/u);
  const scored: { entry: EmojiEntry; score: number; order: number }[] = [];
  INDEX.forEach((item, order) => {
    let total = 0;
    for (const part of parts) {
      if (item.id.startsWith(part)) total += 3;
      else if (item.words.some((word) => word.startsWith(part))) total += 2;
      else if (item.words.some((word) => word.includes(part))) total += 1;
      else return;
    }
    scored.push({ entry: item.entry, score: total, order });
  });
  scored.sort((a, b) => b.score - a.score || a.order - b.order);
  return scored.map((item) => item.entry);
}

export interface ShortcodeTrigger {
  /** Index of the colon. */
  readonly start: number;
  readonly query: string;
}

/** The `:shortcode` being typed before the caret, or null. The colon must follow a space, a line break or the start. */
export function activeShortcode(
  text: string,
  caret: number,
): ShortcodeTrigger | null {
  const match = /(?:^|\s):([a-z0-9_+-]{2,32})$/iu.exec(text.slice(0, caret));
  if (!match) return null;
  const query = match[1]!;
  return { start: caret - query.length - 1, query };
}

/** The emoji whose shortcode starts with, or whose name contains, what was typed after the colon. */
export function shortcodeSuggestions(query: string): readonly EmojiEntry[] {
  const needle = query.toLowerCase();
  if (needle.length < SHORTCODE_MIN_QUERY) return [];
  const starts: EmojiEntry[] = [];
  const contains: EmojiEntry[] = [];
  for (const entry of EMOJI_ENTRIES) {
    if (entry.id.startsWith(needle)) starts.push(entry);
    else if (entry.id.includes(needle)) contains.push(entry);
  }
  // A shorter name is the closer match (smile before smiley); the data order breaks ties.
  const byLength = (a: EmojiEntry, b: EmojiEntry) => a.id.length - b.id.length;
  return [...starts.sort(byLength), ...contains.sort(byLength)].slice(
    0,
    SHORTCODE_MAX,
  );
}

/** Puts `text` in place of the selection and returns the new draft with the caret after it. */
export function insertAtSelection(
  value: string,
  selectionStart: number,
  selectionEnd: number,
  text: string,
): { readonly text: string; readonly caret: number } {
  const start = Math.min(selectionStart, selectionEnd);
  const end = Math.max(selectionStart, selectionEnd);
  return {
    text: value.slice(0, start) + text + value.slice(end),
    caret: start + text.length,
  };
}

/** Replaces the `:shortcode` with the emoji. */
export function insertShortcode(
  value: string,
  trigger: ShortcodeTrigger,
  caret: number,
  char: string,
): { readonly text: string; readonly caret: number } {
  return insertAtSelection(value, trigger.start, caret, char);
}

/** The recent list after using `char`: it moves to the front, no repeats, capped. */
export function pushRecent(
  recent: readonly string[],
  char: string,
): readonly string[] {
  return [char, ...recent.filter((known) => known !== char)].slice(
    0,
    EMOJI_RECENT_MAX,
  );
}
