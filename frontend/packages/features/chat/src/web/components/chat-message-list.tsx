import { useLayoutEffect, useRef, useState, type KeyboardEvent } from "react";
import {
  ArrowDown,
  ChevronLeft,
  Lock,
  MessageSquareText,
  RotateCw,
} from "lucide-react";
import {
  Button,
  Empty,
  EmptyDescription,
  EmptyHeader,
  EmptyMedia,
  EmptyTitle,
  Skeleton,
  Textarea,
  cn,
} from "@notrelix/ui-web";
import type { ChatMessage } from "../../core/types/chat";
import type { ChatHooks } from "../query/create-chat-hooks";
import type { ThreadCounts } from "../query/interaction-cache";
import {
  dayKeyOf,
  previousDayKey,
  type ChatRow,
  type PendingMessage,
} from "../../core/model/chat-model";
import type { ChatCopy } from "./chat.copy";
import { fillCopy } from "./use-chat-copy";
import {
  ChatMessageRow,
  ChatPendingRow,
  formatClock,
  type QuoteView,
} from "./chat-message-row";
import {
  RowActivityProvider,
  createRowActivityStore,
} from "./chat-row-activity";
import { ChatTypingBubble } from "./chat-typing-indicator";

/** Within this many pixels of the bottom the list counts as at the bottom. */
const BOTTOM_SLACK_PX = 48;
/** Within this many pixels of the top the next older page is requested. */
const TOP_TRIGGER_PX = 160;

export type ChatListStatus = "loading" | "ready" | "error" | "no-access";

export interface ChatEditingState {
  readonly messageId: string;
  readonly draft: string;
  readonly busy: boolean;
}

export interface ChatScrollRequest {
  /** A new request scrolls even when the target did not change. */
  readonly nonce: number;
  readonly target: "divider" | "bottom";
}

export interface ChatMessageListProps {
  readonly channelName: string;
  /** Replaces the "start of the channel" empty state (a conversation has its own wording). */
  readonly emptyState?: { readonly title: string; readonly body: string };
  readonly rows: readonly ChatRow[];
  readonly status: ChatListStatus;
  readonly hasOlder: boolean;
  readonly isLoadingOlder: boolean;
  readonly hasNewer: boolean;
  readonly isLoadingNewer: boolean;
  readonly scrollRequest: ChatScrollRequest | null;
  readonly editing: ChatEditingState | null;
  readonly staleNotice: string | null;
  readonly viewerId: string;
  readonly authorName: (userId: string) => string;
  readonly locale: string;
  readonly timeZone?: string;
  readonly copy: ChatCopy;
  readonly onRetryLoad: () => void;
  readonly onOlderNeeded: () => void;
  readonly onNewerNeeded: () => void;
  readonly onAtBottomChange: (atBottom: boolean) => void;
  readonly onJumpLatest: () => void;
  readonly onEdit?: (message: ChatMessage) => void;
  readonly onEditDraftChange: (draft: string) => void;
  readonly onEditSave: () => void;
  readonly onEditCancel: () => void;
  readonly onDelete?: (message: ChatMessage) => void;
  /** Delete as a moderator: set only when the caller may moderate this channel. */
  readonly onModerate?: (message: ChatMessage) => void;
  readonly onRetryPending: (pending: PendingMessage) => void;
  readonly onDiscardPending: (clientMessageId: string) => void;
  readonly pendingEntries: readonly PendingMessage[];
  /** Shown on the no-access and error states on narrow screens, to return to the channel list. */
  readonly onBackToList?: () => void;
  /** Phase 2: the workspace and hooks the reaction popover reads through. */
  readonly workspaceId?: string;
  readonly hooks?: ChatHooks;
  /** The pinned message ids of the channel (the toolbar shows Unpin for them). */
  readonly pinnedIds?: ReadonlySet<string>;
  /** The reply count and latest reply of each thread root, by message id. */
  readonly threadCounts?: ThreadCounts;
  /** The quote of a reply (the message it answers), or null when the message quotes nothing. */
  readonly quoteOf?: (message: ChatMessage) => QuoteView | null;
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
  /** Names above groups of other people's messages: on in channels and groups, off in a 1-1 conversation. */
  readonly showNames?: boolean;
  /** The people typing now (not the caller): a typing bubble closes the list. */
  readonly typingIds?: readonly string[];
}

/** The width of the conversation column: a comfortable measure on wide screens, fluid below it. */
export const CHAT_COLUMN = "mx-auto w-full max-w-3xl";

export function dayLabel(
  dayKey: string,
  locale: string,
  copy: ChatCopy,
  timeZone?: string,
  now: Date = new Date(),
): string {
  const today = dayKeyOf(now.toISOString(), timeZone);
  if (dayKey === today) return copy.time.today;
  if (dayKey === previousDayKey(today)) return copy.time.yesterday;
  const [year, month, day] = dayKey.split("-").map(Number);
  const date = new Date(Date.UTC(year!, month! - 1, day!, 12));
  return new Intl.DateTimeFormat(locale, {
    weekday: "long",
    day: "numeric",
    month: "long",
    year: "numeric",
    timeZone: "UTC",
  }).format(date);
}

/** The message list (UC-CHT-B02, B03, D03): a log of the window, newest at the bottom. */
export function ChatMessageList(props: ChatMessageListProps) {
  const {
    channelName,
    rows,
    status,
    copy,
    locale,
    timeZone,
    scrollRequest,
    editing,
  } = props;
  const scrollRef = useRef<HTMLDivElement | null>(null);
  const [atBottom, setAtBottom] = useState(true);
  const [activity] = useState(createRowActivityStore);
  const typingIds = props.typingIds ?? [];
  // Messages newer than the newest one at the first render fade in; the history already there does not.
  const baseline = useRef<{ space: string; sequence: number } | null>(null);
  const firstMessage = rows.find((row) => row.kind === "message");
  const space =
    firstMessage?.kind === "message" ? firstMessage.message.spaceId : "";
  let newestSequence = 0;
  for (const row of rows) {
    if (row.kind === "message" && row.message.sequence > newestSequence)
      newestSequence = row.message.sequence;
  }
  useLayoutEffect(() => {
    if (status !== "ready" || space === "") return;
    if (baseline.current === null || baseline.current.space !== space) {
      baseline.current = { space, sequence: newestSequence };
    }
  }, [status, space, newestSequence]);
  const memo = useRef<{
    firstKey: string | null;
    height: number;
    atBottom: boolean;
    request: number | null;
  }>({ firstKey: null, height: 0, atBottom: true, request: null });

  // Keeps the reading position. Older messages added above the view move the
  // reader's place down by the height they added; a new message follows the
  // reader only when they were already at the bottom.
  useLayoutEffect(() => {
    const el = scrollRef.current;
    if (!el) return;
    const firstKey = rows.find((row) => row.kind === "message")?.key ?? null;
    const prev = memo.current;
    const prepended =
      prev.firstKey !== null &&
      firstKey !== null &&
      firstKey !== prev.firstKey &&
      rows.findIndex((row) => row.key === prev.firstKey) > 0;
    if (prepended) {
      el.scrollTop += el.scrollHeight - prev.height;
    } else if (el.scrollHeight !== prev.height && prev.atBottom) {
      el.scrollTop = el.scrollHeight;
    }
    memo.current = {
      firstKey,
      height: el.scrollHeight,
      atBottom: isAtBottom(el),
      request: memo.current.request,
    };
  }, [rows, typingIds.length]);

  // An explicit request: open at the first unread message, or go to the newest.
  useLayoutEffect(() => {
    const el = scrollRef.current;
    if (!el || !scrollRequest) return;
    if (memo.current.request === scrollRequest.nonce) return;
    memo.current.request = scrollRequest.nonce;
    if (scrollRequest.target === "divider") {
      const divider = el.querySelector<HTMLElement>("[data-unread-divider]");
      if (divider) {
        divider.scrollIntoView({ block: "center" });
        return;
      }
    }
    el.scrollTop = el.scrollHeight;
    memo.current.atBottom = true;
  }, [scrollRequest, rows]);

  function handleScroll() {
    const el = scrollRef.current;
    if (!el) return;
    const bottom = isAtBottom(el);
    memo.current.atBottom = bottom;
    setAtBottom(bottom);
    props.onAtBottomChange(bottom);
    if (
      el.scrollTop < TOP_TRIGGER_PX &&
      props.hasOlder &&
      !props.isLoadingOlder
    ) {
      props.onOlderNeeded();
    }
    if (bottom && props.hasNewer && !props.isLoadingNewer) {
      props.onNewerNeeded();
    }
  }

  const label = fillCopy(copy.list.label, { channel: channelName });
  const announcement = useIncomingAnnouncement(rows, props.viewerId, (row) =>
    fillCopy(row.text === null ? copy.list.newFromOmitted : copy.list.newFrom, {
      name: props.authorName(row.authorId),
      text: row.text ?? "",
    }),
  );

  if (status === "loading") {
    return (
      <div
        className="flex flex-1 flex-col gap-5 overflow-hidden px-4 py-5"
        role="status"
      >
        <span className="sr-only">{copy.list.loading}</span>
        <div className={cn(CHAT_COLUMN, "flex flex-col gap-3")}>
          {[0, 1, 2, 3, 4].map((index) => {
            const own = index % 3 === 2;
            return (
              <div
                key={index}
                className={cn(
                  "flex items-end gap-2",
                  own ? "flex-row-reverse" : "flex-row",
                )}
                aria-hidden="true"
              >
                {own ? null : (
                  <Skeleton className="size-7 shrink-0 rounded-full" />
                )}
                <Skeleton
                  className={cn(
                    "h-9 rounded-2xl",
                    index % 2 === 0 ? "w-2/5 max-w-xs" : "w-3/5 max-w-md",
                  )}
                />
              </div>
            );
          })}
        </div>
      </div>
    );
  }

  if (status === "error") {
    return (
      <Empty className="flex-1 px-6 py-10">
        <EmptyHeader>
          <EmptyMedia variant="icon" className="text-destructive">
            <MessageSquareText aria-hidden="true" />
          </EmptyMedia>
          <EmptyTitle>{copy.list.error}</EmptyTitle>
          <EmptyDescription>{copy.list.errorBody}</EmptyDescription>
        </EmptyHeader>
        <div className="flex flex-wrap items-center justify-center gap-2">
          <Button onClick={props.onRetryLoad}>
            <RotateCw aria-hidden="true" />
            {copy.list.retry}
          </Button>
          {props.onBackToList ? (
            <BackToList
              label={copy.list.backToList}
              onClick={props.onBackToList}
            />
          ) : null}
        </div>
      </Empty>
    );
  }

  if (status === "no-access") {
    return (
      <Empty className="flex-1 px-6 py-10">
        <EmptyHeader>
          <EmptyMedia variant="icon">
            <Lock aria-hidden="true" />
          </EmptyMedia>
          <EmptyTitle>{copy.list.noAccess}</EmptyTitle>
          <EmptyDescription>{copy.list.noAccessBody}</EmptyDescription>
        </EmptyHeader>
        {props.onBackToList ? (
          <BackToList
            label={copy.list.backToList}
            onClick={props.onBackToList}
          />
        ) : null}
      </Empty>
    );
  }

  const hasMessages = rows.some((row) => row.kind === "message");
  const lastOwnKey = latestOwnKey(rows, props.viewerId);
  if (!hasMessages && props.pendingEntries.length === 0) {
    return (
      <Empty className="flex-1 px-6 py-10">
        <EmptyHeader>
          <EmptyMedia variant="icon">
            <MessageSquareText aria-hidden="true" />
          </EmptyMedia>
          <EmptyTitle>
            {props.emptyState?.title ??
              fillCopy(copy.list.startOf, { channel: channelName })}
          </EmptyTitle>
          <EmptyDescription>
            {props.emptyState?.body ?? copy.list.emptyBody}
          </EmptyDescription>
        </EmptyHeader>
      </Empty>
    );
  }

  return (
    <div className="relative flex min-h-0 flex-1 flex-col">
      {props.staleNotice ? (
        <p
          role="status"
          className="mx-4 mt-2 rounded-md border border-border bg-muted px-3 py-2 text-sm text-muted-foreground"
        >
          {props.staleNotice}
        </p>
      ) : null}
      <div
        ref={scrollRef}
        role="log"
        aria-live="off"
        aria-label={label}
        aria-busy={props.isLoadingOlder || props.isLoadingNewer}
        tabIndex={0}
        onScroll={handleScroll}
        className="min-h-0 flex-1 overflow-y-auto overscroll-contain py-3 outline-none focus-visible:ring-2 focus-visible:ring-focus-ring focus-visible:ring-inset"
        data-testid="chat-message-list"
      >
        {props.isLoadingOlder ? (
          <p className="px-4 py-2 text-center text-xs text-muted-foreground">
            {copy.list.loadingOlder}
          </p>
        ) : null}
        <RowActivityProvider value={activity}>
          <ol className={cn(CHAT_COLUMN, "flex flex-col pb-2")}>
            {rows.map((row) => {
              if (row.kind === "day") {
                return (
                  <li
                    key={row.key}
                    role="presentation"
                    className="mt-6 mb-2 flex justify-center px-4 first:mt-2"
                  >
                    <span className="text-xs font-medium whitespace-nowrap text-muted-foreground">
                      {dayLabel(row.dayKey, locale, copy, timeZone)}
                    </span>
                  </li>
                );
              }
              if (row.kind === "time") {
                return (
                  <li
                    key={row.key}
                    role="presentation"
                    className="mt-5 mb-1 flex justify-center px-4"
                    data-testid="chat-time-separator"
                  >
                    <time
                      dateTime={row.at}
                      className="text-xs whitespace-nowrap tabular-nums text-muted-foreground"
                    >
                      {formatClock(row.at, locale, timeZone)}
                    </time>
                  </li>
                );
              }
              if (row.kind === "unread") {
                return (
                  <li
                    key={row.key}
                    role="separator"
                    aria-label={copy.list.newDivider}
                    data-unread-divider=""
                    className="mt-4 mb-1 flex items-center gap-3 px-4 text-xs font-semibold text-destructive"
                  >
                    <span
                      className="h-px flex-1 bg-destructive/40"
                      aria-hidden="true"
                    />
                    <span>{copy.list.newDivider}</span>
                    <span
                      className="h-px flex-1 bg-destructive/40"
                      aria-hidden="true"
                    />
                  </li>
                );
              }
              if (row.kind === "pending") {
                return (
                  <li key={row.key}>
                    <ChatPendingRow
                      pending={row.pending}
                      author={{ name: props.authorName(row.pending.authorId) }}
                      locale={locale}
                      timeZone={timeZone}
                      copy={copy}
                      continued={row.continued}
                      groupEnd={row.groupEnd}
                      onRetry={props.onRetryPending}
                      onDiscard={props.onDiscardPending}
                    />
                  </li>
                );
              }
              const isEditing = editing?.messageId === row.message.id;
              const isOwn = row.message.authorId === props.viewerId;
              const fresh =
                baseline.current !== null &&
                baseline.current.space === row.message.spaceId &&
                row.message.sequence > baseline.current.sequence &&
                !isOwn;
              return (
                <li
                  key={row.key}
                  id={`message-${row.message.id}`}
                  className={cn(row.message.kind === "system" && "my-2")}
                >
                  {isEditing ? (
                    <EditForm
                      copy={copy}
                      editing={editing}
                      continued={row.continued}
                      onChange={props.onEditDraftChange}
                      onSave={props.onEditSave}
                      onCancel={props.onEditCancel}
                    />
                  ) : (
                    <ChatMessageRow
                      message={row.message}
                      author={{ name: props.authorName(row.message.authorId) }}
                      continued={row.continued}
                      groupEnd={row.groupEnd}
                      showName={props.showNames ?? true}
                      showSent={row.key === lastOwnKey}
                      fresh={fresh}
                      isOwn={isOwn}
                      locale={locale}
                      timeZone={timeZone}
                      copy={copy}
                      onEdit={props.onEdit}
                      onDelete={props.onDelete}
                      onModerate={props.onModerate}
                      quote={props.quoteOf?.(row.message) ?? null}
                      thread={threadView(props.threadCounts, row.message)}
                      pinned={props.pinnedIds?.has(row.message.id) ?? false}
                      reactions={row.message.reactions ?? []}
                      workspaceId={props.workspaceId}
                      hooks={props.hooks}
                      authorName={props.authorName}
                      onReact={props.onReact}
                      onReply={props.onReply}
                      onOpenThread={props.onOpenThread}
                      onTogglePin={props.onTogglePin}
                      onOpenQuote={props.onOpenQuote}
                      attachmentsReadOnly={props.attachmentsReadOnly}
                    />
                  )}
                </li>
              );
            })}
            {typingIds.length > 0 ? (
              <li aria-hidden="true">
                <ChatTypingBubble
                  ids={typingIds}
                  authorName={props.authorName}
                  copy={copy}
                />
              </li>
            ) : null}
          </ol>
        </RowActivityProvider>
        {props.isLoadingNewer ? (
          <p className="px-4 py-2 text-center text-xs text-muted-foreground">
            {copy.list.loadingOlder}
          </p>
        ) : null}
      </div>
      <p role="status" aria-live="polite" className="sr-only">
        {announcement}
      </p>
      <JumpLatest
        visible={!atBottom || props.hasNewer}
        label={copy.list.jumpLatest}
        onClick={() => {
          props.onJumpLatest();
          const el = scrollRef.current;
          if (el) el.scrollTop = el.scrollHeight;
        }}
      />
    </div>
  );
}

/** The caller's newest stored message, when nothing of theirs is still sending after it ("Sent" shows under it). */
function latestOwnKey(
  rows: readonly ChatRow[],
  viewerId: string,
): string | null {
  for (let index = rows.length - 1; index >= 0; index--) {
    const row = rows[index]!;
    if (row.kind === "pending") return null;
    if (row.kind === "message" && row.message.authorId === viewerId) {
      return row.message.deleted || row.message.kind === "system"
        ? null
        : row.key;
    }
  }
  return null;
}

/**
 * The thread chip of a root: the live projection (replies learned from realtime
 * frames) or the count the server sent with the message, whichever is higher.
 * History alone is enough to show "N replies" after a reload.
 */
function threadView(counts: ThreadCounts | undefined, message: ChatMessage) {
  const entry = counts?.[message.id];
  const stored = message.threadReplyCount ?? 0;
  const live = entry?.replyCount ?? 0;
  if (live === 0 && stored === 0) return null;
  if (live >= stored) {
    return { replyCount: live, lastReplyAt: entry?.lastReplyAt ?? null };
  }
  return { replyCount: stored, lastReplyAt: message.threadLastReplyAt ?? null };
}

/**
 * The line a screen reader hears for a new message from someone else: the
 * author and the text, once per arrival. The log itself is not live (aria-live
 * is off on it), so a busy channel does not read the whole list. The first
 * render and older pages loaded above stay silent.
 */
function useIncomingAnnouncement(
  rows: readonly ChatRow[],
  viewerId: string,
  format: (incoming: { authorId: string; text: string | null }) => string,
): string {
  let newest: ChatMessage | null = null;
  for (let index = rows.length - 1; index >= 0; index--) {
    const row = rows[index]!;
    if (row.kind === "message") {
      newest = row.message;
      break;
    }
  }
  const seen = useRef<{ id: string | null } | null>(null);
  const [line, setLine] = useState("");
  const newestId = newest?.id ?? null;
  useLayoutEffect(() => {
    const previous = seen.current;
    seen.current = { id: newestId };
    if (previous === null || previous.id === newestId || !newest) return;
    if (newest.kind !== "user" || newest.authorId === viewerId) return;
    setLine(
      format({
        authorId: newest.authorId,
        text: newest.deleted ? null : newest.content,
      }),
    );
  }, [newestId]);
  return line;
}

/** The narrow-screen way back from a channel that cannot be shown. */
function BackToList({
  label,
  onClick,
}: {
  readonly label: string;
  readonly onClick: () => void;
}) {
  return (
    <Button type="button" variant="outline" onClick={onClick}>
      <ChevronLeft aria-hidden="true" />
      {label}
    </Button>
  );
}

function isAtBottom(el: HTMLElement): boolean {
  return el.scrollHeight - el.scrollTop - el.clientHeight < BOTTOM_SLACK_PX;
}

/**
 * The jump-to-latest control sits in the flow between the message log and the
 * composer, so it never covers a message: a small floating pill, centred on the
 * conversation column, that takes its own line while it shows.
 */
function JumpLatest({
  visible,
  label,
  onClick,
}: {
  readonly visible: boolean;
  readonly label: string;
  readonly onClick: () => void;
}) {
  if (!visible) return null;
  return (
    <div className="flex shrink-0 justify-center px-4 pt-1 pb-1.5">
      <Button
        type="button"
        variant="secondary"
        size="sm"
        className="max-w-full rounded-full border border-border bg-popover text-popover-foreground shadow-md hover:bg-muted motion-safe:animate-in motion-safe:fade-in-0 motion-safe:duration-150"
        onClick={onClick}
      >
        <ArrowDown aria-hidden="true" />
        <span className="truncate">{label}</span>
      </Button>
    </div>
  );
}

function EditForm({
  copy,
  editing,
  continued,
  onChange,
  onSave,
  onCancel,
}: {
  readonly copy: ChatCopy;
  readonly editing: ChatEditingState;
  readonly continued: boolean;
  readonly onChange: (draft: string) => void;
  readonly onSave: () => void;
  readonly onCancel: () => void;
}) {
  function handleKeyDown(event: KeyboardEvent<HTMLTextAreaElement>) {
    if (event.key === "Escape") {
      event.preventDefault();
      onCancel();
    } else if (
      event.key === "Enter" &&
      !event.shiftKey &&
      !event.nativeEvent.isComposing
    ) {
      event.preventDefault();
      onSave();
    }
  }
  return (
    <div
      className={cn(
        "flex w-full justify-end px-3 md:px-4",
        continued ? "mt-0.5" : "mt-3",
      )}
    >
      <form
        className="flex w-full max-w-[min(75%,36rem)] min-w-64 flex-col gap-1.5 rounded-2xl border border-ring bg-card p-1.5 shadow-sm"
        onSubmit={(event) => {
          event.preventDefault();
          onSave();
        }}
      >
        <Textarea
          aria-label={copy.list.editLabel}
          value={editing.draft}
          onChange={(event) => onChange(event.target.value)}
          onKeyDown={handleKeyDown}
          rows={1}
          autoFocus
          disabled={editing.busy}
          className="max-h-40 min-h-9 resize-none border-0 bg-transparent px-2 py-1.5 shadow-none focus-visible:ring-0 dark:bg-transparent"
        />
        <div className="flex items-center justify-end gap-1.5">
          <Button
            type="button"
            size="sm"
            variant="ghost"
            className="rounded-full"
            onClick={onCancel}
          >
            {copy.list.cancel}
          </Button>
          <Button
            type="submit"
            size="sm"
            className="rounded-full"
            disabled={editing.busy}
          >
            {copy.list.save}
          </Button>
        </div>
      </form>
    </div>
  );
}
