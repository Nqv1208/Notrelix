import type { ThreadCopy } from "../comment-thread/comment-thread.copy";

/**
 * The words a refused resolve or reopen shows (UC-COL-A07, A08): 404 when the item is no longer visible to the
 * person, otherwise the 403 that names who may change the thread.
 */
export function threadRefusalMessage(error: unknown, copy: ThreadCopy): string {
  const status = (error as { status?: unknown } | null)?.status;
  return status === 404 ? copy.resolveNotFound : copy.resolveRefused;
}
