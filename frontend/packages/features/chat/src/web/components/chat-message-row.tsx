import { Ellipsis, Pencil, RotateCw, Trash2, X } from "lucide-react";
import {
  Avatar,
  AvatarFallback,
  Button,
  cn,
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@notrelix/ui-web";
import type { ChatMessage } from "../../core/types/chat";
import type { PendingMessage } from "../../core/model/chat-model";
import type { ChatCopy } from "./chat.copy";
import { initialsOf } from "./chat-initials";
import { fillCopy } from "./use-chat-copy";

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

  return (
    <div
      className={cn(
        "group relative flex gap-3 px-4 py-0.5 hover:bg-muted/40",
        continued ? "" : "mt-3",
      )}
    >
      <div className="w-8 shrink-0 pt-0.5">
        {continued ? null : (
          <Avatar className="size-8" aria-hidden="true">
            <AvatarFallback className="text-xs">
              {initialsOf(author.name)}
            </AvatarFallback>
          </Avatar>
        )}
      </div>
      <div className="min-w-0 flex-1">
        {continued ? null : (
          <div className="flex items-baseline gap-2">
            <span className="font-semibold text-foreground">
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
          <p className="text-sm italic text-muted-foreground">
            {copy.list.deleted}
          </p>
        ) : (
          <p className="whitespace-pre-wrap break-words text-sm text-foreground">
            {message.content}
            {message.edited ? (
              <span className="ms-1.5 text-xs text-muted-foreground">
                ({copy.list.edited})
              </span>
            ) : null}
          </p>
        )}
      </div>
      {!message.deleted && isOwn ? (
        <div className="absolute end-3 -top-2 opacity-0 transition-opacity group-hover:opacity-100 focus-within:opacity-100">
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button
                variant="ghost"
                size="icon-sm"
                aria-label={fillCopy(copy.list.actions, {
                  author: author.name,
                })}
              >
                <Ellipsis aria-hidden="true" />
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end">
              <DropdownMenuItem onSelect={() => onEdit(message)}>
                <Pencil aria-hidden="true" />
                {copy.list.edit}
              </DropdownMenuItem>
              <DropdownMenuItem
                variant="destructive"
                onSelect={() => onDelete(message)}
              >
                <Trash2 aria-hidden="true" />
                {copy.list.delete}
              </DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>
        </div>
      ) : null}
    </div>
  );
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
      className={cn(
        "flex gap-3 px-4 py-0.5 mt-3",
        failed ? "text-destructive" : "text-muted-foreground",
      )}
      data-testid="chat-pending-message"
      data-status={pending.status}
    >
      <div className="w-8 shrink-0" aria-hidden="true" />
      <div className="min-w-0 flex-1">
        <div className="flex items-baseline gap-2">
          <span className="font-semibold text-foreground">{author.name}</span>
          <time
            dateTime={pending.createdAt}
            className="text-xs tabular-nums text-muted-foreground"
          >
            {formatClock(pending.createdAt, locale, timeZone)}
          </time>
          <span className="text-xs">
            {failed ? copy.list.notSent : copy.list.sending}
          </span>
        </div>
        <p className="whitespace-pre-wrap break-words text-sm">
          {pending.content}
        </p>
        {failed ? (
          <div className="mt-1 flex flex-wrap items-center gap-2 text-xs">
            {pending.failure === "refused" ? (
              <span>{copy.list.refusedSend}</span>
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
