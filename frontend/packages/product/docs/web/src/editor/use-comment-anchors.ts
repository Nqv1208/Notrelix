import { useEffect } from "react";
import { useEditorState, type Editor } from "@tiptap/react";
import {
  PENDING_ID,
  commentAnchorStateOf,
  type CommentAnchorInput,
} from "./comment-anchors";
import type { CommentAnchor } from "@notrelix/docs-core";

/**
 * Draws the page's anchored comments (and the selection being commented on)
 * over the editor's text.
 */
export function useShowCommentAnchors(
  editor: Editor | null,
  anchors: readonly CommentAnchorInput[],
  pending: CommentAnchor | null,
  activeId: string | null,
): void {
  const signature = JSON.stringify([anchors, pending, activeId]);
  useEffect(() => {
    if (!editor || editor.isDestroyed) return;
    const all: CommentAnchorInput[] = pending
      ? [...anchors, { id: PENDING_ID, ...pending }]
      : [...anchors];
    editor.commands.setCommentAnchors(all, pending ? PENDING_ID : activeId);
    // `signature` stands for the three inputs.
  }, [editor, signature]);
}

/** Ids of the anchored comments whose text is no longer on the page. */
export function useDetachedComments(editor: Editor | null): readonly string[] {
  const joined = useEditorState({
    editor,
    selector: ({ editor: current }) =>
      current && !current.isDestroyed
        ? (commentAnchorStateOf(current.state)?.detached ?? []).join("|")
        : "",
  });
  return joined ? joined.split("|") : [];
}

/** Scrolls the highlighted text of a comment into view; false when detached. */
export function revealComment(editor: Editor, commentId: string): boolean {
  const placed = commentAnchorStateOf(editor.state)?.placed.find(
    (entry) => entry.id === commentId,
  );
  if (!placed) return false;
  const { node } = editor.view.domAtPos(placed.from);
  const element = node instanceof HTMLElement ? node : node.parentElement;
  element?.scrollIntoView?.({ block: "center", behavior: "smooth" });
  return true;
}
