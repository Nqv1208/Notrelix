import { Ban, Pencil, RotateCw, Trash2, X } from "lucide-react";
import { Avatar, AvatarFallback, Button, cn } from "@notrelix/ui-web";
import type { ChatMessage } from "../../core/types/chat";
import type { PendingMessage } from "../../core/model/chat-model";
import type { ChatCopy } from "./chat.copy";
import { avatarToneOf, initialsOf } from "./chat-initials";

export interface MessageAuthor {
  readonly name: string;
}

export interface ChatMessageRowProps {
  readonly message: ChatMessage;
  /** The author's name; a message from someone not in the Workspace list shows the fallback. */
  readonly author: MessageAuthor;
  /** A run of one author within minutes: no avatar and name line. */
  readonly continued: boolean;
  readonly isOwn: boolean;
  readonly locale: string;
  readonly timeZone?: string;
  readonly copy: ChatCopy;
  readonly onEdit: (message: ChatMessage) => void;
  readonly onDelete: (message: ChatMessage) => void;
}

export function formatClock(iso: string, locale: string, timeZone?: string) {
  return new Intl.DateTimeFormat(locale, {
    hour: "2-digit",
    minute: "2-digit",
    timeZone,
  }).format(new Date(iso));
}

/** The avatar column width; the continued rows keep the same gutter so the text lines up. */
const GUTTER = "w-9 shrink-0";

/** A message the server stored: text, edited flag, tombstone, or a system line. */
export function ChatMessageRow({
  message,
  author,
  continued,
  isOwn,
  locale,
  timeZone,
  copy,
  onEdit,
  onDelete,
}: ChatMessageRowProps) {
  const time = formatClock(message.createdAt, locale, timeZone);
  const isSystem = message.kind === "system";

  if (isSystem) {
    return (
      <div
        className="flex items-center gap-2 px-4 py-1 text-sm text-muted-foreground"
        data-testid="chat-system-message"
      >
        <span className="sr-only">{copy.list.systemLabel}: </span>
        <span className="italic">{author.name}</span>
        <span>{message.content}</span>
        <time dateTime={message.createdAt} className="tabular-nums">
          {time}
        </time>
      </div>
    );
  }

  const showToolbar = !message.deleted && isOwn;

  return (
    <div
      className={cn(
        "group relative flex gap-3 px-4 hover:bg-muted/50",
        continued ? "py-0.5" : "mt-3 pt-0.5 pb-0.5",
      )}
    >
      <div className={GUTTER}>
        {continued ? (
          <time
            dateTime={message.createdAt}
            className="invisible block pt-1 text-xs tabular-nums text-muted-foreground group-hover:visible"
          >
            {time}
          </time>
        ) : (
          <Avatar className="size-9" aria-hidden="true">
            <AvatarFallback
              className={cn(
                "text-xs font-semibold",
                avatarToneOf(message.authorId),
              )}
            >
              {initialsOf(author.name)}
            </AvatarFallback>
          </Avatar>
        )}
      </div>
      <div className="min-w-0 flex-1">
        {continued ? null : (
          <div className="flex flex-wrap items-baseline gap-x-2">
            <span className="text-sm font-semibold text-foreground">
              {author.name}
              {isOwn ? (
                <span className="sr-only"> ({copy.header.you})</span>
              ) : null}
            </span>
            <time
              dateTime={message.createdAt}
              className="text-xs tabular-nums text-muted-foreground"
            >
              {time}
            </time>
          </div>
        )}
        {message.deleted ? (
          <p className="flex items-center gap-1.5 text-sm italic text-muted-foreground">
            <Ban className="size-3.5 shrink-0" aria-hidden="true" />
            {copy.list.deleted}
          </p>
        ) : (
          <p className="whitespace-pre-wrap break-words text-sm leading-relaxed text-foreground">
            {message.content}
            {message.edited ? (
              <span className="ms-1.5 text-xs text-muted-foreground">
                ({copy.list.edited})
              </span>
            ) : null}
          </p>
        )}
      </div>
      {showToolbar ? (
        <div
          role="toolbar"
          aria-label={fillActions(copy, author.name)}
          className="absolute end-4 -top-3 z-10 flex items-center gap-0.5 rounded-md border border-border bg-background p-0.5 shadow-sm invisible group-focus-within:visible group-hover:visible"
        >
          <Button
            variant="ghost"
            size="icon-sm"
            aria-label={copy.list.edit}
            title={copy.list.edit}
            onClick={() => onEdit(message)}
          >
            <Pencil aria-hidden="true" />
          </Button>
          <Button
            variant="ghost"
            size="icon-sm"
            className="text-destructive hover:text-destructive"
            aria-label={copy.list.delete}
            title={copy.list.delete}
            onClick={() => onDelete(message)}
          >
            <Trash2 aria-hidden="true" />
          </Button>
        </div>
      ) : null}
    </div>
  );
}

function fillActions(copy: ChatCopy, author: string): string {
  return copy.list.actions.replace("{{author}}", author);
}

export interface ChatPendingRowProps {
  readonly pending: PendingMessage;
  readonly author: MessageAuthor;
  readonly locale: string;
  readonly timeZone?: string;
  readonly copy: ChatCopy;
  readonly onRetry: (pending: PendingMessage) => void;
  readonly onDiscard: (clientMessageId: string) => void;
}

/** A send the user made that the server has not stored: sending, failed or refused. */
export function ChatPendingRow({
  pending,
  author,
  locale,
  timeZone,
  copy,
  onRetry,
  onDiscard,
}: ChatPendingRowProps) {
  const failed = pending.status === "failed";
  return (
    <div
      className="mt-3 flex gap-3 px-4 pt-0.5 pb-0.5"
      data-testid="chat-pending-message"
      data-status={pending.status}
    >
      <div className={GUTTER}>
        <Avatar className="size-9" aria-hidden="true">
          <AvatarFallback
            className={cn(
              "text-xs font-semibold",
              avatarToneOf(pending.authorId),
            )}
          >
            {initialsOf(author.name)}
          </AvatarFallback>
        </Avatar>
      </div>
      <div className="min-w-0 flex-1">
        <div className="flex flex-wrap items-baseline gap-x-2">
          <span className="text-sm font-semibold text-foreground">
            {author.name}
          </span>
          <time
            dateTime={pending.createdAt}
            className="text-xs tabular-nums text-muted-foreground"
          >
            {formatClock(pending.createdAt, locale, timeZone)}
          </time>
          <span
            className={cn(
              "text-xs font-medium",
              failed ? "text-destructive" : "text-muted-foreground",
            )}
          >
            {failed ? copy.list.notSent : copy.list.sending}
          </span>
        </div>
        <p
          className={cn(
            "whitespace-pre-wrap break-words text-sm leading-relaxed",
            failed ? "text-muted-foreground" : "text-muted-foreground/80",
          )}
        >
          {pending.content}
        </p>
        {failed ? (
          <div className="mt-1 flex flex-wrap items-center gap-2 text-xs">
            {pending.failure === "refused" ? (
              <span className="text-destructive">{copy.list.refusedSend}</span>
            ) : (
              <Button
                type="button"
                variant="outline"
                size="sm"
                onClick={() => onRetry(pending)}
              >
                <RotateCw aria-hidden="true" />
                {copy.list.retrySend}
              </Button>
            )}
            <Button
              type="button"
              variant="ghost"
              size="sm"
              onClick={() => onDiscard(pending.clientMessageId)}
            >
              <X aria-hidden="true" />
              {copy.list.discardSend}
            </Button>
          </div>
        ) : null}
      </div>
    </div>
  );
}
