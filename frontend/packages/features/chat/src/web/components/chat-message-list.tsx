import {
  useCallback,
  useEffect,
  useLayoutEffect,
  useMemo,
  useRef,
  useState,
  type KeyboardEvent,
  type ReactElement,
} from "react";
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
  rowPropsEqual,
  type ChatMessageRowProps,
  type QuoteView,
} from "./chat-message-row";
import {
  RowActivityProvider,
  createRowActivityStore,
} from "./chat-row-activity";
import { ChatTypingBubble } from "./chat-typing-indicator";
import { useChatHighlight } from "./chat-highlight-context";
import { animateScrollTo, prefersReducedMotion } from "./animate-scroll";
import { guardResizeObserverLoop } from "./resize-observer-loop-guard";

/** Within this many pixels of the bottom the list counts as at the bottom. */
const BOTTOM_SLACK_PX = 48;
const NO_REACTIONS: readonly never[] = [];
/** The height of the strip above the first row: the older-page placeholder shows in it, so loading moves nothing. */
const HEADER_PX = 36;
/** How long a row keeps its entrance class: the base motion duration with room to spare. */
const ENTER_MS = 600;
/** The height of the strip below the last row. */
const FOOTER_PX = 24;
/** The fade above the composer reaches this far above it. */
const FADE_PX = 40;

/**
 * Off-screen rows cost no layout or paint (`content-visibility`), and the browser keeps the size of a row once it has
 * been seen (`auto`); until then a row counts as this tall.
 */
const ROW_STYLE = {
  contentVisibility: "auto",
  containIntrinsicSize: "auto 64px",
} as const;

/** An older page is mounted this many rows per frame, nearest the reader first, so one page never holds a frame. */
const MOUNT_CHUNK = 5;

/** How many screens above and below the view are drawn in full. */
const REALIZE_SCREENS = 2;

/** The `li` built for a stored message, with what it was built from. */
interface KeptItem {
  readonly props: ChatMessageRowProps;
  readonly fresh: boolean;
  readonly el: ReactElement;
}

/** The typing bubble is the last entry of the list, after the rows. */
const TYPING_ENTRY = { kind: "typing", key: "typing" } as const;
type ListEntry = ChatRow | typeof TYPING_ENTRY;

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
  const listRef = useRef<HTMLOListElement | null>(null);
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
  useLayoutEffect(() => guardResizeObserverLoop(), []);
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
  const allEntries = useMemo<readonly ListEntry[]>(
    () => (hasTyping ? [...rows, TYPING_ENTRY] : rows),
    [rows, hasTyping],
  );
  // Rows added above the old first message wait unmounted and join a chunk at a time (see MOUNT_CHUNK).
  const headKey =
    allEntries.find((e) => e.kind === "message" || e.kind === "pending")?.key ??
    "";
  const [paged, setPaged] = useState({ head: headKey, held: 0 });
  if (paged.head !== headKey) {
    const at = allEntries.findIndex((e) => e.key === paged.head);
    setPaged({ head: headKey, held: at > 0 ? at : 0 });
  }
  const held = Math.min(paged.held, allEntries.length);
  useEffect(() => {
    if (held === 0) return;
    const id = requestAnimationFrame(() =>
      setPaged((p) => ({ ...p, held: Math.max(0, p.held - MOUNT_CHUNK) })),
    );
    return () => cancelAnimationFrame(id);
  }, [held]);
  const entries = useMemo(
    () => (held > 0 ? allEntries.slice(held) : allEntries),
    [allEntries, held],
  );
  // The `li` of every entry, kept while the inputs of the rows are the same objects: a chunk of an older page joining
  // re-renders this list with the same props, and building 500 elements again each frame was the cost of that frame.
  const built = useRef<{
    props: ChatMessageListProps;
    entries: readonly ListEntry[];
    laidOut: boolean;
    items: ReactElement[];
    kept: Map<string, KeptItem>;
  } | null>(null);

  // A new window (another channel, or a jump to a message far from the loaded ones) replaces the rows: the list is
  // built again and opens where it was asked to (a message being looked for, the first unread message, or the newest).
  // Older rows added above only move the first row's number down by as many, so the library keeps the reader's place.
  const highlightId = useChatHighlight();
  const numbering = useRef<{
    anchorKey: string | null;
    epoch: number;
    /** False for the first frame of a window: a request that arrives then rebuilds the list at its target instead of gliding. */
    settled: boolean;
    openAt: { index: number | "LAST"; align: "center" | "end" };
    opened: boolean;
  }>({
    anchorKey: null,
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
      state.epoch += 1;
      state.opened = false;
      state.settled = false;
    }
    const anchor = rows.findIndex((row) => row.kind === "message");
    state.anchorKey = anchor < 0 ? null : rows[anchor]!.key;
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

  // The list state the scroll code reads without re-rendering.
  const memo = useRef<{
    atBottom: boolean;
    request: number | null;
  }>({
    atBottom: true,
    request: null,
  });
  const cancelGlide = useRef<() => void>(() => undefined);

  // A browser starts with a layout (the room around the list has its height from the first frame); a test DOM starts without.
  const [laidOut, setLaidOut] = useState(
    () => typeof ResizeObserver !== "undefined",
  );
  useLayoutEffect(() => {
    const wrap = wrapRef.current;
    if (!wrap) return;
    const measure = () => setLaidOut(wrap.offsetHeight > 0);
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

  // A new row at the end while the reader is there: follow it over the row's own 180 ms slide-in. The library's own
  // follow (and its "size increased" catch-up) jumps the whole row in one frame, so it is off (followOutput={false}).
  const lastKey = entries.length > 0 ? entries[entries.length - 1]!.key : "";
  const lastSeen = useRef({ space: "", key: "" });
  useLayoutEffect(() => {
    const prev = lastSeen.current;
    lastSeen.current = { space, key: lastKey };
    const el = scrollRef.current;
    // Layout effects run before the list measures the new row: the end is still where the reader was.
    if (!laidOut || !el || prev.key === "" || prev.space !== space) return;
    if (prev.key === lastKey || !isAtBottom(el)) return;
    cancelGlide.current();
    // The end follows the row while it is measured: the target is read every frame for the whole 180 ms.
    // Never backwards: the list's height dips for a frame while it swaps the row's estimate for its measure.
    let end = el.scrollTop;
    cancelGlide.current = animateScrollTo(
      el,
      () => (end = Math.max(end, el.scrollHeight - el.clientHeight)),
      { durationMs: 180, gentle: true, reducedMotion: prefersReducedMotion() },
    );
  }, [lastKey, space, laidOut]);

  const rowNode = (key: string) =>
    scrollRef.current?.querySelector<HTMLElement>(
      `[data-row-key="${CSS.escape(key)}"]`,
    ) ?? null;

  /** Brings an entry to the middle of the view; false when it is not in the list. */
  const glideToIndex = (index: number, instant = false): boolean => {
    const el = scrollRef.current;
    const entry = allEntries[index];
    if (!el || !entry || !rowNode(entry.key)) return false;
    const top = () => centeredTop(el, rowNode(entry.key)!);
    // Far away: land one screen short of it, then glide the last screen.
    const gap = top() - el.scrollTop;
    if (!instant && Math.abs(gap) > el.clientHeight * 2) {
      el.scrollTop += gap - Math.sign(gap) * el.clientHeight;
    }
    glide(top, instant);
    return true;
  };

  /** Brings a loaded message into view, centred; false when it is not in the window. */
  const scrollToMessage = (messageId: string): boolean => {
    const index = rows.findIndex(
      (row) => row.kind === "message" && row.message.id === messageId,
    );
    return index < 0 ? false : glideToIndex(index);
  };
  const jumpRef = useRef(scrollToMessage);
  jumpRef.current = scrollToMessage;

  // The reader's place: the row at the middle of the view, and how far under the top edge it starts. A size change
  // above it (a row seen for the first time, an older page added) puts it back at that offset before the frame is
  // painted, so what the reader looks at never moves. Safari has no `overflow-anchor`, so this is done here for all.
  const place = useRef<{ key: string; offset: number } | null>(null);
  function recordPlace() {
    const el = scrollRef.current;
    const items = listRef.current?.children;
    place.current = null;
    if (!el || !items) return;
    let lo = 0;
    let hi = items.length - 1;
    let found: HTMLElement | null = null;
    while (lo <= hi) {
      const mid = (lo + hi) >> 1;
      const li = items[mid] as HTMLElement;
      if (li.offsetTop + li.offsetHeight > el.scrollTop + el.clientHeight / 2) {
        found = li;
        hi = mid - 1;
      } else lo = mid + 1;
    }
    // (The middle, not the top: the row at the seam of a page changes its own size when it joins the group above.)
    // Separators move with the page (a day's line is dropped when an older page continues that day): only a message
    // or a sending row is a place to keep.
    while (found && found.dataset.rowAnchor === undefined)
      found = found.nextElementSibling as HTMLElement | null;
    if (found?.dataset.rowKey)
      place.current = {
        key: found.dataset.rowKey,
        offset: found.offsetTop - el.scrollTop,
      };
  }
  // The rows within this much of the view are drawn in full (their real sizes are known), so a row never changes size
  // in the browser's own pass while the reader looks at the rows next to it: it is done here, in the scroll event,
  // and the reader's place is put back before the frame is painted.
  function realize() {
    const el = scrollRef.current;
    const items = listRef.current?.children;
    if (!el || !items) return;
    const lo = el.scrollTop - el.clientHeight * REALIZE_SCREENS;
    const hi = el.scrollTop + el.clientHeight * (1 + REALIZE_SCREENS);
    const changes: [HTMLElement, boolean][] = [];
    for (let i = 0; i < items.length; i++) {
      const li = items[i] as HTMLElement;
      const near = li.offsetTop + li.offsetHeight > lo && li.offsetTop < hi;
      if (near !== (li.dataset.near === "")) changes.push([li, near]);
    }
    for (const [li, near] of changes) {
      if (near) li.dataset.near = "";
      else delete li.dataset.near;
      li.style.contentVisibility = near ? "visible" : "auto";
    }
  }
  // A window opened at its newest message stays on the end while the rows near it are drawn for the first time
  // (their sizes are only known then); the reader's first input ends it.
  const pinned = useRef<number | null>(null);
  function restorePlace(follow = false) {
    const el = scrollRef.current;
    if (follow && pinned.current !== null && el) {
      el.scrollTop = el.scrollHeight;
      return;
    }
    const at = place.current;
    const li = el && at ? rowNode(at.key) : null;
    if (!el || !at || !li) return;
    const want = li.offsetTop - at.offset;
    if (Math.abs(el.scrollTop - want) >= 1) el.scrollTop = want;
  }
  // A window opens where it was asked to: the message being looked for, the first unread one, or the newest.
  const placed = useRef("");
  useLayoutEffect(() => {
    const el = scrollRef.current;
    const state = numbering.current;
    if (!laidOut || !el || status !== "ready" || !state.opened) return;
    const id = `${space}:${state.epoch}`;
    if (placed.current === id) return;
    placed.current = id;
    const at =
      state.openAt.index === "LAST" ? null : allEntries[state.openAt.index];
    const node = at ? rowNode(at.key) : null;
    el.scrollTop = node ? centeredTop(el, node) : el.scrollHeight;
    recordPlace();
    realize();
    restorePlace();
    if (!node) {
      window.clearTimeout(pinned.current ?? undefined);
      pinned.current = window.setTimeout(() => (pinned.current = null), 600);
    }
    recordPlace();
    // The jump control follows where the list ended up, even when no scroll event comes after the placement.
    requestAnimationFrame(updateBottom);
  });
  useLayoutEffect(() => {
    restorePlace();
    realize();
    restorePlace();
    updateBottom();
  }, [entries]);
  // Rows measured as they come near the view change the height of the list: the reader's place is kept.
  const hasList = entries.length > 0;
  useLayoutEffect(() => {
    const ol = listRef.current;
    if (!laidOut || !ol || typeof ResizeObserver === "undefined") return;
    const observer = new ResizeObserver(() => {
      restorePlace(true);
      updateBottom();
    });
    observer.observe(ol);
    return () => observer.disconnect();
  }, [laidOut, hasList, status]);

  // A search hit points at a message: the row scrolls itself once mounted, so the list first brings it into the window.
  const highlighted = useRef<string | null>(null);
  useLayoutEffect(() => {
    if (highlightId === null) {
      highlighted.current = null;
      return;
    }
    if (highlighted.current === highlightId || !laidOut) return;
    if (jumpRef.current(highlightId)) highlighted.current = highlightId;
  }, [highlightId, rows, laidOut]);

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
        if (laidOut) {
          memo.current.request = scrollRequest.nonce;
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
    if (laidOut) glideToBottom(instant);
    else {
      el.scrollTop = el.scrollHeight;
    }
  }, [scrollRequest, rows, laidOut, status]);
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
    window.clearTimeout(pinned.current ?? undefined);
    pinned.current = null;
  }

  function updateBottom() {
    const el = scrollRef.current;
    if (!el) return;
    const bottom = isAtBottom(el);
    if (bottom === memo.current.atBottom) return;
    memo.current.atBottom = bottom;
    setAtBottom(bottom);
    props.onAtBottomChange(bottom);
  }

  /** Within reach of either end: ask for the page beyond it. */
  function checkEdges() {
    const el = scrollRef.current;
    if (!el) return;
    if (
      el.scrollTop < el.clientHeight * 3 &&
      props.hasOlder &&
      !props.isLoadingOlder
    ) {
      props.onOlderNeeded();
    }
    if (isAtBottom(el) && props.hasNewer && !props.isLoadingNewer) {
      props.onNewerNeeded();
    }
  }
  useLayoutEffect(() => {
    if (laidOut && placed.current === `${space}:${numbering.current.epoch}`)
      checkEdges();
  }, [
    laidOut,
    entries.length,
    props.hasOlder,
    props.isLoadingOlder,
    props.hasNewer,
    props.isLoadingNewer,
  ]);

  const lastTop = useRef(0);
  function handleScroll() {
    const el = scrollRef.current;
    // Moving up ends the stay at the end (the reader, or a jump, took the scroll).
    if (el && el.scrollTop < lastTop.current - 1) {
      window.clearTimeout(pinned.current ?? undefined);
      pinned.current = null;
    }
    if (el) lastTop.current = el.scrollTop;
    recordPlace();
    realize();
    restorePlace();
    updateBottom();
    checkEdges();
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
  const rowByKey = useMemo(() => new Map(rows.map((r) => [r.key, r])), [rows]);
  const isFresh = useCallback(
    (key: string) => {
      const row = rowByKey.get(key);
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
    [rowByKey],
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

  /** What a stored message's row is given: the cached `li` of a row is kept while this is equal. */
  const messageRowProps = (
    row: Extract<ChatRow, { kind: "message" }>,
  ): ChatMessageRowProps => ({
    message: row.message,
    author: { name: props.authorName(row.message.authorId) },
    continued: row.continued,
    groupEnd: row.groupEnd,
    showName: props.showNames ?? true,
    showSent: row.key === lastOwnKey,
    isOwn: row.message.authorId === props.viewerId,
    locale: locale,
    timeZone: timeZone,
    copy: copy,
    onEdit: props.onEdit ? rowHandlers.onEdit : undefined,
    onDelete: props.onDelete ? rowHandlers.onDelete : undefined,
    onModerate: props.onModerate ? rowHandlers.onModerate : undefined,
    quote: props.quoteOf?.(row.message) ?? null,
    thread: threadView(props.threadCounts, row.message),
    pinned: props.pinnedIds?.has(row.message.id) ?? false,
    reactions: row.message.reactions ?? NO_REACTIONS,
    workspaceId: props.workspaceId,
    hooks: props.hooks,
    authorName: rowHandlers.authorName,
    onReact: props.onReact ? rowHandlers.onReact : undefined,
    onReply: props.onReply ? rowHandlers.onReply : undefined,
    onOpenThread: props.onOpenThread ? rowHandlers.onOpenThread : undefined,
    onTogglePin: props.onTogglePin ? rowHandlers.onTogglePin : undefined,
    onOpenQuote: props.onOpenQuote ? rowHandlers.onOpenQuote : undefined,
    attachmentsReadOnly: props.attachmentsReadOnly,
  });
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
    return editing?.messageId === row.message.id ? (
      <EditForm
        copy={copy}
        editing={editing}
        continued={row.continued}
        onChange={props.onEditDraftChange}
        onSave={props.onEditSave}
        onCancel={props.onEditCancel}
      />
    ) : (
      <ChatMessageRow {...messageRowProps(row)} />
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

  let items: ReactElement[];
  const reuse = built.current;
  if (
    reuse &&
    reuse.props === props &&
    reuse.entries === allEntries &&
    reuse.laidOut === laidOut
  )
    items = reuse.items;
  else {
    // A stored message whose row props are equal to the last build keeps its element: React skips it whole, and the
    // render of a long channel does not create a thousand elements for the few rows that changed.
    const kept = new Map<string, KeptItem>();
    items = allEntries.map((entry) => {
      const fresh = laidOut && isFresh(entry.key);
      const asProps =
        entry.kind === "message" && editing?.messageId !== entry.message.id
          ? messageRowProps(entry)
          : null;
      const old = reuse?.kept.get(entry.key);
      if (
        asProps &&
        old &&
        old.fresh === fresh &&
        rowPropsEqual(old.props, asProps)
      ) {
        kept.set(entry.key, old);
        return old.el;
      }
      const el = (
        <li
          key={entry.key}
          data-row-key={entry.key}
          data-row-anchor={
            entry.kind === "message" || entry.kind === "pending"
              ? ""
              : undefined
          }
          style={ROW_STYLE}
          {...itemShell(entry, fresh)}
        >
          {asProps ? <ChatMessageRow {...asProps} /> : renderEntry(entry)}
        </li>
      );
      if (asProps) kept.set(entry.key, { props: asProps, fresh, el });
      return el;
    });
    built.current = { props, entries: allEntries, laidOut, items, kept };
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
        ref={wrapRef}
        className="min-h-0 flex-1"
        onWheel={releaseFollow}
        onTouchStart={releaseFollow}
        onPointerDown={releaseFollow}
        onKeyDown={releaseFollow}
      >
        <RowActivityProvider value={activity}>
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
            style={{ overflowAnchor: "none" }}
            className="relative h-full overflow-y-auto overscroll-contain outline-none focus:outline-none focus-visible:outline-none"
          >
            <div className="flex min-h-full flex-col justify-end">
              <Strip
                height={HEADER_PX}
                text={props.isLoadingOlder ? copy.list.loadingOlder : null}
              />
              <ol ref={listRef} className={cn(CHAT_COLUMN, "flex flex-col")}>
                {held > 0 ? items.slice(held) : items}
              </ol>
              <Strip
                height={FOOTER_PX + (props.bottomInset ?? 0)}
                text={props.isLoadingNewer ? copy.list.loadingOlder : null}
              />
            </div>
          </div>
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
          "h-[34px] max-w-full rounded-full border border-border bg-popover text-popover-foreground shadow-md transition-[opacity,transform,translate,visibility,background-color,box-shadow] duration-200 ease-[cubic-bezier(.34,1.56,.64,1)] hover:-translate-y-0.5 hover:bg-muted hover:shadow-lg active:translate-y-0 active:bg-muted active:shadow-md motion-reduce:transition-none motion-reduce:hover:translate-y-0 motion-reduce:active:translate-y-0",
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
