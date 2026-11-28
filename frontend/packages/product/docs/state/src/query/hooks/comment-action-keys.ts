import type { CreateCommentPayload } from "@notrelix/docs-core";

/** A repeat of the same comment within this window is the same action (UC-COL-H01). */
export const COMMENT_REPEAT_WINDOW_MS = 2 * 60_000;

/**
 * One Idempotency-Key per comment the person means to post. A second send of
 * the same text, place and mentions while the first is unanswered or failed
 * (a double click, a retry) reuses the key, so the producer replays the first
 * comment instead of storing another. A success forgets the key: posting the
 * same words again later is a new comment.
 */
export function createCommentActionKeys(
  now: () => number = Date.now,
  newKey: () => string = () => crypto.randomUUID(),
) {
  const sent = new Map<string, { key: string; at: number }>();
  return {
    keyFor(payload: CreateCommentPayload): string {
      const signature = signatureOf(payload);
      const kept = sent.get(signature);
      const key =
        kept && now() - kept.at < COMMENT_REPEAT_WINDOW_MS
          ? kept.key
          : newKey();
      sent.set(signature, { key, at: kept?.key === key ? kept.at : now() });
      return key;
    },
    succeeded(payload: CreateCommentPayload): void {
      sent.delete(signatureOf(payload));
    },
  };
}

function signatureOf(payload: CreateCommentPayload): string {
  return JSON.stringify([
    payload.pageId,
    payload.body.trim(),
    payload.parentId ?? null,
    payload.anchor ?? null,
    [...(payload.mentionIds ?? [])].sort(),
  ]);
}
