import { Extension } from "@tiptap/core";
import type { Node as PmNode } from "@tiptap/pm/model";
import {
  Plugin,
  PluginKey,
  type EditorState,
  type Transaction,
} from "@tiptap/pm/state";
import { Decoration, DecorationSet } from "@tiptap/pm/view";

/**
 * Find and replace over the page. Matches are searched inside each text block
 * (so a match runs across bold or coloured text) and shown as decorations; the
 * document changes only when the member replaces.
 */

export interface FindMatch {
  readonly from: number;
  readonly to: number;
}

export interface FindState {
  readonly open: boolean;
  /** How many times the member asked to open (focuses the field again). */
  readonly opens: number;
  /** The replace field shows. */
  readonly replaceMode: boolean;
  readonly term: string;
  readonly caseSensitive: boolean;
  readonly matches: readonly FindMatch[];
  /** The current match, or -1 when there is none. */
  readonly index: number;
  readonly decorations: DecorationSet;
}

type FindInput = Partial<
  Pick<FindState, "open" | "replaceMode" | "term" | "caseSensitive" | "index">
>;

export const findPluginKey = new PluginKey<FindState>("docFindReplace");

export const CURRENT_MATCH_CLASS = "find-match-current";
export const MATCH_CLASS = "find-match";

const OBJECT = "￼";

function fold(text: string, caseSensitive: boolean): string {
  if (caseSensitive) return text;
  // Per character, so a character that lowercases to several keeps its length.
  let folded = "";
  for (const char of text) {
    const lower = char.toLowerCase();
    folded += lower.length === char.length ? lower : char;
  }
  return folded;
}

/** Every match of `term` in the page's text blocks, in document order. */
export function findMatches(
  doc: PmNode,
  term: string,
  caseSensitive: boolean,
): FindMatch[] {
  if (!term) return [];
  const needle = fold(term, caseSensitive);
  const matches: FindMatch[] = [];
  doc.descendants((node, pos) => {
    if (!node.isTextblock) return true;
    let text = "";
    const positions: number[] = [];
    node.forEach((child, offset) => {
      const start = pos + 1 + offset;
      if (child.isText && child.text) {
        for (let i = 0; i < child.text.length; i += 1)
          positions.push(start + i);
        text += child.text;
      } else {
        // Inline nodes break a match.
        positions.push(start);
        text += child.type.name === "hardBreak" ? "\n" : OBJECT;
      }
    });
    const haystack = fold(text, caseSensitive);
    let at = haystack.indexOf(needle);
    while (at !== -1) {
      matches.push({
        from: positions[at] as number,
        to: (positions[at + needle.length - 1] as number) + 1,
      });
      at = haystack.indexOf(needle, at + needle.length);
    }
    return false;
  });
  return matches;
}

function build(
  doc: PmNode,
  base: Omit<FindState, "matches" | "index" | "decorations">,
  wanted: number,
): FindState {
  const matches = base.open
    ? findMatches(doc, base.term, base.caseSensitive)
    : [];
  const index = matches.length
    ? Math.min(Math.max(wanted, 0), matches.length - 1)
    : -1;
  return {
    ...base,
    matches,
    index,
    decorations: DecorationSet.create(
      doc,
      matches.map((match, i) =>
        Decoration.inline(match.from, match.to, {
          class:
            i === index ? `${MATCH_CLASS} ${CURRENT_MATCH_CLASS}` : MATCH_CLASS,
        }),
      ),
    ),
  };
}

const EMPTY_BASE = {
  open: false,
  opens: 0,
  replaceMode: false,
  term: "",
  caseSensitive: false,
} as const;

declare module "@tiptap/core" {
  interface Commands<ReturnType> {
    findReplace: {
      openFind: (replaceMode?: boolean) => ReturnType;
      closeFind: () => ReturnType;
      setFindTerm: (term: string) => ReturnType;
      setFindCaseSensitive: (caseSensitive: boolean) => ReturnType;
      stepFind: (direction: 1 | -1) => ReturnType;
      replaceCurrentMatch: (replacement: string) => ReturnType;
      replaceAllMatches: (replacement: string) => ReturnType;
    };
  }
}

export function findStateOf(state: EditorState): FindState | undefined {
  return findPluginKey.getState(state);
}

export const FindReplace = Extension.create({
  name: "findReplace",

  addCommands() {
    return {
      openFind:
        (replaceMode = false) =>
        ({ tr, dispatch, editor }) => {
          if (dispatch) {
            const wantsReplace = replaceMode && editor.isEditable;
            const current = findStateOf(editor.state);
            tr.setMeta(findPluginKey, {
              open: true,
              replaceMode: wantsReplace || (current?.replaceMode ?? false),
            } satisfies FindInput);
            dispatch(tr);
          }
          return true;
        },
      closeFind:
        () =>
        ({ tr, dispatch }) => {
          if (dispatch) {
            tr.setMeta(findPluginKey, {
              open: false,
              replaceMode: false,
            } satisfies FindInput);
            dispatch(tr);
          }
          return true;
        },
      setFindTerm:
        (term) =>
        ({ tr, dispatch, state }) => {
          if (dispatch) {
            // Start at the first match from the caret, as browsers do.
            const current = findStateOf(state);
            const found = findMatches(
              state.doc,
              term,
              current?.caseSensitive ?? false,
            );
            const after = found.findIndex(
              (match) => match.from >= state.selection.from,
            );
            tr.setMeta(findPluginKey, {
              term,
              index: after === -1 ? 0 : after,
            } satisfies FindInput);
            dispatch(tr);
          }
          return true;
        },
      setFindCaseSensitive:
        (caseSensitive) =>
        ({ tr, dispatch }) => {
          if (dispatch) {
            tr.setMeta(findPluginKey, {
              caseSensitive,
              index: 0,
            } satisfies FindInput);
            dispatch(tr);
          }
          return true;
        },
      stepFind:
        (direction) =>
        ({ tr, dispatch, state }) => {
          const current = findStateOf(state);
          if (!current || current.matches.length === 0) return false;
          if (dispatch) {
            const count = current.matches.length;
            tr.setMeta(findPluginKey, {
              index: (current.index + direction + count) % count,
            } satisfies FindInput);
            dispatch(tr);
          }
          return true;
        },
      replaceCurrentMatch:
        (replacement) =>
        ({ tr, dispatch, editor, state }) => {
          const current = findStateOf(state);
          const match = current?.matches[current.index];
          if (!current || !match || !editor.isEditable) return false;
          if (dispatch) {
            writeReplacement(tr, match, replacement);
            // Continue with the first match after the new text.
            const next = findMatches(
              tr.doc,
              current.term,
              current.caseSensitive,
            ).findIndex((m) => m.from >= match.from + replacement.length);
            tr.setMeta(findPluginKey, {
              index: Math.max(next, 0),
            } satisfies FindInput);
            dispatch(tr.scrollIntoView());
          }
          return true;
        },
      replaceAllMatches:
        (replacement) =>
        ({ tr, dispatch, editor, state }) => {
          const current = findStateOf(state);
          if (!current || current.matches.length === 0 || !editor.isEditable)
            return false;
          if (dispatch) {
            // From the end, so earlier positions stay valid; one transaction is
            // one undo step.
            for (let i = current.matches.length - 1; i >= 0; i -= 1) {
              writeReplacement(
                tr,
                current.matches[i] as FindMatch,
                replacement,
              );
            }
            dispatch(tr);
          }
          return true;
        },
    };
  },

  addKeyboardShortcuts() {
    return {
      "Mod-f": () => this.editor.commands.openFind(false),
      "Mod-h": () =>
        this.editor.isEditable ? this.editor.commands.openFind(true) : false,
    };
  },

  addProseMirrorPlugins() {
    return [
      new Plugin<FindState>({
        key: findPluginKey,
        state: {
          init: (_config, state) => build(state.doc, EMPTY_BASE, 0),
          apply(tr, previous, _old, next) {
            const input = tr.getMeta(findPluginKey) as FindInput | undefined;
            if (!input && !tr.docChanged) return previous;
            return build(
              next.doc,
              {
                open: input?.open ?? previous.open,
                opens: input?.open ? previous.opens + 1 : previous.opens,
                replaceMode: input?.replaceMode ?? previous.replaceMode,
                term: input?.term ?? previous.term,
                caseSensitive: input?.caseSensitive ?? previous.caseSensitive,
              },
              input?.index ?? previous.index,
            );
          },
        },
        props: {
          decorations: (state) => findStateOf(state)?.decorations ?? null,
        },
      }),
    ];
  },
});

function writeReplacement(
  tr: Transaction,
  match: FindMatch,
  replacement: string,
): void {
  if (replacement) tr.insertText(replacement, match.from, match.to);
  else tr.delete(match.from, match.to);
}
