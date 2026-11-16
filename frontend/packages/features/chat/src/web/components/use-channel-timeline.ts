import { useEffect, useMemo, useRef, useState } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { chatQueryKeys } from "../../query/keys";
import { buildChatRows, unconfirmedPending } from "../../core/model/chat-model";
import type { ChatHistoryAnchor } from "../../core/types/chat";
import { flattenPages, type ChatHooks } from "../query/create-chat-hooks";
import type { ChatScrollRequest } from "./chat-message-list";

const LATEST: ChatHistoryAnchor = { kind: "latest" };

interface ChannelLock {
  readonly channelId: string;
  readonly anchor: ChatHistoryAnchor;
  /** The first unread sequence when the channel opened (the "New" divider). */
  readonly firstUnread: number | null;
}

/** A message to land on (a search hit or a pin): its space, its sequence and, for a thread reply, the thread to show. */
export interface TimelineJumpTarget {
  readonly spaceId: string;
  readonly messageId: string;
  readonly sequence: number;
  readonly threadRootId: string | null;
}

interface TimelineJump extends TimelineJumpTarget {
  readonly applied: boolean;
}

export interface ChannelTimelineInput {
  readonly hooks: ChatHooks;
  readonly workspaceId: string;
  /** The open space (channel or conversation), or null on the bare chat route. */
  readonly channelId: string | null;
  /** The `?m=` of the address the page was opened with: the history opens around it. */
  readonly linkedSequence: number | null;
  /** The unread summary has answered (or failed): the anchor may be chosen. */
  readonly unreadSettled: boolean;
  /** The open space's first unread sequence in that summary, or null. */
  readonly firstUnreadSequence: number | null;
  readonly timeZone?: string;
  /** Shows a thread when a jump lands on a thread reply. */
  readonly onShowThread: (rootId: string) => void;
}

/**
 * The message window of the open space and its read state: which window opens (first unread, a linked message
 * or the newest), older and newer pages, the "New" divider, mark-read, the highlight and the jump to the newest.
 * The list reports whether the reader is at the bottom (`setAtBottom`); everything else is derived here.
 */
export function useChannelTimeline({
  hooks,
  workspaceId: ws,
  channelId,
  linkedSequence,
  unreadSettled,
  firstUnreadSequence,
  timeZone,
  onShowThread,
}: ChannelTimelineInput) {
  const queryClient = useQueryClient();
  const ch = channelId ?? "";
  const reader = hooks.useMarkRead(ws);

  const [jump, setJump] = useState<TimelineJump | null>(null);
  const [highlightId, setHighlightId] = useState<string | null>(null);
  const [scrollRequest, setScrollRequest] = useState<ChatScrollRequest | null>(
    null,
  );
  const [atBottom, setAtBottom] = useState(true);
  const requestedFor = useRef<string | null>(null);
  const nonce = useRef(0);

  // Latest callbacks, read by effects that must not re-run when only their identity changes.
  const markReadRef = useRef(reader.markRead);
  markReadRef.current = reader.markRead;
  const showThreadRef = useRef(onShowThread);
  showThreadRef.current = onShowThread;

  // The history opens once per visit: at the first unread message when there is
  // one (D03), otherwise at the newest. The choice does not move while the reader
  // is in the channel, even when the unread summary refreshes.
  const lockRef = useRef<ChannelLock | null>(null);
  const linkTarget = useRef<{
    readonly channelId: string;
    readonly sequence: number;
    applied?: boolean;
  } | null>(null);
  /** Cancels the send's deferred jump to the newest window (frames, then a task). */
  const jumpWorkRef = useRef<(() => void) | null>(null);
  // Unmount only: the ref holds the work that is pending then, so it is read at that time on purpose.
  useEffect(() => () => jumpWorkRef.current?.(), []);
  if (channelId && lockRef.current?.channelId !== channelId && unreadSettled) {
    const first = firstUnreadSequence;
    // An address with ?m= opens around that message instead of the first unread one (UC-CHT-B10), once per visit.
    linkTarget.current =
      linkedSequence !== null ? { channelId, sequence: linkedSequence } : null;
    const linked = linkedSequence;
    lockRef.current = {
      channelId,
      anchor:
        linked !== null
          ? { kind: "around", sequence: linked }
          : first !== null
            ? { kind: "around", sequence: first }
            : LATEST,
      firstUnread: linked !== null ? null : first,
    };
  }
  const lock =
    channelId && lockRef.current?.channelId === channelId
      ? lockRef.current
      : null;
  const anchor = lock?.anchor ?? LATEST;

  const messages = hooks.useMessages(ws, ch, anchor);
  const flat = useMemo(() => flattenPages(messages.data), [messages.data]);
  const pendingQuery = hooks.usePendingSends(ws, ch);
  const pendingAll = useMemo(
    () => pendingQuery.data ?? [],
    [pendingQuery.data],
  );
  const pendingHere = useMemo(
    () =>
      unconfirmedPending(
        pendingAll.filter((entry) => entry.spaceId === ch),
        flat,
      ),
    [pendingAll, flat, ch],
  );
  // The divider stays where the visit found it, even as messages arrive.
  const firstUnread = lock ? lock.firstUnread : null;
  const rows = useMemo(
    () =>
      buildChatRows({
        messages: flat,
        pending: pendingHere,
        firstUnread,
        timeZone,
      }),
    [flat, pendingHere, firstUnread, timeZone],
  );

  function request(target: "divider" | "bottom") {
    nonce.current += 1;
    setScrollRequest({ nonce: nonce.current, target });
  }

  // Open the channel at its divider (or the newest) once its history has loaded.
  useEffect(() => {
    // A window kept on screen while the next one loads (placeholder) is not the history to open on.
    if (
      !channelId ||
      !messages.isSuccess ||
      messages.isPlaceholderData ||
      !lock
    )
      return;
    const key = `${channelId}|${JSON.stringify(anchor)}`;
    if (requestedFor.current === key) return;
    requestedFor.current = key;
    nonce.current += 1;
    setScrollRequest({
      nonce: nonce.current,
      target: lock.firstUnread !== null ? "divider" : "bottom",
    });
    setAtBottom(true);
  }, [channelId, messages.isSuccess, messages.isPlaceholderData, lock, anchor]);

  // Read state: the boundary moves to the newest message only when it is
  // loaded and on screen (the reader is at the bottom and the page is visible).
  const newest = flat[flat.length - 1];
  const newestSequence = newest?.sequence;
  const hasNewer = messages.hasNextPage === true;
  useEffect(() => {
    if (!channelId || newestSequence === undefined || hasNewer || !atBottom)
      return;
    if (
      typeof document !== "undefined" &&
      document.visibilityState === "hidden"
    )
      return;
    markReadRef.current(channelId, newestSequence);
  }, [channelId, newestSequence, hasNewer, atBottom]);

  // The reader is looking at this channel's newest message: its unread frames add nothing (the read boundary moves
  // with the message above), so neither sidebar flashes a badge for a message that is read as it arrives.
  const hasNewest = newest !== undefined;
  useEffect(() => {
    if (!channelId || !hasNewest || hasNewer || !atBottom) return;
    const viewingKey = chatQueryKeys.viewing(ws);
    const publish = () =>
      queryClient.setQueryData<string | null>(
        viewingKey,
        document.visibilityState === "hidden" ? null : channelId,
      );
    publish();
    document.addEventListener("visibilitychange", publish);
    return () => {
      document.removeEventListener("visibilitychange", publish);
      queryClient.setQueryData<string | null>(viewingKey, null);
    };
  }, [channelId, hasNewest, hasNewer, atBottom, ws, queryClient]);

  function jumpToLatest() {
    if (channelId) {
      lockRef.current = { channelId, anchor: LATEST, firstUnread: null };
      requestedFor.current = null;
    }
    request("bottom");
  }

  // A search hit (UC-CHT-H07): once its space is open, show the message. A message already loaded is only highlighted; otherwise the
  // history reopens around its sequence. A thread reply opens its thread. The highlight fades from state after a few seconds.
  useEffect(() => {
    if (!jump || jump.applied || !lock || channelId !== jump.spaceId) return;
    if (!flat.some((entry) => entry.id === jump.messageId)) {
      const target: ChatHistoryAnchor = {
        kind: "around",
        sequence: jump.sequence,
      };
      lockRef.current = {
        channelId: jump.spaceId,
        anchor: target,
        firstUnread: null,
      };
      requestedFor.current = `${jump.spaceId}|${JSON.stringify(target)}`;
    }
    if (jump.threadRootId !== null) {
      showThreadRef.current(jump.threadRootId);
    }
    setJump({ ...jump, applied: true });
  }, [jump, lock, flat, channelId]);

  // The message a link points at (UC-CHT-B10): highlighted once, when its window has loaded. A sequence that is not in the
  // window (a missing message, or one the caller may not read) highlights nothing and shows no detail about it.
  useEffect(() => {
    const target = linkTarget.current;
    if (!target || target.applied || target.channelId !== channelId) return;
    if (messages.isPlaceholderData || !messages.isSuccess) return;
    target.applied = true;
    const hit = flat.find((entry) => entry.sequence === target.sequence);
    if (hit) setHighlightId(hit.id);
  }, [channelId, flat, messages.isSuccess, messages.isPlaceholderData]);

  useEffect(() => {
    if (highlightId === null) return;
    const timer = setTimeout(() => setHighlightId(null), 6000);
    return () => clearTimeout(timer);
  }, [highlightId]);

  /** Lands on a message like a search hit: highlights it and, when its window is not loaded, reopens around it. */
  function openMessage(target: TimelineJumpTarget) {
    setHighlightId(target.messageId);
    setJump({ ...target, applied: false });
  }

  /** Call after a send: scrolls to the bottom, and from an older window goes on to the newest window. */
  function afterSend() {
    if (!channelId) return;
    request("bottom");
    // From an older window the bubble paints at the end of the window on screen; the newest window loads after
    // that frame, so the keypress does not wait for a cold render of a whole page of rows.
    if (hasNewer) {
      const sentIn = channelId;
      // The bubble's frame is produced first: the work starts a task after the second frame callback.
      jumpWorkRef.current?.();
      let frame2 = 0;
      let timer: ReturnType<typeof setTimeout> | undefined;
      const frame1 = requestAnimationFrame(() => {
        frame2 = requestAnimationFrame(() => {
          timer = setTimeout(() => {
            jumpWorkRef.current = null;
            if (lockRef.current?.channelId !== sentIn) return;
            jumpToLatest();
          }, 0);
        });
      });
      // Unmount cancels whichever step is still waiting, so nothing runs after the page is gone.
      jumpWorkRef.current = () => {
        cancelAnimationFrame(frame1);
        cancelAnimationFrame(frame2);
        clearTimeout(timer);
      };
    }
  }

  return {
    /** The open window's query: pages, status, `fetchPreviousPage`, `fetchNextPage`, `refetch`. */
    messages,
    /** The loaded messages, oldest first. */
    flat,
    pendingAll,
    /** Sends not yet shown by a window, drawn after the messages. */
    pendingHere,
    rows,
    hasNewer,
    scrollRequest,
    /** The list reports whether the reader is at the bottom. */
    setAtBottom,
    highlightId,
    openMessage,
    jumpToLatest,
    afterSend,
  };
}
