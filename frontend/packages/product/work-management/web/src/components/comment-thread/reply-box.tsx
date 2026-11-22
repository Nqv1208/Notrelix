import { useEffect, useState } from "react";
import { Textarea, cn } from "@notrelix/ui-web";
import type { ThreadCopy } from "./comment-thread.copy";
import type { TaskDetailCallbacks } from "../card-detail/task-detail-types";
import type { ItemPageCopy } from "../item-page/item-page.copy";
import { MentionList, useMentionField } from "../item-page/item-page-mention";

const BUTTON_CLASS =
  "inline-flex h-7 items-center rounded-[7px] border px-2.5 text-crumb font-medium outline-none focus-visible:ring-2 focus-visible:ring-focus-ring";

/**
 * The inline box under a thread where a reply is written. It grows in place,
 * takes focus, and goes away on send or cancel; a failed post keeps the text
 * for a retry. ⌘↵ (or Ctrl+↵) sends. Typing "@" offers the people the server
 * lists for the item (only people who can view it); the picked people go out
 * with the reply, for the names still in the text.
 */
export function ReplyBox({
  cardId,
  workspaceId,
  parentCommentId,
  authorName,
  copy,
  mentionCopy,
  onCreateUpdate,
  onClose,
}: {
  cardId: string;
  workspaceId: string;
  /** The top-level comment the reply attaches to. */
  parentCommentId: string;
  authorName: string;
  copy: ThreadCopy;
  mentionCopy: ItemPageCopy;
  onCreateUpdate: TaskDetailCallbacks["onCreateUpdate"];
  onClose: () => void;
}) {
  const [body, setBody] = useState("");
  const [sending, setSending] = useState(false);
  const [mentions, setMentions] = useState<{ id: string; name: string }[]>([]);
  const canSend = body.trim().length > 0 && !sending;
  const mention = useMentionField({
    draft: {
      body,
      changeBody: setBody,
      insertMention: (range, label, userIds) => {
        setBody(
          (current) =>
            `${current.slice(0, range.from)}@${label} ${current.slice(range.to)}`,
        );
        setMentions((current) => [
          ...current,
          ...userIds.map((id) => ({ id, name: label })),
        ]);
      },
    },
    workspaceId,
    itemId: cardId,
  });
  const { fieldRef } = mention;
  useEffect(() => fieldRef.current?.focus(), [fieldRef]);

  function send() {
    if (!canSend) return;
    setSending(true);
    onCreateUpdate(
      {
        cardId,
        body: body.trim(),
        // Only people whose "@name" is still in the text are reached.
        mentionUserIds: [
          ...new Set(
            mentions
              .filter((entry) => body.includes(`@${entry.name}`))
              .map((entry) => entry.id),
          ),
        ],
        attachmentIds: [],
        parentCommentId,
      },
      { onSuccess: onClose, onSettled: () => setSending(false) },
    );
  }

  return (
    <div className="relative mt-2 flex flex-col gap-2">
      {mention.listOpen ? (
        <MentionList
          candidates={mention.candidates}
          status={mention.status}
          query={mention.token?.query ?? ""}
          activeIndex={mention.activeIndex}
          copy={mentionCopy}
          onPick={mention.pick}
          onHover={mention.setActive}
          onRetry={mention.retry}
        />
      ) : null}
      <Textarea
        ref={fieldRef}
        value={body}
        rows={2}
        aria-label={copy.replyTo.replace("{{name}}", authorName)}
        placeholder={copy.replyPlaceholder}
        aria-controls={mention.listOpen ? "item-page-mention-list" : undefined}
        onChange={(event) => {
          setBody(event.target.value);
          mention.readToken(
            event.target.value,
            event.target.selectionStart ?? event.target.value.length,
          );
        }}
        onClick={(event) =>
          mention.readToken(
            event.currentTarget.value,
            event.currentTarget.selectionStart,
          )
        }
        onBlur={mention.dismiss}
        onKeyDown={(event) => {
          if (mention.handleKey(event)) return;
          if (event.key === "Enter" && (event.metaKey || event.ctrlKey)) {
            event.preventDefault();
            send();
          }
          if (event.key === "Escape") onClose();
        }}
        className="min-h-12 resize-none text-item-text"
      />
      <div className="flex justify-end gap-1.5">
        <button
          type="button"
          onClick={onClose}
          className={cn(
            BUTTON_CLASS,
            "border-transparent bg-transparent text-muted-foreground hover:bg-state-hover-strong",
          )}
        >
          {copy.replyCancel}
        </button>
        <button
          type="button"
          disabled={!canSend}
          onClick={send}
          className={cn(
            BUTTON_CLASS,
            "border-primary bg-primary font-semibold text-primary-foreground hover:opacity-90 disabled:cursor-default disabled:opacity-50",
          )}
        >
          {copy.replySend}
        </button>
      </div>
    </div>
  );
}
