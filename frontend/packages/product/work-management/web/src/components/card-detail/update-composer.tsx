import { onSessionReset } from "@notrelix/work-management-state";
import { useRef, useState, type KeyboardEvent } from "react";
import type { TaskDetailCallbacks } from "./task-detail-types";

/** A repeat inside this window is a retry of the same click; later it is a new comment. */
const REPEAT_WINDOW_MS = 2 * 60_000;

/** Unsent comment text by card, kept past the composer so a card that vanishes under it does not take the text along. */
const unsentComments = new Map<string, string>();
onSessionReset(() => unsentComments.clear());

/** What the person had typed on a card and not sent ("" when nothing). */
export function unsentCommentOf(cardId: string): string {
  return unsentComments.get(cardId) ?? "";
}

/**
 * The comment draft the composer uses: the text, the people it mentions
 * and the send state. ⌘↵ (or Ctrl+↵) submits; a failed post keeps the text
 * for a retry; typing "@" opens the people picker and a pick inserts the name
 * and records the mention.
 */
export function useCommentDraft({
  cardId,
  disabled = false,
  onCreateUpdate,
}: {
  cardId: string;
  disabled?: boolean;
  onCreateUpdate: TaskDetailCallbacks["onCreateUpdate"];
}) {
  const [body, setBodyState] = useState(() => unsentCommentOf(cardId));
  const setBody = (next: string | ((current: string) => string)) =>
    setBodyState((current) => {
      const value = typeof next === "function" ? next(current) : next;
      if (value) unsentComments.set(cardId, value);
      else unsentComments.delete(cardId);
      return value;
    });
  const [mentionUserIds, setMentionUserIds] = useState<string[]>([]);
  const [isSubmitting, setIsSubmitting] = useState(false);
  // The key of a post sent and not answered: a retry of the same text within the window repeats it.
  const sent = useRef<{ signature: string; key: string; at: number } | null>(
    null,
  );
  const canSubmit = body.trim().length > 0 && !disabled && !isSubmitting;

  function submit() {
    const nextBody = body.trim();
    if (!nextBody || disabled || isSubmitting) return;
    setIsSubmitting(true);
    const signature = JSON.stringify([nextBody, mentionUserIds]);
    const kept = sent.current;
    const key =
      kept?.signature === signature && Date.now() - kept.at < REPEAT_WINDOW_MS
        ? kept.key
        : crypto.randomUUID();
    sent.current = { signature, key, at: Date.now() };
    onCreateUpdate(
      {
        cardId,
        body: nextBody,
        mentionUserIds,
        attachmentIds: [],
        idempotencyKey: key,
      },
      {
        onSuccess: () => {
          sent.current = null;
          setBody("");
          setMentionUserIds([]);
        },
        // Success or failure: a failed post keeps its text for a retry.
        onSettled: () => setIsSubmitting(false),
      },
    );
  }

  function handleKeyDown(event: KeyboardEvent<HTMLTextAreaElement>) {
    if (event.key === "Enter" && (event.metaKey || event.ctrlKey)) {
      event.preventDefault();
      submit();
    }
  }

  function changeBody(next: string) {
    setBody(next);
  }

  /** Replaces the typed "@…" at `range` with the chosen name and records who is reached. */
  function insertMention(
    range: { readonly from: number; readonly to: number },
    label: string,
    userIds: readonly string[],
  ) {
    setBody(
      (current) =>
        `${current.slice(0, range.from)}@${label} ${current.slice(range.to)}`,
    );
    setMentionUserIds((current) => [
      ...current,
      ...userIds.filter((id) => !current.includes(id)),
    ]);
  }

  return {
    body,
    canSubmit,
    changeBody,
    handleKeyDown,
    insertMention,
    submit,
  };
}
