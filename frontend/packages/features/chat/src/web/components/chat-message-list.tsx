import {
  forwardRef,
  useCallback,
  useLayoutEffect,
  useMemo,
  useRef,
  useState,
  type ComponentPropsWithoutRef,
  type KeyboardEvent,
  type Ref,
} from "react";
import {
  Virtuoso,
  type Components,
  type ListRange,
  type VirtuosoHandle,
} from "react-virtuoso";
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
import { animateScrollTo, prefersReducedMotion } from "./animate-scroll";

/** Within this many pixels of the bottom the list counts as at the bottom. */
const BOTTOM_SLACK_PX = 48;
const NO_REACTIONS: readonly never[] = [];
/** Rows are drawn this far above and below the viewport, so a fast scroll meets rows already measured. */
const VIEWPORT_EXTRA_PX = { top: 600, bottom: 400 } as const;
/** The first row's number: it only counts down, one per older row added above, so the reader keeps their place. */
const START_INDEX = 1_000_000;
/** The height of the strip above the first row: the older-page placeholder shows in it, so loading moves nothing. */
const HEADER_PX = 36;
/** How long a row keeps its entrance class: the base motion duration with room to spare. */
const ENTER_MS = 600;
/** The height of the strip below the last row. */
const FOOTER_PX = 24;
/** The fade above the composer reaches this far above it. */
const FADE_PX = 40;

/** The typing bubble is the last entry of the list, after the rows. */
const TYPING_ENTRY = { kind: "typing", key: "typing" } as const;
type ListEntry = ChatRow | typeof TYPING_ENTRY;

/** What the Virtuoso parts (scroller, list, items, header, footer) read: they are module-level so they keep one identity. */
interface ListContext {
  readonly label: string;
  readonly busy: boolean;
  readonly count: number;
  readonly firstItemIndex: number;
  readonly loadingOlder: boolean;
  readonly loadingNewer: boolean;
  readonly loadingText: string;
  readonly footerPx: number;
  readonly isFresh: (key: string) => boolean;
}

/** A new row slides up 8 px and fades in over the base duration; reduced motion shows it at once. */
const ROW_ENTER =
  "motion-safe:animate-in motion-safe:fade-in-0 motion-safe:slide-in-from-bottom-2 motion-safe:duration-[var(--motion-duration-base)] motion-safe:[animation-timing-function:var(--motion-ease-out)]";

/** The `li` of an entry: its id, its box (a flow root, so the margins of what is inside count in its height) and its entrance. */
function itemShell(entry: ListEntry, fresh: boolean) {
  const base = "flow-root";
  const enter = fresh ? ROW_ENTER : "";
  if (entry.kind === "day")
    return {
      className: cn(base, "py-1.5"),
      "data-testid": "chat-day-separator",
    };
  if (entry.kind === "time")
    return {
      className: cn(base, "pt-3"),
      "data-testid": "chat-time-separator",
    };
  if (entry.kind === "unread") return { className: cn(base, "py-0") };
  if (entry.kind === "typing")
    return { className: cn(base), "aria-hidden": true as const };
  if (entry.kind === "pending") return { className: cn(base, enter) };
  return {
    id: `message-${entry.message.id}`,
    className: cn(base, entry.message.kind === "system" && "py-2", enter),
  };
}

const Scroller: Components<ListEntry, ListContext>["Scroller"] = forwardRef<
  HTMLDivElement,
  ComponentPropsWithoutRef<"div"> & { context?: ListContext }
>(function Scroller({ context, children, className: _c, ...props }, ref) {
  return (
    <div
      ref={ref}
      {...props}
      role="log"
      aria-live="off"
      aria-label={context?.label}
      aria-busy={context?.busy}
      tabIndex={0}
      data-testid="chat-message-list"
      className="overscroll-contain outline-none focus:outline-none focus-visible:outline-none"
    >
      {children}
    </div>
  );
});

const List: Components<ListEntry, ListContext>["List"] = forwardRef<
  HTMLDivElement,
  ComponentPropsWithoutRef<"div"> & { context?: ListContext }
>(function List({ context: _context, children, ...props }, ref) {
  return (
    // The library types its list as a div; an `ol` takes the same ref and props.
    <ol
      ref={ref as unknown as Ref<HTMLOListElement>}
      {...(props as ComponentPropsWithoutRef<"ol">)}
      className={CHAT_COLUMN}
    >
      {children}
    </ol>
  );
});

const Item: Components<ListEntry, ListContext>["Item"] = ({
  item,
  context,
  children,
  ...props
}) => {
  const absolute = Number(
    (props as Record<string, unknown>)["data-item-index"] ?? 0,
  );
  return (
    <li
      {...props}
      {...itemShell(item, context.isFresh(item.key))}
      aria-posinset={absolute - context.firstItemIndex + 1}
      aria-setsize={context.count}
    >
      {children}
    </li>
  );
};

/** A strip of fixed height: the "loading" line comes and goes inside it, so the rows below never move. */
function Strip({
  height,
  text,
}: {
  readonly height: number;
  readonly text: string | null;
}) {
  return (
    <div
      className="flex items-center justify-center text-xs text-muted-foreground"
      style={{ height }}
    >
      {text === null ? null : <p>{text}</p>}
    </div>
  );
}
const Header: Components<ListEntry, ListContext>["Header"] = ({ context }) => (
  <Strip
    height={HEADER_PX}
    text={context?.loadingOlder ? context.loadingText : null}
  />
);
const Footer: Components<ListEntry, ListContext>["Footer"] = ({ context }) => (
  <Strip
    height={context?.footerPx ?? FOOTER_PX}
    text={context?.loadingNewer ? context.loadingText : null}
  />
);
const LIST_COMPONENTS: Components<ListEntry, ListContext> = {
  Scroller,
  List,
  Item,
  Header,
  Footer,
};

function isAtBottom(el: HTMLElement): boolean {
  return el.scrollHeight - el.scrollTop - el.clientHeight < BOTTOM_SLACK_PX;
}

/** Where the scroller must be for `node` to sit in the middle of the view. */
function centeredTop(scroller: HTMLElement, node: HTMLElement): number {
  const offset =
    node.getBoundingClientRect().top - scroller.getBoundingClientRect().top;
  return (
    scroller.scrollTop +
    offset -
    (scroller.clientHeight - node.offsetHeight) / 2
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
  /** The height of the composer floating over the bottom of the list: rows end above it, with a fade under the pill. */
  readonly bottomInset?: number;
  /** The people typing now (not the caller): a typing bubble closes the list. */
  readonly typingIds?: readonly string[];
}

/** The width of the conversation column: it uses the room, up to a wide cap; the composer shares its edges. */
export const CHAT_COLUMN = "mx-auto w-full max-w-[1120px]";

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
  const scrollRef = useRef<HTMLElement | null>(null);
  const wrapRef = useRef<HTMLDivElement | null>(null);
  const virtuoso = useRef<VirtuosoHandle | null>(null);
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
  const entries = useMemo<readonly ListEntry[]>(
    () => (hasTyping ? [...rows, TYPING_ENTRY] : rows),
    [rows, hasTyping],
  );

  // A new window (another channel, or a jump to a message far from the loaded ones) replaces the rows: the list is
  // built again and opens where it was asked to (a message being looked for, the first unread message, or the newest).
  // Older rows added above only move the first row's number down by as many, so the library keeps the reader's place.
  const highlightId = useChatHighlight();
  const numbering = useRef<{
    anchorKey: string | null;
    anchorIndex: number;
    first: number;
    epoch: number;
    /** False for the first frame of a window: a request that arrives then rebuilds the list at its target instead of gliding. */
    settled: boolean;
    openAt: { index: number | "LAST"; align: "center" | "end" };
    opened: boolean;
  }>({
    anchorKey: null,
    anchorIndex: -1,
    first: START_INDEX,
    epoch: 0,
    settled: false,
    openAt: { index: "LAST", align: "end" },
    opened: false,
  });
  const honoredRequest = useRef<number | null>(null);
  {
    const state = numbering.current;
    const anchorAt =
      state.anchorKey === null
        ? -1
        : rows.findIndex((row) => row.key === state.anchorKey);
    const replaced = state.anchorKey !== null && anchorAt < 0;
    if (replaced) {
      state.first = START_INDEX;
      state.epoch += 1;
      state.opened = false;
      state.settled = false;
    } else if (anchorAt > state.anchorIndex) {
      state.first -= anchorAt - state.anchorIndex;
    }
    const anchor = rows.findIndex((row) => row.kind === "message");
    state.anchorKey = anchor < 0 ? null : rows[anchor]!.key;
    state.anchorIndex = anchor;
    // The request for the first unread message came just after the window: the list is built again at it.
    const lateDivider =
      state.opened &&
      !state.settled &&
      scrollRequest?.target === "divider" &&
      honoredRequest.current !== scrollRequest.nonce &&
      rows.some((row) => row.kind === "unread");
    if (lateDivider) {
      state.epoch += 1;
      state.opened = false;
    }
    if (!state.opened && status === "ready" && anchor >= 0) {
      state.opened = true;
      const wanted =
        highlightId !== null
          ? rows.findIndex(
              (row) => row.kind === "message" && row.message.id === highlightId,
            )
          : -1;
      const divider =
        scrollRequest?.target === "divider"
          ? rows.findIndex((row) => row.kind === "unread")
          : -1;
      if (wanted >= 0) state.openAt = { index: wanted, align: "center" };
      else if (divider >= 0) {
        state.openAt = { index: divider, align: "center" };
        honoredRequest.current = scrollRequest!.nonce;
      } else state.openAt = { index: "LAST", align: "end" };
    }
  }
  const firstItemIndex = numbering.current.first;

  // The list state the scroll code reads without re-rendering.
  const memo = useRef<{
    atBottom: boolean;
    request: number | null;
    range: ListRange;
  }>({
    atBottom: true,
    request: null,
    range: { startIndex: 0, endIndex: 0 },
  });
  const cancelGlide = useRef<() => void>(() => undefined);

  // Without a layout (a hidden panel, a test DOM) every row renders in normal flow.
  // A browser starts in the virtual list (the room around it has its height from the first frame); a test DOM starts plain.
  const [virtualOn, setVirtualOn] = useState(
    () => typeof ResizeObserver !== "undefined",
  );
  useLayoutEffect(() => {
    const wrap = wrapRef.current;
    if (!wrap) return;
    const measure = () => setVirtualOn(wrap.offsetHeight > 0);
    measure();
    if (typeof ResizeObserver === "undefined") return;
    const observer = new ResizeObserver(measure);
    observer.observe(wrap);
    return () => observer.disconnect();
  });

  /** Glides the scroller to `top` (or jumps under reduced motion); a new glide replaces the running one. */
  const glide = (top: number | (() => number), instant = false) => {
    const el = scrollRef.current;
    cancelGlide.current();
    if (!el) return;
    cancelGlide.current = animateScrollTo(el, top, {
      reducedMotion: instant ? true : undefined,
    });
  };

  /** Brings the newest row to the end of the view. Far away: land one screen above the end first, then glide the last screen. */
  const glideToBottom = (instant = false) => {
    const el = scrollRef.current;
    if (!el) return;
    const end = () => el.scrollHeight - el.clientHeight;
    if (!instant && end() - el.scrollTop > el.clientHeight * 2) {
      el.scrollTop = end() - el.clientHeight;
    }
    glide(end, instant);
  };

  /** Brings an entry to the middle of the view; false when it is not in the list. */
  const glideToIndex = (
    index: number,
    instant = false,
    retried = false,
  ): boolean => {
    const el = scrollRef.current;
    if (!el || index < 0 || index >= entries.length) return false;
    const node = el.querySelector<HTMLElement>(
      `[data-item-index="${firstItemIndex + index}"]`,
    );
    if (node) {
      let top = centeredTop(el, node);
      glide(() => {
        const live = el.querySelector<HTMLElement>(
          `[data-item-index="${firstItemIndex + index}"]`,
        );
        if (live) top = centeredTop(el, live);
        return top;
      }, instant);
      return true;
    }
    if (retried) return true;
    // Not drawn yet: land one screen short of it, then glide the last screen.
    const below = index > memo.current.range.endIndex;
    virtuoso.current?.scrollToIndex({
      index,
      align: "center",
      behavior: "auto",
      offset: instant ? 0 : (below ? -1 : 1) * el.clientHeight,
    });
    requestAnimationFrame(() =>
      requestAnimationFrame(() => glideToIndex(index, instant, true)),
    );
    return true;
  };

  /** Brings a loaded message into view, centred; false when it is not in the window. */
  const scrollToMessage = (messageId: string): boolean => {
    const index = rows.findIndex(
      (row) => row.kind === "message" && row.message.id === messageId,
    );
    if (index < 0) return false;
    if (virtualOn) return glideToIndex(index);
    const target = scrollRef.current?.querySelector<HTMLElement>(
      `#message-${CSS.escape(messageId)}`,
    );
    target?.scrollIntoView({ block: "center" });
    return target != null;
  };
  const jumpRef = useRef(scrollToMessage);
  jumpRef.current = scrollToMessage;

  // A search hit points at a message: the row scrolls itself once mounted, so the list first brings it into the window.
  const highlighted = useRef<string | null>(null);
  useLayoutEffect(() => {
    if (highlightId === null) {
      highlighted.current = null;
      return;
    }
    if (highlighted.current === highlightId || !virtualOn) return;
    if (jumpRef.current(highlightId)) highlighted.current = highlightId;
  }, [highlightId, rows, virtualOn]);

  // An explicit request: open at the first unread message, or go to the newest. The first one after the list
  // opened lands at once; later ones glide.
  useLayoutEffect(() => {
    const el = scrollRef.current;
    if (!scrollRequest || status !== "ready") return;
    if (memo.current.request === scrollRequest.nonce) return;
    if (honoredRequest.current === scrollRequest.nonce) {
      memo.current.request = scrollRequest.nonce;
      return;
    }
    const instant = !numbering.current.settled;
    if (scrollRequest.target === "divider") {
      const index = rows.findIndex((row) => row.kind === "unread");
      if (index >= 0) {
        if (virtualOn) {
          memo.current.request = scrollRequest.nonce;
          // Not the bottom: a pinned list would scroll on, past the divider, as the rows measure.
          memo.current.atBottom = false;
          glideToIndex(index, instant);
          return;
        }
        const divider = el?.querySelector<HTMLElement>("[data-unread-divider]");
        if (divider) {
          memo.current.request = scrollRequest.nonce;
          divider.scrollIntoView({ block: "center" });
          return;
        }
      }
    }
    if (!el) return;
    memo.current.request = scrollRequest.nonce;
    if (virtualOn) glideToBottom(instant);
    else {
      el.scrollTop = el.scrollHeight;
    }
    memo.current.atBottom = true;
  }, [scrollRequest, rows, virtualOn, status]);
  useLayoutEffect(() => {
    if (status !== "ready") return;
    const id = requestAnimationFrame(() => {
      numbering.current.settled = true;
    });
    return () => cancelAnimationFrame(id);
  }, [status, numbering.current.epoch]);

  /** The reader takes the scroll over: the list stops following the newest messages and any glide ends. */
  function releaseFollow() {
    cancelGlide.current();
  }

  function handleAtBottom(bottom: boolean) {
    memo.current.atBottom = bottom;
    setAtBottom(bottom);
    props.onAtBottomChange(bottom);
  }

  // The plain list (no layout) tells the same story from its own scroll events.
  function handleScroll() {
    const el = scrollRef.current;
    if (!el || virtualOn) return;
    const bottom = isAtBottom(el);
    memo.current.atBottom = bottom;
    setAtBottom(bottom);
    props.onAtBottomChange(bottom);
    if (el.scrollTop < 160 && props.hasOlder && !props.isLoadingOlder) {
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
  // A row slides in when it mounts, and keeps the entrance class while that plays; a row that comes back into the
  // window later (scrolled away and back) does not play it again.
  const enteredAt = useRef(new Map<string, number>());
  const isFresh = useCallback(
    (key: string) => {
      const row = rows.find((r) => r.key === key);
      if (!row) return false;
      const isNew =
        row.kind === "pending" ||
        (row.kind === "message" &&
          baseline.current !== null &&
          baseline.current.space === row.message.spaceId &&
          row.message.sequence > baseline.current.sequence &&
          row.message.authorId !== latest.current.viewerId);
      if (!isNew) return false;
      const now = Date.now();
      const first = enteredAt.current.get(key);
      if (first === undefined) {
        enteredAt.current.set(key, now);
        return true;
      }
      return now - first < ENTER_MS;
    },
    [rows],
  );
  const listContext = useMemo<ListContext>(
    () => ({
      label,
      busy: props.isLoadingOlder || props.isLoadingNewer,
      count: entries.length,
      firstItemIndex,
      loadingOlder: props.isLoadingOlder,
      loadingNewer: props.isLoadingNewer,
      loadingText: copy.list.loadingOlder,
      footerPx: FOOTER_PX + (props.bottomInset ?? 0),
      isFresh,
    }),
    [
      label,
      props.isLoadingOlder,
      props.isLoadingNewer,
      entries.length,
      firstItemIndex,
      copy.list.loadingOlder,
      props.bottomInset,
      isFresh,
    ],
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

  const renderRow = (row: ChatRow) => {
    if (row.kind === "day") {
      return (
        <div className="flex h-6 items-center gap-3 px-5 before:h-px before:flex-1 before:bg-border before:content-[''] after:h-px after:flex-1 after:bg-border after:content-['']">
          <span className="text-xs leading-[1.45] font-semibold whitespace-nowrap text-muted-foreground">
            {dayLabel(row.dayKey, locale, copy, timeZone)}
          </span>
        </div>
      );
    }
    if (row.kind === "time") {
      return (
        <div className="flex justify-center px-5">
          <time
            dateTime={row.at}
            className="text-micro leading-[1.45] font-semibold whitespace-nowrap tabular-nums text-muted-foreground"
          >
            {formatClock(row.at, locale, timeZone)}
          </time>
        </div>
      );
    }
    if (row.kind === "unread") {
      return (
        <div
          role="separator"
          data-unread-divider=""
          aria-label={copy.list.newDivider}
          className="my-1.5 flex h-6 items-center gap-3 px-5 text-xs leading-[1.45] font-[650] text-destructive"
        >
          <span className="h-px flex-1 bg-destructive/45" aria-hidden="true" />
          <span aria-hidden="true">{copy.list.newDivider}</span>
        </div>
      );
    }
    if (row.kind === "pending") {
      return (
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
      );
    }
    const isEditing = editing?.messageId === row.message.id;
    const isOwn = row.message.authorId === props.viewerId;
    return isEditing ? (
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
        onOpenThread={props.onOpenThread ? rowHandlers.onOpenThread : undefined}
        onTogglePin={props.onTogglePin ? rowHandlers.onTogglePin : undefined}
        onOpenQuote={props.onOpenQuote ? rowHandlers.onOpenQuote : undefined}
        attachmentsReadOnly={props.attachmentsReadOnly}
      />
    );
  };
  const renderEntry = (entry: ListEntry) =>
    entry.kind === "typing" ? (
      <ChatTypingBubble
        ids={typingIds}
        authorName={props.authorName}
        copy={copy}
      />
    ) : (
      renderRow(entry)
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
        ref={wrapRef}
        className="min-h-0 flex-1"
        onWheel={releaseFollow}
        onTouchStart={releaseFollow}
        onPointerDown={releaseFollow}
        onKeyDown={releaseFollow}
      >
        <RowActivityProvider value={activity}>
          {virtualOn ? (
            <Virtuoso<ListEntry, ListContext>
              key={`${space}:${numbering.current.epoch}`}
              ref={virtuoso}
              style={{ height: "100%" }}
              data={entries as ListEntry[]}
              context={listContext}
              components={LIST_COMPONENTS}
              computeItemKey={(_, entry) => entry.key}
              itemContent={(_, entry) => renderEntry(entry)}
              firstItemIndex={firstItemIndex}
              initialTopMostItemIndex={numbering.current.openAt}
              alignToBottom
              atBottomThreshold={BOTTOM_SLACK_PX}
              increaseViewportBy={VIEWPORT_EXTRA_PX}
              defaultItemHeight={60}
              // Follows a new row only when the reader is at the end right now (the live position, not the last state
              // the library reported): a reader who just scrolled up keeps their place.
              followOutput={(bottom) =>
                bottom && (!scrollRef.current || isAtBottom(scrollRef.current))
                  ? prefersReducedMotion()
                    ? "auto"
                    : "smooth"
                  : false
              }
              atBottomStateChange={handleAtBottom}
              rangeChanged={(range) => {
                memo.current.range = range;
              }}
              startReached={() => {
                if (props.hasOlder && !props.isLoadingOlder)
                  props.onOlderNeeded();
              }}
              endReached={() => {
                if (props.hasNewer && !props.isLoadingNewer)
                  props.onNewerNeeded();
              }}
              scrollerRef={(node) => {
                // The library clears this on unmount, after the plain list may have taken its place: only a node is kept.
                if (node instanceof HTMLElement) scrollRef.current = node;
              }}
            />
          ) : (
            <div
              ref={(node) => {
                scrollRef.current = node;
              }}
              role="log"
              aria-live="off"
              aria-label={label}
              aria-busy={props.isLoadingOlder || props.isLoadingNewer}
              tabIndex={0}
              onScroll={handleScroll}
              data-testid="chat-message-list"
              className="h-full overflow-y-auto overscroll-contain outline-none focus:outline-none focus-visible:outline-none"
            >
              <Strip
                height={HEADER_PX}
                text={props.isLoadingOlder ? copy.list.loadingOlder : null}
              />
              <ol className={cn(CHAT_COLUMN, "flex flex-col")}>
                {entries.map((entry) => (
                  <li key={entry.key} {...itemShell(entry, false)}>
                    {renderEntry(entry)}
                  </li>
                ))}
              </ol>
              <Strip
                height={FOOTER_PX + (props.bottomInset ?? 0)}
                text={props.isLoadingNewer ? copy.list.loadingOlder : null}
              />
            </div>
          )}
        </RowActivityProvider>
      </div>
      {props.bottomInset ? (
        <div
          aria-hidden="true"
          data-testid="chat-list-fade"
          className="pointer-events-none absolute inset-x-0 bottom-0 bg-gradient-to-t from-background via-background/85 to-transparent"
          style={{ height: props.bottomInset + FADE_PX }}
        />
      ) : null}
      <p role="status" aria-live="polite" className="sr-only">
        {announcement}
      </p>
      <JumpLatest
        inset={props.bottomInset ?? 0}
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
          if (!el) return;
          glideToBottom();
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

/**
 * The jump-to-latest control: a floating pill laid over the bottom of the message
 * list (no layout space, so the list never shifts when it shows), centred on the
 * list. It fades and rises 8 px in 160 ms; hidden it is inert.
 */
function JumpLatest({
  inset,
  visible,
  label,
  count,
  countLabel,
  onClick,
}: {
  /** The height of the composer the pill floats above. */
  readonly inset: number;
  readonly visible: boolean;
  readonly label: string;
  readonly count: number;
  readonly countLabel: string | null;
  readonly onClick: () => void;
}) {
  return (
    <div
      className="pointer-events-none absolute inset-x-0 flex justify-center px-4"
      style={{ bottom: inset + 12 }}
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
          "h-[34px] max-w-full rounded-full border border-border bg-popover text-popover-foreground shadow-md transition-[opacity,transform,visibility,background-color] duration-150 ease-out hover:scale-105 hover:bg-muted active:scale-95 active:bg-muted motion-reduce:transition-none motion-reduce:hover:scale-100 motion-reduce:active:scale-100",
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
