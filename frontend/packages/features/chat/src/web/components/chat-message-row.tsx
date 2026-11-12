import { useRef, type KeyboardEvent } from "react";
import {
  Ban,
  MessagesSquare,
  MessageSquareText,
  Pencil,
  Pin,
  PinOff,
  Reply,
  RotateCw,
  Trash2,
  X,
} from "lucide-react";
import { Avatar, AvatarFallback, Button, cn } from "@notrelix/ui-web";
import type { ChatMessage, ChatReactionSummary } from "../../core/types/chat";
import type { PendingMessage } from "../../core/model/chat-model";
import type { ChatHooks } from "../query/create-chat-hooks";
import type { ChatCopy } from "./chat.copy";
import { avatarToneOf, initialsOf } from "./chat-initials";
import { mentionsViewer } from "../../core/model/mention-model";
import { ChatMentionText } from "./chat-mention-text";
import { useChatMentions } from "./chat-mention-context";
import { MessageAttachments } from "./chat-attachments";
import { ReactionChips, ReactionPicker } from "./chat-reactions";
import { fillCopy, pluralCopy } from "./use-chat-copy";

export interface MessageAuthor {
  readonly name: string;
}

/** The quoted message of a reply, as the row shows it. */
export interface QuoteView {
  readonly name: string;
  readonly text: string | null;
  readonly deleted: boolean;
  /** The quoted message cannot be read here (not loaded and no live preview). */
  readonly unavailable: boolean;
}

/** The thread summary of a root message as the row shows it. */
export interface ThreadChipView {
  readonly replyCount: number;
  readonly lastReplyAt: string | null;
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
  /** Edit and delete of the caller's own message; without a handler the action is not offered. */
  readonly onEdit?: (message: ChatMessage) => void;
  readonly onDelete?: (message: ChatMessage) => void;
  /** The message this one replies to (quote preview), when it replies. */
  readonly quote?: QuoteView | null;
  /** The reply count and the latest reply of a thread root; no chip when absent or zero. */
  readonly thread?: ThreadChipView | null;
  readonly pinned?: boolean;
  readonly reactions?: readonly ChatReactionSummary[];
  /** The workspace and the hooks the reaction popover reads through. */
  readonly workspaceId?: string;
  readonly hooks?: ChatHooks;
  readonly authorName?: (userId: string) => string;
  /** Toolbar actions. Without a handler the action is not offered. */
  readonly onReact?: (
    message: ChatMessage,
    emoji: string,
    add: boolean,
  ) => void;
  readonly onReply?: (message: ChatMessage) => void;
  readonly onOpenThread?: (message: ChatMessage) => void;
  readonly onTogglePin?: (message: ChatMessage, pinned: boolean) => void;
  readonly onOpenQuote?: (messageId: string) => void;
  /** An archived channel is read-only: its files cannot be removed. */
  readonly attachmentsReadOnly?: boolean;
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
  quote,
  thread,
  pinned = false,
  reactions = [],
  workspaceId,
  hooks,
  authorName,
  onReact,
  onReply,
  onOpenThread,
  onTogglePin,
  onOpenQuote,
  attachmentsReadOnly = false,
}: ChatMessageRowProps) {
  const time = formatClock(message.createdAt, locale, timeZone);
  const isSystem = message.kind === "system";
  const toolbarRef = useRef<HTMLDivElement | null>(null);
  const mentionContext = useChatMentions();
  const mentionsMe =
    mentionContext.viewerId !== null &&
    mentionsViewer(message, mentionContext.viewerId);

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

  const live = !message.deleted;
  const reacted = reactions
    .filter((entry) => entry.reactedByMe)
    .map((entry) => entry.emoji);
  const canReact = live && Boolean(onReact);
  const showToolbar =
    live &&
    (Boolean(onReply) ||
      Boolean(onOpenThread) ||
      Boolean(onTogglePin) ||
      canReact ||
      isOwn);

  // Arrow keys move between the toolbar buttons (the toolbar is one Tab stop).
  function handleToolbarKeyDown(event: KeyboardEvent<HTMLDivElement>) {
    if (!["ArrowLeft", "ArrowRight", "Home", "End"].includes(event.key)) return;
    const buttons = Array.from(
      toolbarRef.current?.querySelectorAll<HTMLButtonElement>("button") ?? [],
    ).filter((button) => !button.disabled);
    if (buttons.length === 0) return;
    const at = buttons.indexOf(document.activeElement as HTMLButtonElement);
    let next = at;
    if (event.key === "ArrowRight")
      next = (at + 1 + buttons.length) % buttons.length;
    if (event.key === "ArrowLeft")
      next = (at - 1 + buttons.length) % buttons.length;
    if (event.key === "Home") next = 0;
    if (event.key === "End") next = buttons.length - 1;
    event.preventDefault();
    buttons[next]?.focus();
  }

  const threadCount = thread?.replyCount ?? 0;

  return (
    <div
      className={cn(
        "group relative flex gap-3 px-4 hover:bg-muted/50",
        continued ? "py-0.5" : "mt-3 pt-0.5 pb-0.5",
        mentionsMe && "border-s-2 border-warning bg-warning-subtle/40",
      )}
      data-message-id={message.id}
      data-mentions-me={mentionsMe ? "true" : undefined}
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
            {pinned ? (
              <span className="inline-flex items-center gap-1 text-xs text-muted-foreground">
                <Pin className="size-3" aria-hidden="true" />
                {copy.pins.pinned}
              </span>
            ) : null}
          </div>
        )}
        {quote ? (
          <QuoteBlock
            quote={quote}
            copy={copy}
            onOpen={
              onOpenQuote
                ? () => onOpenQuote(message.replyToId ?? "")
                : undefined
            }
          />
        ) : null}
        {message.deleted ? (
          <p className="flex items-center gap-1.5 text-sm italic text-muted-foreground">
            <Ban className="size-3.5 shrink-0" aria-hidden="true" />
            {copy.list.deleted}
          </p>
        ) : (
          <p className="whitespace-pre-wrap break-words text-sm leading-relaxed text-foreground">
            {mentionsMe ? (
              <span className="sr-only">{copy.mentions.forYou}: </span>
            ) : null}
            <ChatMentionText
              content={message.content ?? ""}
              mentions={message.mentions}
              copy={copy}
            />
            {message.edited ? (
              <span className="ms-1.5 text-xs text-muted-foreground">
                ({copy.list.edited})
              </span>
            ) : null}
          </p>
        )}
        {live && hooks && workspaceId ? (
          <MessageAttachments
            message={message}
            workspaceId={workspaceId}
            hooks={hooks}
            isOwn={isOwn}
            readOnly={attachmentsReadOnly}
            copy={copy}
          />
        ) : null}
        {live && isOwn ? <SentNotices message={message} copy={copy} /> : null}
        {live && hooks && workspaceId && authorName ? (
          <ReactionChips
            messageId={message.id}
            workspaceId={workspaceId}
            reactions={reactions}
            hooks={hooks}
            authorName={authorName}
            copy={copy}
            onToggle={(emoji, add) => onReact?.(message, emoji, add)}
          />
        ) : null}
        {threadCount > 0 && onOpenThread ? (
          <button
            type="button"
            onClick={() => onOpenThread(message)}
            className="mt-1 inline-flex items-center gap-1.5 rounded-md px-1.5 py-0.5 text-xs font-medium text-primary hover:bg-muted focus-visible:ring-2 focus-visible:ring-focus-ring focus-visible:outline-none"
            aria-label={`${pluralCopy(
              {
                one: copy.thread.replies_one,
                other: copy.thread.replies_other,
              },
              threadCount,
              { count: threadCount },
            )}, ${copy.thread.openThread}`}
            data-testid="chat-thread-chip"
          >
            <MessageSquareText className="size-3.5" aria-hidden="true" />
            <span>
              {fillCopy(
                threadCount === 1
                  ? copy.thread.replies_one
                  : copy.thread.replies_other,
                { count: threadCount },
              )}
            </span>
            {thread?.lastReplyAt ? (
              <time
                dateTime={thread.lastReplyAt}
                className="text-muted-foreground"
              >
                {fillCopy(copy.thread.latest, {
                  time: formatClock(thread.lastReplyAt, locale, timeZone),
                })}
              </time>
            ) : null}
          </button>
        ) : null}
      </div>
      {showToolbar ? (
        <div
          ref={toolbarRef}
          role="toolbar"
          aria-label={fillActions(copy, author.name)}
          onKeyDown={handleToolbarKeyDown}
          className="absolute end-4 -top-3 z-10 flex items-center gap-0.5 rounded-md border border-border bg-background p-0.5 shadow-sm invisible group-focus-within:visible group-hover:visible"
        >
          {canReact ? (
            <ReactionPicker
              copy={copy}
              reacted={reacted}
              onPick={(emoji) =>
                onReact?.(message, emoji, !reacted.includes(emoji))
              }
            />
          ) : null}
          {onReply && live ? (
            <Button
              variant="ghost"
              size="icon-sm"
              aria-label={copy.toolbar.reply}
              title={copy.toolbar.reply}
              onClick={() => onReply(message)}
            >
              <Reply aria-hidden="true" />
            </Button>
          ) : null}
          {onOpenThread && live ? (
            <Button
              variant="ghost"
              size="icon-sm"
              aria-label={copy.toolbar.openThread}
              title={copy.toolbar.openThread}
              onClick={() => onOpenThread(message)}
            >
              <MessagesSquare aria-hidden="true" />
            </Button>
          ) : null}
          {onTogglePin && live ? (
            <Button
              variant="ghost"
              size="icon-sm"
              aria-label={pinned ? copy.toolbar.unpin : copy.toolbar.pin}
              aria-pressed={pinned}
              title={pinned ? copy.toolbar.unpin : copy.toolbar.pin}
              onClick={() => onTogglePin(message, !pinned)}
            >
              {pinned ? (
                <PinOff aria-hidden="true" />
              ) : (
                <Pin aria-hidden="true" />
              )}
            </Button>
          ) : null}
          {isOwn && live && onEdit && onDelete ? (
            <>
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
            </>
          ) : null}
        </div>
      ) : null}
    </div>
  );
}

function QuoteBlock({
  quote,
  copy,
  onOpen,
}: {
  readonly quote: QuoteView;
  readonly copy: ChatCopy;
  readonly onOpen?: () => void;
}) {
  const text = quote.unavailable
    ? copy.quote.unavailable
    : quote.deleted
      ? copy.quote.deleted
      : (quote.text ?? "");
  const body = (
    <>
      <span className="font-semibold">
        {fillCopy(copy.quote.quoteOf, { name: quote.name })}
      </span>
      <span className="min-w-0 truncate">{text}</span>
    </>
  );
  return (
    <div className="mb-1 border-s-2 border-border ps-2 text-xs text-muted-foreground">
      {onOpen ? (
        <button
          type="button"
          onClick={onOpen}
          className="flex min-w-0 max-w-full items-center gap-1.5 text-start hover:text-foreground focus-visible:ring-2 focus-visible:ring-focus-ring focus-visible:outline-none"
        >
          {body}
        </button>
      ) : (
        <div className="flex min-w-0 max-w-full items-center gap-1.5">
          {body}
        </div>
      )}
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

/**
 * What the send or edit of this message did not do for a mention (UC-CHT-G02, G03): the person outside the channel was not
 * notified, a broadcast reached nobody or stayed text. Only the sender's own copy carries notices; a reload drops them.
 */
function SentNotices({
  message,
  copy,
}: {
  readonly message: ChatMessage;
  readonly copy: ChatCopy;
}) {
  const context = useChatMentions();
  const lines: string[] = [];
  for (const notice of message.mentionNotices ?? []) {
    const covered = (message.content ?? "").slice(
      notice.start,
      notice.start + notice.length,
    );
    if (notice.code === "cannot-view") {
      const name =
        (notice.userId ? context.personName(notice.userId) : null) ??
        covered.replace(/^@/, "");
      lines.push(fillCopy(copy.mentions.sentNotice, { name }));
    } else if (notice.code === "no-audience") {
      lines.push(fillCopy(copy.mentions.sentNoAudience, { token: covered }));
    } else if (notice.code === "broadcast-not-allowed") {
      lines.push(fillCopy(copy.mentions.sentNotAllowed, { token: covered }));
    }
  }
  if (lines.length === 0) return null;
  return (
    <ul
      className="mt-1 flex flex-col gap-0.5 text-xs text-muted-foreground"
      data-testid="chat-sent-notices"
    >
      {lines.map((line, index) => (
        <li key={index}>{line}</li>
      ))}
    </ul>
  );
}
