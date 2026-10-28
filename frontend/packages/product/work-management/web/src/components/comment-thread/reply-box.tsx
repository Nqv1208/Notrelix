import { useEffect, useRef, useState } from "react";
import { Textarea, cn } from "@notrelix/ui-web";
import type { ThreadCopy } from "./comment-thread.copy";
import type { TaskDetailCallbacks } from "../card-detail/task-detail-types";

const BUTTON_CLASS =
  "inline-flex h-7 items-center rounded-[7px] border px-2.5 text-crumb font-medium outline-none focus-visible:ring-2 focus-visible:ring-focus-ring";

/**
 * The inline box under a thread where a reply is written. It grows in place,
 * takes focus, and goes away on send or cancel; a failed post keeps the text
 * for a retry. ⌘↵ (or Ctrl+↵) sends.
 */
export function ReplyBox({
  cardId,
  parentCommentId,
  authorName,
  copy,
  onCreateUpdate,
  onClose,
}: {
  cardId: string;
  /** The top-level comment the reply attaches to. */
  parentCommentId: string;
  authorName: string;
  copy: ThreadCopy;
  onCreateUpdate: TaskDetailCallbacks["onCreateUpdate"];
  onClose: () => void;
}) {
  const [body, setBody] = useState("");
  const [sending, setSending] = useState(false);
  const field = useRef<HTMLTextAreaElement>(null);
  const canSend = body.trim().length > 0 && !sending;
  useEffect(() => field.current?.focus(), []);

  function send() {
    if (!canSend) return;
    setSending(true);
    onCreateUpdate(
      {
        cardId,
        body: body.trim(),
        mentionUserIds: [],
        attachmentIds: [],
        parentCommentId,
      },
      { onSuccess: onClose, onSettled: () => setSending(false) },
    );
  }

  return (
    <div className="mt-2 flex flex-col gap-2">
      <Textarea
        ref={field}
        value={body}
        rows={2}
        aria-label={copy.replyTo.replace("{{name}}", authorName)}
        placeholder={copy.replyPlaceholder}
        onChange={(event) => setBody(event.target.value)}
        onKeyDown={(event) => {
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
