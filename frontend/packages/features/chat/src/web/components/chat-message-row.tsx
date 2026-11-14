import {
  useEffect,
  useRef,
  useState,
  type FocusEvent,
  type KeyboardEvent,
  type PointerEvent,
  type ReactNode,
} from "react";
import {
  Ban,
  Copy,
  MessageSquareText,
  MessagesSquare,
  MoreHorizontal,
  Pencil,
  Pin,
  PinOff,
  Reply,
  RotateCw,
  ShieldX,
  Trash2,
  X,
} from "lucide-react";
import { Avatar, AvatarFallback, Button, cn } from "@notrelix/ui-web";
import type { ChatMessage, ChatReactionSummary } from "../../core/types/chat";
import type { PendingMessage } from "../../core/model/chat-model";
import { emojiOnlyCount } from "../../core/model/message-display-model";
import type { ChatHooks } from "../query/create-chat-hooks";
import type { ChatCopy } from "./chat.copy";
import { avatarToneOf, initialsOf } from "./chat-initials";
import { mentionsViewer } from "../../core/model/mention-model";
import { ChatMentionText } from "./chat-mention-text";
import { useChatMentions } from "./chat-mention-context";
import { useChatHighlight } from "./chat-highlight-context";
import { MessageAttachments } from "./chat-attachments";
import { MessageLinkPreviews } from "./chat-link-previews";
import { ReactionChips } from "./chat-reactions";
import {
  MessageActionBar,
  MessageActionSheet,
  type MessageMenuItem,
} from "./chat-message-actions";
import { isKeyboardFocus, useRowActivity } from "./chat-row-activity";
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
  /** Who wrote the quoted message, for the "replied to you" caption; absent when unknown. */
  readonly authorId?: string;
}

/**
 * The thread summary of a root message as the row shows it. The reply count and the latest reply are what the
 * list knows today; the participants are optional (an avatar stack shows when a server sends them).
 */
export interface ThreadChipView {
  readonly replyCount: number;
  readonly lastReplyAt: string | null;
  readonly participantIds?: readonly string[];
}

export interface ChatMessageRowProps {
  readonly message: ChatMessage;
  /** The author's name; a message from someone not in the Workspace list shows the fallback. */
  readonly author: MessageAuthor;
  /** The message continues the group above it (same author within minutes): no name, a 2 px gap. */
  readonly continued: boolean;
  /** The last message of its group: the avatar and the status line sit here. Defaults to true. */
  readonly groupEnd?: boolean;
  /** Shows the author's name above a group of someone else's messages (channels and groups; not a 1-1 DM). */
  readonly showName?: boolean;
  /** The caller's newest stored message: "Sent" shows under it. */
  readonly showSent?: boolean;
  /** Fades the message in (a message that arrived while the list was open). */
  readonly fresh?: boolean;
  readonly isOwn: boolean;
  readonly locale: string;
  readonly timeZone?: string;
  readonly copy: ChatCopy;
  /** Edit and delete of the caller's own message; without a handler the action is not offered. */
  readonly onEdit?: (message: ChatMessage) => void;
  readonly onDelete?: (message: ChatMessage) => void;
  /** Delete as a moderator (UC-CHT-H03): offered on another person's live message; without a handler it is not offered. */
  readonly onModerate?: (message: ChatMessage) => void;
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
  /** Actions. Without a handler the action is not offered. */
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

/** Formatters are costly to build and every row formats twice: one per locale, zone and style. */
const FORMATTERS = new Map<string, Intl.DateTimeFormat>();

function formatter(
  style: "clock" | "full",
  locale: string,
  timeZone?: string,
): Intl.DateTimeFormat {
  const key = `${style}|${locale}|${timeZone ?? ""}`;
  let found = FORMATTERS.get(key);
  if (!found) {
    found = new Intl.DateTimeFormat(
      locale,
      style === "clock"
        ? { hour: "numeric", minute: "2-digit", timeZone }
        : {
            weekday: "long",
            day: "numeric",
            month: "long",
            year: "numeric",
            hour: "numeric",
            minute: "2-digit",
            timeZone,
          },
    );
    FORMATTERS.set(key, found);
  }
  return found;
}

/** The clock time of a message on one line ("9:05 AM", "09:05"), in the viewer's locale and zone. */
export function formatClock(iso: string, locale: string, timeZone?: string) {
  return formatter("clock", locale, timeZone).format(new Date(iso));
}

/** The full date and time of a message, for its tooltip. */
export function formatFullTime(iso: string, locale: string, timeZone?: string) {
  return formatter("full", locale, timeZone).format(new Date(iso));
}

/**
 * The corner radii of a bubble in its group (Messenger): 18 px all round, with the corners toward the
 * author's edge reduced to 6 px where the bubble touches the one above or below it.
 */
export function bubbleShape(
  isOwn: boolean,
  continued: boolean,
  groupEnd: boolean,
): string {
  const top = continued ? (isOwn ? "rounded-se-sm" : "rounded-ss-sm") : null;
  const bottom = groupEnd ? null : isOwn ? "rounded-ee-sm" : "rounded-es-sm";
  return cn("rounded-2xl", top, bottom);
}

/** The width a side action bar needs beside the bubble; with less room it sits above the bubble. */
const BAR_ROOM_PX = 208;
/** How long a touch must rest on a message to open its action sheet. */
const LONG_PRESS_MS = 450;
/** A touch that moves further than this is a scroll, not a long press. */
const LONG_PRESS_SLOP_PX = 10;

/** The avatar column of someone else's messages; rows without the avatar keep the gutter so the bubbles line up. */
const GUTTER = "w-7 shrink-0";

/** A message the server stored: a bubble, a tombstone, or a system line. */
export function ChatMessageRow({
  message,
  author,
  continued,
  groupEnd = true,
  showName = true,
  showSent = false,
  fresh = false,
  isOwn,
  locale,
  timeZone,
  copy,
  onEdit,
  onDelete,
  onModerate,
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
  const rowRef = useRef<HTMLDivElement | null>(null);
  const bubbleRef = useRef<HTMLDivElement | null>(null);
  const highlighted = useChatHighlight() === message.id;
  const { active, store } = useRowActivity(message.id);
  const heldRef = useRef(false);
  const [held, setHeld] = useState(false);
  const [placement, setPlacement] = useState<"side" | "top">("side");
  const [sheetOpen, setSheetOpen] = useState(false);
  const press = useRef<{
    timer: ReturnType<typeof setTimeout>;
    x: number;
    y: number;
  } | null>(null);
  const lastPointer = useRef<string | null>(null);
  // The message a search hit points at: scroll to it and move the reading focus there once.
  useEffect(() => {
    if (!highlighted) return;
    rowRef.current?.scrollIntoView?.({ block: "center" });
    rowRef.current?.focus({ preventScroll: true });
  }, [highlighted]);
  useEffect(
    () => () => {
      if (press.current) clearTimeout(press.current.timer);
    },
    [],
  );
  const mentionContext = useChatMentions();
  const mentionsMe =
    mentionContext.viewerId !== null &&
    mentionsViewer(message, mentionContext.viewerId);

  if (isSystem) {
    return (
      <div
        className="flex justify-center px-4 py-1"
        data-testid="chat-system-message"
      >
        <p className="inline-flex max-w-full flex-wrap items-center justify-center gap-x-1.5 rounded-full bg-muted px-3 py-1 text-center text-xs text-muted-foreground">
          <span className="sr-only">{copy.list.systemLabel}: </span>
          <span className="font-medium">{author.name}</span>
          <span className="wrap-anywhere">{message.content}</span>
          <time
            dateTime={message.createdAt}
            className="whitespace-nowrap tabular-nums"
          >
            {time}
          </time>
        </p>
      </div>
    );
  }

  const live = !message.deleted;
  const reacted = reactions
    .filter((entry) => entry.reactedByMe)
    .map((entry) => entry.emoji);
  const canReact = live && Boolean(onReact);
  const content = message.content ?? "";
  const items: MessageMenuItem[] = live
    ? buildMenuItems({
        message,
        copy,
        isOwn,
        pinned,
        content,
        onEdit,
        onDelete,
        onModerate,
        onOpenThread,
        onTogglePin,
      })
    : [];
  const replyHandler = live && onReply ? () => onReply(message) : undefined;
  const showToolbar =
    live && (canReact || Boolean(replyHandler) || items.length > 0);
  const visible = showToolbar && (active || held);
  const who = isOwn ? copy.list.you : author.name;
  const srWho = fillCopy(copy.list.messageBy, { name: who, time });
  const fullTime = formatFullTime(message.createdAt, locale, timeZone);
  const threadCount = thread?.replyCount ?? 0;
  const emojiCount =
    live && !quote && (message.mentions?.length ?? 0) === 0
      ? emojiOnlyCount(message.content)
      : 0;
  const shape = bubbleShape(isOwn, continued, groupEnd);
  const align = isOwn ? "end" : "start";

  function activate() {
    if (!showToolbar) return;
    const row = rowRef.current;
    const bubble = bubbleRef.current;
    if (row && bubble) {
      const r = row.getBoundingClientRect();
      const b = bubble.getBoundingClientRect();
      const room = isOwn ? b.left - r.left : r.right - b.right;
      setPlacement(r.width === 0 || room >= BAR_ROOM_PX ? "side" : "top");
    }
    store.activate(message.id);
  }

  function setHold(next: boolean) {
    heldRef.current = next;
    setHeld(next);
    if (next) {
      store.activate(message.id);
      return;
    }
    // A closed menu hands focus back to its trigger; the bar stays only while the pointer or the focus is here.
    requestAnimationFrame(() => {
      const row = rowRef.current;
      if (!row) return;
      let hovered = false;
      try {
        hovered = row.matches(":hover");
      } catch {
        hovered = false;
      }
      if (!hovered && !row.contains(document.activeElement)) {
        store.release(message.id);
      }
    });
  }

  function cancelPress() {
    if (press.current) clearTimeout(press.current.timer);
    press.current = null;
  }

  function openSheet() {
    cancelPress();
    store.clear();
    setSheetOpen(true);
  }

  const handlers = {
    onPointerEnter: (event: PointerEvent<HTMLDivElement>) => {
      lastPointer.current = event.pointerType;
      if (event.pointerType !== "touch") activate();
    },
    onPointerLeave: () => {
      cancelPress();
      if (!heldRef.current) store.release(message.id);
    },
    onPointerDown: (event: PointerEvent<HTMLDivElement>) => {
      lastPointer.current = event.pointerType;
      if (event.pointerType !== "touch" || !showToolbar) return;
      cancelPress();
      press.current = {
        x: event.clientX,
        y: event.clientY,
        timer: setTimeout(openSheet, LONG_PRESS_MS),
      };
    },
    onPointerMove: (event: PointerEvent<HTMLDivElement>) => {
      const start = press.current;
      if (!start) return;
      if (
        Math.abs(event.clientX - start.x) > LONG_PRESS_SLOP_PX ||
        Math.abs(event.clientY - start.y) > LONG_PRESS_SLOP_PX
      ) {
        cancelPress();
      }
    },
    onPointerUp: cancelPress,
    onPointerCancel: cancelPress,
    onContextMenu: (event: { preventDefault: () => void }) => {
      // A long press on Android fires contextmenu: the sheet replaces the browser's menu.
      if (lastPointer.current === "touch" && showToolbar) {
        event.preventDefault();
        openSheet();
      }
    },
    onFocus: (event: FocusEvent<HTMLDivElement>) => {
      if (event.target === rowRef.current) return;
      if (isKeyboardFocus(event.target)) activate();
    },
    onBlur: (event: FocusEvent<HTMLDivElement>) => {
      if (heldRef.current) return;
      const next = event.relatedTarget as Node | null;
      if (!next || !rowRef.current?.contains(next)) store.release(message.id);
    },
    onKeyDown: (event: KeyboardEvent<HTMLDivElement>) => {
      if (event.key !== "Escape" || heldRef.current || !active) return;
      event.stopPropagation();
      store.release(message.id);
      rowRef.current?.focus({ preventScroll: true });
    },
  };

  const caption = !continued
    ? replyCaption(
        quote ?? null,
        isOwn,
        author.name,
        mentionContext.viewerId,
        copy,
      )
    : null;
  const nameLine =
    !continued && !isOwn && showName ? (
      <p className="mb-0.5 flex max-w-full items-baseline gap-2 ps-3 text-xs font-medium text-muted-foreground">
        <span className="truncate">{author.name}</span>
        <time
          dateTime={message.createdAt}
          title={fullTime}
          className="shrink-0 font-normal tabular-nums"
        >
          {time}
        </time>
      </p>
    ) : null;

  let bubble: ReactNode;
  if (!live) {
    bubble = (
      <p
        className={cn(
          "inline-flex max-w-full items-center gap-1.5 border border-border px-3 py-2 text-sm italic text-muted-foreground",
          shape,
        )}
        data-bubble="tombstone"
      >
        <span className="sr-only">{srWho}: </span>
        <Ban className="size-3.5 shrink-0" aria-hidden="true" />
        {message.removedByModerator
          ? copy.list.removedByModerator
          : copy.list.deleted}
      </p>
    );
  } else if (emojiCount > 0) {
    bubble = (
      <p
        className={cn(
          "px-1 text-4xl leading-tight",
          highlighted && "rounded-2xl ring-2 ring-ring",
        )}
        data-bubble="emoji"
        data-emoji-count={emojiCount}
      >
        <span className="sr-only">{srWho}: </span>
        {content}
      </p>
    );
  } else {
    bubble = (
      <div
        className={cn(
          "min-w-0 max-w-full px-3 py-2 text-sm leading-relaxed",
          shape,
          isOwn
            ? "bg-primary text-primary-foreground"
            : mentionsMe
              ? // A message that mentions the viewer: the warning tint (AA 5.4:1 light, 9.2:1 dark) plus "For you" for readers.
                "bg-warning-subtle text-warning-subtle-foreground"
              : "bg-sidebar-accent text-sidebar-accent-foreground",
          highlighted &&
            "ring-2 ring-ring ring-offset-2 ring-offset-background",
        )}
        data-bubble={isOwn ? "own" : "other"}
      >
        {quote ? (
          <QuoteBlock
            quote={quote}
            copy={copy}
            isOwn={isOwn}
            onOpen={
              onOpenQuote
                ? () => onOpenQuote(message.replyToId ?? "")
                : undefined
            }
          />
        ) : null}
        <p className="whitespace-pre-wrap break-words wrap-anywhere">
          <span className="sr-only">{srWho}: </span>
          {mentionsMe ? (
            <span className="sr-only">{copy.mentions.forYou}: </span>
          ) : null}
          <ChatMentionText
            content={content}
            mentions={message.mentions}
            copy={copy}
            tone={isOwn ? "own" : "default"}
          />
        </p>
      </div>
    );
  }

  const meta: ReactNode[] = [];
  if (pinned) {
    meta.push(
      <span key="pin" className="inline-flex items-center gap-1">
        <Pin className="size-3" aria-hidden="true" />
        {copy.pins.pinned}
      </span>,
    );
  }
  if (live && message.edited) {
    meta.push(<span key="edited">{copy.list.edited}</span>);
  }
  if (live && isOwn && showSent && groupEnd) {
    meta.push(
      <span key="sent">
        {copy.list.sent}
        <span aria-hidden="true"> · </span>
        <time
          dateTime={message.createdAt}
          title={fullTime}
          className="tabular-nums"
        >
          {time}
        </time>
      </span>,
    );
  }

  return (
    <div
      ref={rowRef}
      tabIndex={-1}
      className={cn(
        "relative flex w-full items-end gap-2 px-3 outline-none md:px-4",
        isOwn ? "flex-row-reverse" : "flex-row",
        continued ? "mt-0.5" : "mt-3",
        fresh &&
          "motion-safe:animate-in motion-safe:fade-in-0 motion-safe:slide-in-from-bottom-1 motion-safe:duration-150",
      )}
      data-message-id={message.id}
      data-own={isOwn ? "true" : "false"}
      data-group-start={continued ? undefined : "true"}
      data-group-end={groupEnd ? "true" : undefined}
      data-highlighted={highlighted ? "true" : undefined}
      data-mentions-me={mentionsMe ? "true" : undefined}
      {...handlers}
    >
      {isOwn ? null : (
        <div className={GUTTER} data-avatar-slot="">
          {groupEnd ? (
            <Avatar className="size-7" aria-hidden="true">
              <AvatarFallback
                className={cn(
                  "text-xs font-semibold",
                  avatarToneOf(message.authorId),
                )}
              >
                {initialsOf(author.name)}
              </AvatarFallback>
            </Avatar>
          ) : null}
        </div>
      )}
      <div
        className={cn(
          "flex min-w-0 max-w-[min(75%,36rem)] flex-col",
          isOwn ? "items-end" : "items-start",
        )}
        data-testid="chat-message-column"
      >
        {caption ? (
          <p
            className={cn(
              "mb-0.5 inline-flex max-w-full items-center gap-1 text-xs text-muted-foreground",
              isOwn ? "pe-3" : "ps-3",
            )}
          >
            <Reply className="size-3 shrink-0" aria-hidden="true" />
            <span className="truncate">{caption}</span>
          </p>
        ) : null}
        {nameLine}
        <div
          className={cn(
            "flex max-w-full items-center gap-1",
            isOwn ? "flex-row-reverse" : "flex-row",
          )}
        >
          <div
            ref={bubbleRef}
            className="relative min-w-0 max-w-full"
            title={fullTime}
          >
            {bubble}
            {showToolbar ? (
              <MessageActionBar
                copy={copy}
                label={copy.list.actions.replace("{{author}}", author.name)}
                visible={visible}
                side={isOwn ? "start" : "end"}
                placement={placement}
                time={
                  <time dateTime={message.createdAt} title={fullTime}>
                    {time}
                  </time>
                }
                canReact={canReact}
                reacted={reacted}
                onPick={(emoji) =>
                  onReact?.(message, emoji, !reacted.includes(emoji))
                }
                onReply={replyHandler}
                items={items}
                onHoldChange={setHold}
              />
            ) : null}
          </div>
          {showToolbar ? (
            <button
              type="button"
              aria-label={copy.toolbar.sheetLabel}
              aria-haspopup="dialog"
              onClick={openSheet}
              className="relative hidden size-8 shrink-0 items-center justify-center rounded-full text-muted-foreground outline-none after:absolute after:-inset-1.5 focus-visible:ring-2 focus-visible:ring-focus-ring pointer-coarse:inline-flex"
              data-testid="chat-message-more-touch"
            >
              <MoreHorizontal className="size-4" aria-hidden="true" />
            </button>
          ) : null}
        </div>
        {live && hooks && workspaceId ? (
          <MessageLinkPreviews
            content={message.content}
            workspaceId={workspaceId}
            hooks={hooks}
            copy={copy}
            align={align}
          />
        ) : null}
        {live && hooks && workspaceId ? (
          <MessageAttachments
            message={message}
            workspaceId={workspaceId}
            hooks={hooks}
            isOwn={isOwn}
            readOnly={attachmentsReadOnly}
            copy={copy}
            align={align}
          />
        ) : null}
        {live && hooks && workspaceId && authorName ? (
          <ReactionChips
            messageId={message.id}
            workspaceId={workspaceId}
            reactions={reactions}
            hooks={hooks}
            authorName={authorName}
            copy={copy}
            align={align}
            onToggle={(emoji, add) => onReact?.(message, emoji, add)}
          />
        ) : null}
        {threadCount > 0 && onOpenThread ? (
          <ThreadChip
            thread={thread!}
            copy={copy}
            locale={locale}
            timeZone={timeZone}
            authorName={authorName}
            onOpen={() => onOpenThread(message)}
          />
        ) : null}
        {live && isOwn ? <SentNotices message={message} copy={copy} /> : null}
        {meta.length > 0 ? (
          <p
            className={cn(
              "mt-0.5 flex items-center gap-1.5 text-xs text-muted-foreground",
              isOwn ? "pe-1" : "ps-1",
            )}
            data-testid="chat-message-meta"
          >
            {meta.flatMap((entry, index) =>
              index === 0
                ? [entry]
                : [
                    <span key={`dot-${index}`} aria-hidden="true">
                      ·
                    </span>,
                    entry,
                  ],
            )}
          </p>
        ) : null}
      </div>
      {sheetOpen ? (
        <MessageActionSheet
          open={sheetOpen}
          onOpenChange={setSheetOpen}
          copy={copy}
          who={who}
          snippet={message.content ?? message.attachments?.[0]?.fileName ?? ""}
          canReact={canReact}
          reacted={reacted}
          onPick={(emoji) =>
            onReact?.(message, emoji, !reacted.includes(emoji))
          }
          onReply={replyHandler}
          items={items}
        />
      ) : null}
    </div>
  );
}

/** The More menu of a live message, in a stable order; destructive entries last. */
export function buildMenuItems({
  message,
  copy,
  isOwn,
  pinned,
  content,
  onEdit,
  onDelete,
  onModerate,
  onOpenThread,
  onTogglePin,
}: {
  readonly message: ChatMessage;
  readonly copy: ChatCopy;
  readonly isOwn: boolean;
  readonly pinned: boolean;
  readonly content: string;
  readonly onEdit?: (message: ChatMessage) => void;
  readonly onDelete?: (message: ChatMessage) => void;
  readonly onModerate?: (message: ChatMessage) => void;
  readonly onOpenThread?: (message: ChatMessage) => void;
  readonly onTogglePin?: (message: ChatMessage, pinned: boolean) => void;
}): MessageMenuItem[] {
  const items: MessageMenuItem[] = [];
  if (onOpenThread) {
    items.push({
      key: "thread",
      label: copy.toolbar.replyInThread,
      icon: MessagesSquare,
      onSelect: () => onOpenThread(message),
    });
  }
  if (onTogglePin) {
    items.push({
      key: "pin",
      label: pinned ? copy.toolbar.unpin : copy.toolbar.pin,
      icon: pinned ? PinOff : Pin,
      onSelect: () => onTogglePin(message, !pinned),
    });
  }
  if (isOwn && onEdit) {
    items.push({
      key: "edit",
      label: copy.list.edit,
      icon: Pencil,
      onSelect: () => onEdit(message),
    });
  }
  if (content.trim().length > 0 && canCopy()) {
    items.push({
      key: "copy",
      label: copy.toolbar.copyText,
      icon: Copy,
      onSelect: () =>
        void navigator.clipboard.writeText(content).catch(() => undefined),
    });
  }
  if (isOwn && onDelete) {
    items.push({
      key: "delete",
      label: copy.list.delete,
      icon: Trash2,
      destructive: true,
      onSelect: () => onDelete(message),
    });
  }
  if (!isOwn && onModerate) {
    items.push({
      key: "moderate",
      label: copy.toolbar.moderate,
      icon: ShieldX,
      destructive: true,
      onSelect: () => onModerate(message),
    });
  }
  return items;
}

function canCopy(): boolean {
  return (
    typeof navigator !== "undefined" &&
    typeof navigator.clipboard?.writeText === "function"
  );
}

/** "You replied to Minh", "Minh replied to you", "Minh replied to An": the caption above a reply. */
export function replyCaption(
  quote: QuoteView | null,
  isOwn: boolean,
  authorName: string,
  viewerId: string | null,
  copy: ChatCopy,
): string | null {
  if (!quote) return null;
  const toViewer =
    quote.authorId !== undefined &&
    viewerId !== null &&
    quote.authorId === viewerId;
  if (isOwn) {
    return toViewer
      ? copy.list.youRepliedSelf
      : fillCopy(copy.list.youReplied, { name: quote.name });
  }
  if (toViewer) return fillCopy(copy.list.repliedToYou, { name: authorName });
  return fillCopy(copy.list.replied, { name: authorName, target: quote.name });
}

function QuoteBlock({
  quote,
  copy,
  isOwn,
  onOpen,
}: {
  readonly quote: QuoteView;
  readonly copy: ChatCopy;
  readonly isOwn: boolean;
  readonly onOpen?: () => void;
}) {
  const text = quote.unavailable
    ? copy.quote.unavailable
    : quote.deleted
      ? copy.quote.deleted
      : (quote.text ?? "");
  const body = (
    <>
      <span className="truncate font-semibold text-foreground">
        {fillCopy(copy.quote.quoteOf, { name: quote.name })}
      </span>
      <span className="truncate text-muted-foreground">{text}</span>
    </>
  );
  // An opaque block of the theme's own surfaces, so its text keeps the proven AA pairs on either bubble.
  const shell = cn(
    "-mx-1 mb-1.5 flex min-w-0 max-w-full flex-col rounded-xl border-s-2 border-primary px-2.5 py-1.5 text-start text-xs whitespace-normal",
    isOwn ? "bg-muted" : "bg-background",
  );
  return onOpen ? (
    <button
      type="button"
      onClick={onOpen}
      className={cn(
        shell,
        "w-full hover:opacity-90 focus-visible:ring-2 focus-visible:ring-focus-ring focus-visible:outline-none",
      )}
    >
      {body}
    </button>
  ) : (
    <div className={shell}>{body}</div>
  );
}

/** "6 replies · Latest reply 9:05", with the repliers' avatars when the server sends them. */
function ThreadChip({
  thread,
  copy,
  locale,
  timeZone,
  authorName,
  onOpen,
}: {
  readonly thread: ThreadChipView;
  readonly copy: ChatCopy;
  readonly locale: string;
  readonly timeZone?: string;
  readonly authorName?: (userId: string) => string;
  readonly onOpen: () => void;
}) {
  const count = thread.replyCount;
  const replies = pluralCopy(
    { one: copy.thread.replies_one, other: copy.thread.replies_other },
    count,
    { count },
  );
  const faces = (thread.participantIds ?? []).slice(0, 3);
  return (
    <button
      type="button"
      onClick={onOpen}
      className="mt-1 inline-flex max-w-full items-center gap-1.5 rounded-full border border-border bg-card px-2.5 py-1 text-xs font-medium text-link shadow-xs hover:bg-muted focus-visible:ring-2 focus-visible:ring-focus-ring focus-visible:outline-none"
      aria-label={`${replies}, ${copy.thread.openThread}`}
      data-testid="chat-thread-chip"
    >
      {faces.length > 0 ? (
        <span className="flex -space-x-1.5" aria-hidden="true">
          {faces.map((id) => (
            <Avatar key={id} className="size-4 ring-2 ring-card">
              <AvatarFallback className={cn("text-xs", avatarToneOf(id))}>
                {initialsOf(authorName?.(id) ?? "")}
              </AvatarFallback>
            </Avatar>
          ))}
        </span>
      ) : (
        <MessageSquareText className="size-3.5 shrink-0" aria-hidden="true" />
      )}
      <span className="whitespace-nowrap">{replies}</span>
      {thread.lastReplyAt ? (
        <time
          dateTime={thread.lastReplyAt}
          className="truncate font-normal text-muted-foreground"
        >
          ·{" "}
          {fillCopy(copy.thread.latest, {
            time: formatClock(thread.lastReplyAt, locale, timeZone),
          })}
        </time>
      ) : null}
    </button>
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
  /** The send continues the caller's group above it. */
  readonly continued?: boolean;
  readonly groupEnd?: boolean;
}

/** A send the user made that the server has not stored: an own bubble with "Sending", or "Not sent" with Resend and Discard. */
export function ChatPendingRow({
  pending,
  locale,
  timeZone,
  copy,
  onRetry,
  onDiscard,
  continued = false,
  groupEnd = true,
}: ChatPendingRowProps) {
  const failed = pending.status === "failed";
  const time = formatClock(pending.createdAt, locale, timeZone);
  return (
    <div
      className={cn(
        "flex w-full flex-row-reverse px-3 md:px-4",
        continued ? "mt-0.5" : "mt-3",
        "motion-safe:animate-in motion-safe:fade-in-0 motion-safe:slide-in-from-bottom-1 motion-safe:duration-150",
      )}
      data-testid="chat-pending-message"
      data-status={pending.status}
      data-own="true"
    >
      <div className="flex min-w-0 max-w-[min(75%,36rem)] flex-col items-end">
        <p
          className={cn(
            "min-w-0 max-w-full whitespace-pre-wrap break-words wrap-anywhere bg-primary px-3 py-2 text-sm leading-relaxed text-primary-foreground",
            bubbleShape(true, continued, groupEnd),
            failed &&
              "ring-2 ring-destructive ring-offset-2 ring-offset-background",
          )}
          data-bubble="own"
        >
          <span className="sr-only">
            {fillCopy(copy.list.messageBy, { name: copy.list.you, time })}:{" "}
          </span>
          {pending.content}
        </p>
        <p
          className={cn(
            "mt-0.5 pe-1 text-xs font-medium",
            failed ? "text-destructive" : "text-muted-foreground",
          )}
          role={failed ? "status" : undefined}
        >
          <span>{failed ? copy.list.notSent : copy.list.sending}</span>
        </p>
        {failed ? (
          <div className="mt-1 flex flex-wrap items-center justify-end gap-1.5 text-xs">
            {pending.failure === "refused" ? (
              <span className="text-destructive">{copy.list.refusedSend}</span>
            ) : (
              <Button
                type="button"
                variant="outline"
                size="sm"
                className="rounded-full"
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
              className="rounded-full"
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
      className="mt-1 flex max-w-full flex-col items-end gap-0.5 pe-1 text-end text-xs text-muted-foreground"
      data-testid="chat-sent-notices"
    >
      {lines.map((line, index) => (
        <li key={index}>{line}</li>
      ))}
    </ul>
  );
}
