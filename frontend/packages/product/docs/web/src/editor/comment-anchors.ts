import { Extension } from "@tiptap/core";
import type { Node as PmNode } from "@tiptap/pm/model";
import { Plugin, PluginKey, type EditorState } from "@tiptap/pm/state";
import { Decoration, DecorationSet } from "@tiptap/pm/view";
import type { Editor } from "@tiptap/core";
import {
  COMMENT_ANCHOR_MAX_QUOTE,
  resolveCommentAnchor,
  type CommentAnchor,
} from "@notrelix/docs-core";

/**
 * Comments anchored to text. The editor only draws them: the anchors are the
 * Collaboration facts (block, quote, offsets) the page hands in, shown as
 * decorations over the text they still point at. Nothing is written into the
 * page's content, and an anchor whose text is gone is reported as detached
 * instead of being attached to other text.
 */

/** One anchor to draw: the comment it belongs to and where it points. */
export interface CommentAnchorInput extends CommentAnchor {
  readonly id: string;
}

interface Placed {
  readonly id: string;
  readonly from: number;
  readonly to: number;
}

export interface CommentAnchorState {
  readonly anchors: readonly CommentAnchorInput[];
  readonly activeId: string | null;
  readonly placed: readonly Placed[];
  /** Ids of anchors whose text is no longer on the page. */
  readonly detached: readonly string[];
  readonly decorations: DecorationSet;
}

export const commentAnchorKey = new PluginKey<CommentAnchorState>(
  "docCommentAnchors",
);

export const COMMENT_ANCHOR_CLASS = "comment-anchor";
export const COMMENT_ANCHOR_ACTIVE_CLASS = "comment-anchor-active";

interface Meta {
  anchors?: readonly CommentAnchorInput[];
  activeId?: string | null;
}

/** What one inline child counts for in the block's plain text. */
function childText(child: PmNode): string {
  if (child.isText) return child.text ?? "";
  if (child.type.name === "hardBreak") return "\n";
  if (child.type.name === "taskRef") {
    // Stored as a text run, so it counts as the run's text does.
    return `[[task:${String(child.attrs.taskId)}]]`;
  }
  return "￼";
}

/** The plain text of a block as the producer counts it (runs joined). */
export function blockPlainText(block: PmNode): string {
  let text = "";
  block.forEach((child) => {
    text += childText(child);
  });
  return text;
}

/** Document position of a text offset in a block that starts at `blockPos`. */
function positionAt(block: PmNode, blockPos: number, offset: number): number {
  let seen = 0;
  let pos = blockPos + 1;
  for (let i = 0; i < block.childCount; i += 1) {
    const child = block.child(i);
    const length = childText(child).length;
    if (offset <= seen + length) {
      // Inside a text node the offset maps one to one; an atom is all or nothing.
      return child.isText
        ? pos + (offset - seen)
        : offset === seen
          ? pos
          : pos + child.nodeSize;
    }
    seen += length;
    pos += child.nodeSize;
  }
  return pos;
}

/** Text offset of a document position inside a block. */
function offsetAt(block: PmNode, blockPos: number, position: number): number {
  let seen = 0;
  let pos = blockPos + 1;
  for (let i = 0; i < block.childCount; i += 1) {
    const child = block.child(i);
    const end = pos + child.nodeSize;
    if (position <= end) {
      return child.isText
        ? seen + (position - pos)
        : seen + (position > pos ? childText(child).length : 0);
    }
    seen += childText(child).length;
    pos = end;
  }
  return seen;
}

function compute(
  doc: PmNode,
  anchors: readonly CommentAnchorInput[],
  activeId: string | null,
): CommentAnchorState {
  const blocks = new Map<string, { node: PmNode; pos: number }>();
  doc.forEach((node, pos) => {
    const id = node.attrs.blockId as string | null | undefined;
    if (id && node.isTextblock) blocks.set(id, { node, pos });
  });
  const placed: Placed[] = [];
  const detached: string[] = [];
  for (const anchor of anchors) {
    const block = blocks.get(anchor.blockId);
    const range = block
      ? resolveCommentAnchor(blockPlainText(block.node), anchor)
      : null;
    if (!block || !range) {
      detached.push(anchor.id);
      continue;
    }
    placed.push({
      id: anchor.id,
      from: positionAt(block.node, block.pos, range.start),
      to: positionAt(block.node, block.pos, range.end),
    });
  }
  return {
    anchors,
    activeId,
    placed,
    detached,
    decorations: DecorationSet.create(
      doc,
      placed
        .filter((entry) => entry.to > entry.from)
        .map((entry) =>
          Decoration.inline(
            entry.from,
            entry.to,
            {
              class:
                entry.id === activeId
                  ? `${COMMENT_ANCHOR_CLASS} ${COMMENT_ANCHOR_ACTIVE_CLASS}`
                  : COMMENT_ANCHOR_CLASS,
              "data-comment-id": entry.id,
            },
            { commentId: entry.id },
          ),
        ),
    ),
  };
}

declare module "@tiptap/core" {
  interface Commands<ReturnType> {
    commentAnchors: {
      /** Shows these anchors; `activeId` is the thread being looked at. */
      setCommentAnchors: (
        anchors: readonly CommentAnchorInput[],
        activeId: string | null,
      ) => ReturnType;
    };
  }
}

export function commentAnchorStateOf(
  state: EditorState,
): CommentAnchorState | undefined {
  return commentAnchorKey.getState(state);
}

export interface CommentAnchorOptions {
  /** A highlighted comment was clicked in the text. */
  readonly onOpen: (commentId: string) => void;
}

export const CommentAnchors = Extension.create<CommentAnchorOptions>({
  name: "commentAnchors",

  addOptions() {
    return { onOpen: () => undefined };
  },

  addCommands() {
    return {
      setCommentAnchors:
        (anchors, activeId) =>
        ({ tr, dispatch }) => {
          if (dispatch) {
            tr.setMeta(commentAnchorKey, { anchors, activeId } satisfies Meta);
            tr.setMeta("addToHistory", false);
            dispatch(tr);
          }
          return true;
        },
    };
  },

  addProseMirrorPlugins() {
    const open = (id: string) => this.options.onOpen(id);
    return [
      new Plugin<CommentAnchorState>({
        key: commentAnchorKey,
        state: {
          init: (_config, state) => compute(state.doc, [], null),
          apply(tr, previous, _old, next) {
            const meta = tr.getMeta(commentAnchorKey) as Meta | undefined;
            if (!meta && !tr.docChanged) return previous;
            return compute(
              next.doc,
              meta?.anchors ?? previous.anchors,
              meta?.activeId === undefined ? previous.activeId : meta.activeId,
            );
          },
        },
        props: {
          decorations: (state) => commentAnchorStateOf(state)?.decorations,
          handleClick(view, pos) {
            const found = commentAnchorStateOf(view.state)?.placed.find(
              (entry) => pos >= entry.from && pos <= entry.to,
            );
            if (!found || found.id === PENDING_ID) return false;
            open(found.id);
            return false;
          },
        },
      }),
    ];
  },
});

/** The id the selection being commented on is drawn with. */
export const PENDING_ID = "pending";

/**
 * The anchor of the current selection, or null when it is not on text of a
 * top-level block. A selection over several blocks is clamped to the part in
 * its first block; the quote is cut to the longest the producer accepts.
 */
export function anchorFromSelection(editor: Editor): CommentAnchor | null {
  const { selection } = editor.state;
  if (selection.empty) return null;
  const { $from, $to } = selection;
  if ($from.depth !== 1 || !$from.parent.isTextblock) return null;
  const block = $from.parent;
  const blockId = block.attrs.blockId as string | null | undefined;
  if (!blockId || block.type.name === "codeBlock") return null;
  const blockPos = $from.before(1);
  const text = blockPlainText(block);
  const start = offsetAt(block, blockPos, selection.from);
  const sameBlock = $to.parent === block;
  const stop = sameBlock
    ? offsetAt(block, blockPos, selection.to)
    : text.length;
  const end = Math.min(stop, start + COMMENT_ANCHOR_MAX_QUOTE);
  const quote = text.slice(start, end);
  if (!quote.trim()) return null;
  return { blockId, quote, start, end };
}
