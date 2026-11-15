import {
  useCallback,
  useLayoutEffect,
  useMemo,
  useRef,
  useState,
  type CSSProperties,
  type KeyboardEvent,
  type Ref,
} from "react";
import { defaultRangeExtractor, useVirtualizer } from "@tanstack/react-virtual";
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
import { fillCopy, pluralCopy } from "./use-chat-copy";
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
import { useChatHighlight } from "./chat-highlight-context";

/** Within this many pixels of the bottom the list counts as at the bottom. */
const BOTTOM_SLACK_PX = 48;
/** Within this many pixels of the top the next older page is requested. */
const TOP_TRIGGER_PX = 160;
const NO_REACTIONS: readonly never[] = [];
/** The overscan of the virtual list: rows rendered above and below the visible ones. */
const OVERSCAN_ROWS = 20;
/** The list's own bottom padding (the former `pb-2`). */
const LIST_PADDING_END_PX = 8;

/** What the virtual list adds to a row's `li`: its slot, its measurement and its place in the set. */
interface LiExtra {
  readonly ref?: Ref<HTMLLIElement>;
  readonly style?: CSSProperties;
  readonly "data-index"?: number;
  readonly "aria-posinset"?: number;
  readonly "aria-setsize"?: number;
}

/** A row's slot is its box plus the margins of the `li` itself (the flex column adds them between rows). */
function measureSlot(element: Element): number {
  const style = getComputedStyle(element);
  return (
    element.getBoundingClientRect().height +
    (parseFloat(style.marginTop) || 0) +
    (parseFloat(style.marginBottom) || 0)
  );
}

let observerNoticeQuieted = false;
/**
 * Measuring rows after a resize makes the browser report "ResizeObserver loop completed with undelivered
 * notifications". It is harmless, and dev overlays treat it as an error, so that one message is dropped (the same
 * guard the Work Management kanban uses).
 */
function quietObserverLoopNotice() {
  if (observerNoticeQuieted || typeof window === "undefined") return;
  observerNoticeQuieted = true;
  window.addEventListener(
    "error",
    (event) => {
      if (event.message?.startsWith("ResizeObserver loop"))
        event.stopImmediatePropagation();
    },
    true,
  );
}

/** The measured slots, current for the rows of this render. */
function measurementsOf(instance: {
  getTotalSize: () => number;
  measurementsCache: readonly { readonly start: number }[];
}) {
  instance.getTotalSize();
  return instance.measurementsCache;
}
function startOf(
  instance: Parameters<typeof measurementsOf>[0],
  index: number,
): number | undefined {
  return measurementsOf(instance)[index]?.start;
}

function estimateRowSize(row: ChatRow | undefined): number {
  if (!row) return 56;
  if (row.kind === "day" || row.kind === "unread") return 36;
  if (row.kind === "time") return 30;
  return row.kind === "message" && row.continued ? 34 : 60;
}

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
export const CHAT_COLUMN = "mx-auto w-full max-w-[760px] 2xl:max-w-[1040px]";

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
    ...(year === Number(today.slice(0, 4)) ? {} : { year: "numeric" }),
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
  // The newest sequence the reader had in view the last time they were at the bottom: what lies beyond it is new to them.
  const seenSequence = useRef(0);
  const [activity] = useState(createRowActivityStore);
  const typingIds = props.typingIds ?? [];
  // The row handlers keep one identity for the life of the list, so memoised rows do not re-render when the
  // workspace around the list does; each call reaches the newest props.
  const latest = useRef(props);
  useLayoutEffect(() => {
    latest.current = props;
  });
  const rowHandlers = useMemo(
    () => ({
      onEdit: (m: ChatMessage) => latest.current.onEdit?.(m),
      onDelete: (m: ChatMessage) => latest.current.onDelete?.(m),
      onModerate: (m: ChatMessage) => latest.current.onModerate?.(m),
      onReact: (m: ChatMessage, emoji: string, add: boolean) =>
        latest.current.onReact?.(m, emoji, add),
      onReply: (m: ChatMessage) => latest.current.onReply?.(m),
      onOpenThread: (m: ChatMessage) => latest.current.onOpenThread?.(m),
      onTogglePin: (m: ChatMessage, pinned: boolean) =>
        latest.current.onTogglePin?.(m, pinned),
      onOpenQuote: (id: string) => {
        jumpRef.current(id);
        latest.current.onOpenQuote?.(id);
      },
      authorName: (id: string) => latest.current.authorName(id),
    }),
    [],
  );
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
  useLayoutEffect(() => {
    if (atBottom) seenSequence.current = newestSequence;
  }, [atBottom, newestSequence, space]);
  let unseenCount = 0;
  if (!atBottom) {
    for (const row of rows) {
      if (
        row.kind === "message" &&
        row.message.sequence > seenSequence.current &&
        row.message.authorId !== props.viewerId
      )
        unseenCount += 1;
    }
  }
  quietObserverLoopNotice();
  const hasTyping = typingIds.length > 0;
  const count = rows.length + (hasTyping ? 1 : 0);
  const listRef = useRef<HTMLOListElement | null>(null);
  const memo = useRef<{
    firstKey: string | null;
    firstIndex: number;
    firstStart: number;
    lastKey: string | null;
    height: number;
    total: number;
    atBottom: boolean;
    /** Set by a request for the newest messages: rows that grow or arrive keep the list at the end until the reader scrolls. */
    follow: boolean;
    request: number | null;
  }>({
    firstKey: null,
    firstIndex: -1,
    firstStart: 0,
    lastKey: null,
    height: 0,
    total: 0,
    atBottom: true,
    follow: false,
    request: null,
  });
  const getItemKey = useCallback(
    (index: number) => rows[index]?.key ?? "typing",
    [rows],
  );
  const estimateSize = useCallback(
    (index: number) => estimateRowSize(rows[index]),
    [rows],
  );
  // Only the rows near the viewport are in the DOM: a channel with thousands of messages costs the same as one with fifty.
  const virtualizer = useVirtualizer<HTMLDivElement, HTMLLIElement>({
    count,
    getScrollElement: () => scrollRef.current,
    estimateSize,
    getItemKey,
    overscan: OVERSCAN_ROWS,
    // The page also keeps the date label that opens the window and the newest row: a message that arrives while
    // the reader is higher up is in the document at once (the announcement and the jump to the latest rely on it).
    rangeExtractor: (range) => {
      const shown = defaultRangeExtractor(range);
      const extra: number[] = [];
      if (rows[0]?.kind === "day" && shown[0] !== 0) extra.push(0);
      const kept = [...extra, ...shown];
      if (range.count > 0 && kept[kept.length - 1] !== range.count - 1) {
        kept.push(range.count - 1);
      }
      return kept;
    },
    paddingEnd: LIST_PADDING_END_PX,
    scrollMargin: listRef.current?.offsetTop ?? 12,
    measureElement: measureSlot,
    initialRect: { width: 760, height: 800 },
    onChange: (instance) => {
      const el = scrollRef.current;
      const state = memo.current;
      if (state.firstIndex >= 0) {
        state.firstStart =
          startOf(instance, state.firstIndex) ?? state.firstStart;
      }
      // A row that grew (an image loaded) keeps a reader at the bottom at the bottom.
      const total = instance.getTotalSize();
      if (el && total !== state.total) {
        state.total = total;
        // A frame later: scrolling inside the resize callback makes the browser report a resize loop.
        if (state.atBottom || state.follow) {
          requestAnimationFrame(() => {
            if (memo.current.atBottom || memo.current.follow)
              el.scrollTop = el.scrollHeight;
          });
        }
      }
    },
  });
  // Without a layout (a hidden panel, a test DOM) every row renders in normal flow.
  const virtualOn = (virtualizer.scrollRect?.height ?? 0) > 0;

  // Keeps the reading position. Older messages added above the view move the
  // reader's place down by the height they added; a new message follows the
  // reader only when they were already at the bottom.
  useLayoutEffect(() => {
    const el = scrollRef.current;
    if (!el) return;
    const firstIndex = rows.findIndex((row) => row.kind === "message");
    const firstKey = rows[firstIndex]?.key ?? null;
    const prev = memo.current;
    const prependedAt =
      prev.firstKey !== null && firstKey !== null && firstKey !== prev.firstKey
        ? rows.findIndex((row) => row.key === prev.firstKey)
        : -1;
    const measurements = virtualOn ? measurementsOf(virtualizer) : [];
    if (prependedAt > 0) {
      if (virtualOn) {
        el.scrollTop +=
          (measurements[prependedAt]?.start ?? 0) - prev.firstStart;
      } else {
        el.scrollTop += el.scrollHeight - prev.height;
      }
    } else if (prev.atBottom || prev.follow) {
      if (virtualOn) {
        if (
          count > 0 &&
          (virtualizer.getTotalSize() !== prev.total ||
            rows[rows.length - 1]?.key !== prev.lastKey)
        ) {
          el.scrollTop = el.scrollHeight;
        }
      } else if (el.scrollHeight !== prev.height) {
        el.scrollTop = el.scrollHeight;
      }
    }
    const keyIndex = firstKey === null ? -1 : firstIndex;
    memo.current = {
      firstKey,
      firstIndex: keyIndex,
      firstStart: measurements[keyIndex]?.start ?? 0,
      lastKey: rows[rows.length - 1]?.key ?? null,
      height: el.scrollHeight,
      total: virtualOn ? virtualizer.getTotalSize() : 0,
      atBottom: isAtBottom(el),
      follow: memo.current.follow,
      request: memo.current.request,
    };
  }, [rows, hasTyping, virtualOn]);

  /** Brings a loaded message into view, centred; false when it is not in the window. */
  const scrollToMessage = (messageId: string): boolean => {
    const index = rows.findIndex(
      (row) => row.kind === "message" && row.message.id === messageId,
    );
    if (index < 0) return false;
    if (virtualOn) {
      virtualizer.scrollToIndex(index, { align: "center" });
      return true;
    }
    const target = scrollRef.current?.querySelector<HTMLElement>(
      `#message-${CSS.escape(messageId)}`,
    );
    target?.scrollIntoView({ block: "center" });
    return target != null;
  };
  const jumpRef = useRef(scrollToMessage);
  jumpRef.current = scrollToMessage;

  // A search hit points at a message: the row scrolls itself once mounted, so the list first brings it into the window.
  const highlightId = useChatHighlight();
  const highlighted = useRef<string | null>(null);
  useLayoutEffect(() => {
    if (highlightId === null) {
      highlighted.current = null;
      return;
    }
    if (highlighted.current === highlightId || !virtualOn) return;
    if (jumpRef.current(highlightId)) highlighted.current = highlightId;
  }, [highlightId, rows, virtualOn]);

  // An explicit request: open at the first unread message, or go to the newest.
  useLayoutEffect(() => {
    const el = scrollRef.current;
    if (!el || !scrollRequest) return;
    if (memo.current.request === scrollRequest.nonce) return;
    memo.current.request = scrollRequest.nonce;
    if (scrollRequest.target === "divider") {
      if (virtualOn) {
        const index = rows.findIndex((row) => row.kind === "unread");
        if (index >= 0) {
          // Not the bottom: a pinned list would scroll on, past the divider, as the rows measure.
          memo.current.atBottom = false;
          memo.current.follow = false;
          virtualizer.scrollToIndex(index, { align: "center" });
          return;
        }
      } else {
        const divider = el.querySelector<HTMLElement>("[data-unread-divider]");
        if (divider) {
          divider.scrollIntoView({ block: "center" });
          return;
        }
      }
    }
    // The end is the scroll height; rows that measure larger move it and the follow flag carries the list along.
    el.scrollTop = el.scrollHeight;
    memo.current.atBottom = true;
    memo.current.follow = true;
  }, [scrollRequest, rows, virtualOn]);

  /** The reader takes the scroll over: the list stops following the newest messages. */
  function releaseFollow() {
    memo.current.follow = false;
  }

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

  const renderRow = (row: ChatRow, index: number, extra: LiExtra) => {
    if (row.kind === "day") {
      return (
        <li
          {...extra}
          key={row.key}
          role="presentation"
          className="my-1.5 flex h-6 items-center gap-3 px-5 before:h-px before:flex-1 before:bg-border before:content-[''] after:h-px after:flex-1 after:bg-border after:content-['']"
        >
          <span className="text-xs leading-[1.45] font-semibold whitespace-nowrap text-muted-foreground">
            {dayLabel(row.dayKey, locale, copy, timeZone)}
          </span>
        </li>
      );
    }
    if (row.kind === "time") {
      return (
        <li
          {...extra}
          key={row.key}
          role="presentation"
          className="mt-3 flex justify-center px-5"
          data-testid="chat-time-separator"
        >
          <time
            dateTime={row.at}
            className="text-micro leading-[1.45] font-semibold whitespace-nowrap tabular-nums text-muted-foreground"
          >
            {formatClock(row.at, locale, timeZone)}
          </time>
        </li>
      );
    }
    if (row.kind === "unread") {
      return (
        <li
          {...extra}
          key={row.key}
          role="separator"
          aria-label={copy.list.newDivider}
          data-unread-divider=""
          className="my-1.5 flex h-6 items-center gap-3 px-5 text-xs leading-[1.45] font-[650] text-destructive"
        >
          <span className="h-px flex-1 bg-destructive/45" aria-hidden="true" />
          <span>{copy.list.newDivider}</span>
        </li>
      );
    }
    if (row.kind === "pending") {
      return (
        <li {...extra} key={row.key}>
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
        {...extra}
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
            onEdit={props.onEdit ? rowHandlers.onEdit : undefined}
            onDelete={props.onDelete ? rowHandlers.onDelete : undefined}
            onModerate={props.onModerate ? rowHandlers.onModerate : undefined}
            quote={props.quoteOf?.(row.message) ?? null}
            thread={threadView(props.threadCounts, row.message)}
            pinned={props.pinnedIds?.has(row.message.id) ?? false}
            reactions={row.message.reactions ?? NO_REACTIONS}
            workspaceId={props.workspaceId}
            hooks={props.hooks}
            authorName={rowHandlers.authorName}
            onReact={props.onReact ? rowHandlers.onReact : undefined}
            onReply={props.onReply ? rowHandlers.onReply : undefined}
            onOpenThread={
              props.onOpenThread ? rowHandlers.onOpenThread : undefined
            }
            onTogglePin={
              props.onTogglePin ? rowHandlers.onTogglePin : undefined
            }
            onOpenQuote={
              props.onOpenQuote ? rowHandlers.onOpenQuote : undefined
            }
            attachmentsReadOnly={props.attachmentsReadOnly}
          />
        )}
      </li>
    );
  };
  const renderTyping = (extra: LiExtra) => (
    <li {...extra} key="typing" aria-hidden="true">
      <ChatTypingBubble
        ids={typingIds}
        authorName={props.authorName}
        copy={copy}
      />
    </li>
  );

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
        onWheel={releaseFollow}
        onTouchStart={releaseFollow}
        onPointerDown={releaseFollow}
        onKeyDown={releaseFollow}
        className="min-h-0 flex-1 overflow-y-auto overscroll-contain py-3 outline-none focus:outline-none focus-visible:outline-none"
        data-testid="chat-message-list"
      >
        {props.isLoadingOlder ? (
          <p className="px-4 py-2 text-center text-xs text-muted-foreground">
            {copy.list.loadingOlder}
          </p>
        ) : null}
        <RowActivityProvider value={activity}>
          {virtualOn ? (
            <ol
              ref={listRef}
              className={cn(CHAT_COLUMN, "relative")}
              style={{ height: virtualizer.getTotalSize() }}
            >
              {virtualizer.getVirtualItems().map((item) => {
                const extra: LiExtra = {
                  ref: virtualizer.measureElement,
                  "data-index": item.index,
                  "aria-posinset": item.index + 1,
                  "aria-setsize": count,
                  style: {
                    position: "absolute",
                    top: 0,
                    left: 0,
                    width: "100%",
                    transform: `translateY(${item.start - virtualizer.options.scrollMargin}px)`,
                  },
                };
                const row = rows[item.index];
                return row
                  ? renderRow(row, item.index, extra)
                  : renderTyping(extra);
              })}
            </ol>
          ) : (
            <ol ref={listRef} className={cn(CHAT_COLUMN, "flex flex-col pb-2")}>
              {rows.map((row, index) => renderRow(row, index, {}))}
              {hasTyping ? renderTyping({}) : null}
            </ol>
          )}
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
        count={unseenCount}
        countLabel={
          unseenCount > 0
            ? pluralCopy(
                {
                  one: copy.list.jumpLatestNew_one,
                  other: copy.list.jumpLatestNew_other,
                },
                unseenCount,
              )
            : null
        }
        onClick={() => {
          props.onJumpLatest();
          const el = scrollRef.current;
          memo.current.follow = true;
          if (!el) return;
          // Far away: land one screen above the end first, then glide the last screen.
          const left = el.scrollHeight - el.scrollTop - el.clientHeight;
          if (left > el.clientHeight * 2) {
            el.scrollTop = el.scrollHeight - el.clientHeight * 2;
          }
          const calm = window.matchMedia?.(
            "(prefers-reduced-motion: reduce)",
          ).matches;
          el.scrollTo({
            top: el.scrollHeight,
            behavior: calm ? "auto" : "smooth",
          });
          el.focus({ preventScroll: true });
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
 * The jump-to-latest control: a floating pill laid over the bottom of the message
 * list (no layout space, so the list never shifts when it shows), centred on the
 * list. It fades and rises 8 px in 160 ms; hidden it is inert.
 */
function JumpLatest({
  visible,
  label,
  count,
  countLabel,
  onClick,
}: {
  readonly visible: boolean;
  readonly label: string;
  readonly count: number;
  readonly countLabel: string | null;
  readonly onClick: () => void;
}) {
  return (
    <div
      className="pointer-events-none absolute inset-x-0 bottom-3 flex justify-center px-4"
      data-testid="chat-jump-latest-layer"
    >
      <Button
        type="button"
        variant="secondary"
        size="sm"
        aria-label={label}
        title={label}
        tabIndex={visible ? 0 : -1}
        aria-hidden={visible ? undefined : true}
        data-visible={visible ? "true" : "false"}
        data-count={count > 0 ? count : undefined}
        className={cn(
          "h-[34px] max-w-full rounded-full border border-border bg-popover text-popover-foreground shadow-md transition-[opacity,transform,visibility,background-color] duration-[160ms] ease-out hover:bg-muted active:scale-95 active:bg-muted motion-reduce:transition-none",
          visible
            ? "pointer-events-auto visible translate-y-0 opacity-100"
            : "invisible translate-y-2 opacity-0",
        )}
        onClick={onClick}
      >
        <ArrowDown aria-hidden="true" />
        <span className="truncate">{countLabel ?? label}</span>
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
