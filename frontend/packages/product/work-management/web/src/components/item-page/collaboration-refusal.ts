import type { ItemPageCopy } from "./item-page.copy";

const BY_CODE = {
  "collaboration.comment_author_only": "commentAuthorOnly",
  "collaboration.comment_thread_starter_or_editor_only":
    "threadStarterOrEditorOnly",
  "collaboration.attachment_editor_only": "attachmentEditorOnly",
} as const;

/**
 * The words for a comment or file write the server refused: the 403 code names
 * who may do it, and a 404 means the viewer can no longer see the item.
 */
export function collaborationRefusalMessage(
  error: unknown,
  toasts: ItemPageCopy["toasts"],
): string {
  const { code, status } = (error ?? {}) as { code?: string; status?: number };
  const key = code
    ? (BY_CODE as Record<string, keyof typeof toasts>)[code]
    : undefined;
  if (key) return toasts[key];
  return status === 404 ? toasts.targetGone : toasts.commentFailed;
}
