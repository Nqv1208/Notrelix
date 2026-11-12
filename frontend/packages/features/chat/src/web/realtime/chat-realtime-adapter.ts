/**
 * Chat realtime adapter (UC-CHT-C02 to C06, C09). The host registers it in its
 * module adapter registry and feeds it the frames of two subscriptions: the
 * ACTIVE channel's stream (scope channel, one at a time) and the caller's own
 * per-user stream (scope user, unread counters). The adapter never creates a
 * client and never subscribes: lifecycle belongs to the host (FE-RT-004).
 *
 * Frames are applied to the cache through the pure reducers. When a frame cannot
 * be applied safely (a gap, a text that did not fit, a recovery signal), the
 * adapter fetches the missing range over REST after the last known sequence and
 * merges it (C04, C05); repeated trouble drops the channel's window and reloads
 * the newest page.
 *
 * Structural types only (no `@notrelix/realtime` import), like the other feature
 * adapters: the host passes its own envelope and context.
 */

import type { QueryClient } from "@tanstack/react-query";
import { CHAT_LIMITS, type ChatService } from "../../core";
import type { ChatMessage, ChatUnreadSummary } from "../../core/types/chat";
import type { PendingMessage } from "../../core/model/chat-model";
import {
  isChatMessageEvent,
  parseChatMessageFrame,
  parseChatReactionFrame,
  parseChatSpaceFrame,
  parseChatUnreadFrame,
  CHAT_REACTION_EVENT,
  CHAT_SPACE_EVENT,
  CHAT_UNREAD_EVENT,
  type ChatMessageFramePayload,
} from "../../core/realtime/chat-frames";
import { chatQueryKeys } from "../../query/keys";
import {
  applyMessageCreated,
  applyMessageDeleted,
  applyMessageUpdated,
  applyUnreadFrame,
  lastKnownSequence,
  mergeHistoryPage,
  messageFromCreatedFrame,
  windowIsLive,
  type MessageFrameOutcome,
} from "../query/chat-realtime-reducers";
import type { ChatMessagesCache } from "../query/message-cache";
import {
  applyReactionChange,
  patchMessageInData,
} from "../query/interaction-cache";
import { applyStoredReply } from "../query/thread-writes";

export interface ChatRealtimeEnvelope {
  readonly eventType: string;
  readonly eventId: string;
  readonly resourceId: string;
  readonly workspaceId: string;
  readonly timestamp: string;
  readonly payload?: Readonly<Record<string, unknown>>;
  readonly payloadOmitted?: boolean;
}

export interface ChatRealtimeContext {
  readonly workspaceId: string;
  /** The open channel, when the frame came through its subscription. */
  readonly channelId?: string;
  readonly invalidateQueries: (
    keys: readonly unknown[][],
  ) => Promise<void> | void;
}

/** A recovery signal of the realtime transport, as the host passes it on. */
export interface ChatRecoverySignal {
  readonly reason:
    | "subscribed"
    | "gap"
    | "duplicate"
    | "reset"
    | "access-changed"
    | "permission-lost"
    | "subscription-failed";
  readonly workspaceId: string;
  readonly scope: { readonly kind: string; readonly id: string };
}

export interface ChatRealtimeAdapterOptions {
  readonly queryClient: QueryClient;
  readonly service: Pick<ChatService, "listMessages">;
  /** The caller's user id: a reaction frame of the caller is already applied by the caller's own write. */
  readonly viewerId?: string;
}

/** Bounded backfill: past this many pages the window is reloaded instead (C04, repeated gaps). */
const MAX_BACKFILL_PAGES = 5;

export interface ChatRealtimeAdapter {
  readonly id: string;
  supports(envelope: ChatRealtimeEnvelope): boolean;
  validateAndHandle(
    envelope: ChatRealtimeEnvelope,
    context: ChatRealtimeContext,
  ): Promise<void>;
  /**
   * Re-establishes the Chat state after a recovery signal of the channel or
   * per-user stream. Workspace-level access changes are revalidated by the host
   * (every Workspace query, Chat included) before this runs.
   */
  recover(signal: ChatRecoverySignal): Promise<void>;
}

export function createChatRealtimeAdapter(
  options: ChatRealtimeAdapterOptions,
): ChatRealtimeAdapter {
  const { queryClient, service, viewerId } = options;

  /** Pages after `from` (or after the last known sequence) merged into the live window. */
  async function backfill(
    workspaceId: string,
    channelId: string,
    from?: number,
  ): Promise<void> {
    const prefix = chatQueryKeys.messages(workspaceId, channelId);
    const live = queryClient
      .getQueriesData<ChatMessagesCache>({ queryKey: prefix })
      .map(([, cache]) => cache)
      .filter((cache): cache is ChatMessagesCache =>
        Boolean(cache && windowIsLive(cache)),
      );
    if (live.length === 0) return;
    let after = from ?? Math.max(...live.map(lastKnownSequence));
    try {
      for (let page = 0; page < MAX_BACKFILL_PAGES; page++) {
        const next = await service.listMessages(
          workspaceId,
          channelId,
          { after },
          CHAT_LIMITS.pageSize,
        );
        queryClient.setQueriesData<ChatMessagesCache>(
          { queryKey: prefix },
          (cache) => (cache ? mergeHistoryPage(cache, next) : cache),
        );
        const last = next.items[next.items.length - 1];
        if (!next.hasNewer || !last) return;
        after = last.sequence;
      }
      // Still behind after the bounded pages: reload the newest window (C04).
      await queryClient.invalidateQueries({ queryKey: prefix });
    } catch {
      await queryClient.invalidateQueries({ queryKey: prefix });
    }
  }

  /**
   * A thread reply: it joins its open thread and moves its root's reply count. It
   * is not inserted into the channel history, because the frame does not say
   * whether the author also sent it to the channel; the channel backfill from
   * the sequence before it brings the reply in only when it is in the main flow.
   */
  function applyThreadReplyCreated(
    frame: ChatMessageFramePayload,
    envelope: ChatRealtimeEnvelope,
    workspaceId: string,
    channelId: string,
  ): Promise<void> {
    const rootId = frame.threadRootId!;
    const reply: ChatMessage = {
      ...messageFromCreatedFrame(frame, channelId, envelope.timestamp),
      threadRootId: rootId,
      threadSequence: frame.threadSequence,
    };
    applyStoredReply(queryClient, workspaceId, reply);
    return backfill(workspaceId, channelId, frame.sequence - 1);
  }

  function applyChannelFrame(
    envelope: ChatRealtimeEnvelope,
    workspaceId: string,
    channelId: string,
  ): Promise<void> {
    const prefix = chatQueryKeys.messages(workspaceId, channelId);
    const pendingKey = chatQueryKeys.pending(workspaceId, channelId);
    const frame = envelope.payloadOmitted
      ? null
      : parseChatMessageFrame(envelope.payload);
    // Without a usable payload the missing range is fetched after the last known sequence.
    if (!frame) return backfill(workspaceId, channelId);

    if (frame.threadRootId !== null) {
      // The thread's own cache and count move; a reply edit or delete also reaches the loaded thread.
      if (envelope.eventType === "chat.message.created") {
        return applyThreadReplyCreated(frame, envelope, workspaceId, channelId);
      }
      queryClient.setQueriesData(
        { queryKey: chatQueryKeys.thread(workspaceId, frame.threadRootId) },
        (data: unknown) =>
          patchMessageInData(data, frame.id, (message) =>
            message.version >= frame.version
              ? message
              : {
                  ...message,
                  content: frame.deleted ? null : frame.content,
                  edited: frame.editedAt !== null || message.edited,
                  editedAt: frame.editedAt ?? message.editedAt,
                  deleted: frame.deleted,
                  deletedAt: frame.deleted
                    ? (message.deletedAt ?? envelope.timestamp)
                    : message.deletedAt,
                  version: frame.version,
                },
          ),
      );
    }

    let refetchFrom: number | null = null;
    const reduce = (cache: ChatMessagesCache): ChatMessagesCache => {
      let outcome: MessageFrameOutcome;
      if (envelope.eventType === "chat.message.created") {
        outcome = applyMessageCreated(
          cache,
          frame,
          channelId,
          envelope.timestamp,
        );
      } else if (envelope.eventType === "chat.message.updated") {
        outcome = applyMessageUpdated(cache, frame);
      } else {
        outcome = applyMessageDeleted(cache, frame, envelope.timestamp);
      }
      if (outcome.kind === "gap" || outcome.kind === "omitted") {
        refetchFrom =
          refetchFrom === null
            ? outcome.after
            : Math.min(refetchFrom, outcome.after);
      }
      return outcome.kind === "applied" ? outcome.cache : cache;
    };
    queryClient.setQueriesData<ChatMessagesCache>(
      { queryKey: prefix },
      (cache) => (cache ? reduce(cache) : cache),
    );

    // The send of this client is stored: its pending entry is confirmed (matched by clientMessageId).
    if (frame.clientMessageId !== null) {
      queryClient.setQueryData<PendingMessage[]>(pendingKey, (entries) =>
        entries
          ? entries.filter(
              (entry) => entry.clientMessageId !== frame.clientMessageId,
            )
          : entries,
      );
    }

    const gapFrom: number | null = refetchFrom;
    return gapFrom === null
      ? Promise.resolve()
      : backfill(workspaceId, channelId, gapFrom);
  }

  async function applyUnreadFrameOf(
    envelope: ChatRealtimeEnvelope,
    workspaceId: string,
    context: ChatRealtimeContext,
  ): Promise<void> {
    const key = chatQueryKeys.unread(workspaceId);
    const frame = envelope.payloadOmitted
      ? null
      : parseChatUnreadFrame(envelope.payload);
    if (!frame) {
      await queryClient.invalidateQueries({ queryKey: key });
      return;
    }
    const summary = queryClient.getQueryData<ChatUnreadSummary>(key);
    // Nothing loaded yet: the summary fetches on its first read.
    if (!summary) return;
    const outcome = applyUnreadFrame(summary, frame);
    if (outcome.kind === "applied") {
      queryClient.setQueryData<ChatUnreadSummary>(key, outcome.summary);
    } else if (outcome.kind === "unknown-space") {
      // A channel the summary does not list yet: revalidate the list and the summary.
      await context.invalidateQueries([
        [...key],
        [...chatQueryKeys.channels(workspaceId)],
      ]);
    }
  }

  /** A reaction of a message changed: the summary of the message in every loaded cache moves. */
  function applyReactionFrame(
    envelope: ChatRealtimeEnvelope,
    workspaceId: string,
  ): void {
    const frame = envelope.payloadOmitted
      ? null
      : parseChatReactionFrame(envelope.payload);
    if (!frame) return;
    const actorIsViewer = viewerId !== undefined && frame.actorId === viewerId;
    queryClient.setQueriesData(
      { queryKey: chatQueryKeys.all(workspaceId) },
      (data: unknown) =>
        patchMessageInData(data, frame.messageId, (message) => ({
          ...message,
          reactions: applyReactionChange(message.reactions ?? [], {
            kind: frame.kind,
            change: frame.change,
            actorIsViewer,
          }),
        })),
    );
  }

  /** A pin of the channel changed: the pin list is read again (the header and the Pins panel follow it). */
  function applySpaceFrame(
    envelope: ChatRealtimeEnvelope,
    workspaceId: string,
    channelId: string,
  ): Promise<void> {
    const frame = envelope.payloadOmitted
      ? null
      : parseChatSpaceFrame(envelope.payload);
    if (!frame) return Promise.resolve();
    if (frame.change === "permissions") {
      // The capabilities ride on the channel views and the overrides have their own read: both are read again.
      return Promise.all([
        queryClient.invalidateQueries({
          queryKey: chatQueryKeys.channel(workspaceId, channelId),
        }),
        queryClient.invalidateQueries({
          queryKey: chatQueryKeys.channels(workspaceId),
        }),
        queryClient.invalidateQueries({
          queryKey: chatQueryKeys.permissions(workspaceId, channelId),
        }),
      ]).then(() => undefined);
    }
    return queryClient
      .invalidateQueries({
        queryKey: chatQueryKeys.pins(workspaceId, channelId),
      })
      .then(() => undefined);
  }

  return {
    id: "chat-adapter",
    supports(envelope: ChatRealtimeEnvelope): boolean {
      return (
        isChatMessageEvent(envelope.eventType) ||
        envelope.eventType === CHAT_UNREAD_EVENT ||
        envelope.eventType === CHAT_REACTION_EVENT ||
        envelope.eventType === CHAT_SPACE_EVENT
      );
    },
    async validateAndHandle(envelope, context): Promise<void> {
      // A frame of another workspace belongs to a previous scope (NRX-014).
      if (envelope.workspaceId !== context.workspaceId) return;
      if (
        isChatMessageEvent(envelope.eventType) ||
        envelope.eventType === CHAT_REACTION_EVENT ||
        envelope.eventType === CHAT_SPACE_EVENT
      ) {
        // Only the open channel's stream is subscribed; any other channel is ignored.
        if (!context.channelId || envelope.resourceId !== context.channelId)
          return;
        if (envelope.eventType === CHAT_REACTION_EVENT) {
          applyReactionFrame(envelope, context.workspaceId);
          return;
        }
        if (envelope.eventType === CHAT_SPACE_EVENT) {
          await applySpaceFrame(
            envelope,
            context.workspaceId,
            context.channelId,
          );
          return;
        }
        await applyChannelFrame(
          envelope,
          context.workspaceId,
          context.channelId,
        );
        return;
      }
      await applyUnreadFrameOf(envelope, context.workspaceId, context);
    },
    async recover(signal): Promise<void> {
      const workspaceId = signal.workspaceId;
      if (signal.scope.kind === "channel") {
        const channelId = signal.scope.id;
        if (signal.reason === "permission-lost") {
          // Lost access: the channel leaves the sidebar cache and its detail is re-read (no-access state).
          queryClient.setQueryData<{ id: string }[]>(
            chatQueryKeys.channels(workspaceId),
            (list) =>
              list ? list.filter((entry) => entry.id !== channelId) : list,
          );
          await queryClient.invalidateQueries({
            queryKey: chatQueryKeys.channel(workspaceId, channelId),
          });
          return;
        }
        // Subscribed, gap, duplicate or reset: backfill after the last known sequence (C04, C05).
        await backfill(workspaceId, channelId);
        await queryClient.invalidateQueries({
          queryKey: chatQueryKeys.pins(workspaceId, channelId),
        });
        return;
      }
      if (signal.scope.kind === "user") {
        // The per-user stream lost continuity: the counters are read again (C05).
        await queryClient.invalidateQueries({
          queryKey: chatQueryKeys.unread(workspaceId),
        });
        return;
      }
    },
  };
}
