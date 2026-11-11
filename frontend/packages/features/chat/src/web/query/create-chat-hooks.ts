import {
  type InfiniteData,
  type QueryClient,
  type QueryKey,
  useInfiniteQuery,
  useMutation,
  useQuery,
  useQueryClient,
} from "@tanstack/react-query";
import { useRef } from "react";
import {
  createChatService,
  type ChatApiClient,
  type ChatEndpoints,
} from "../../core/api/chat.service";
import { chatFailureOf, type ChatFailure } from "../../core/api/chat-failure";
import {
  mergeMessages,
  nextReadBoundary,
  shouldReportRead,
  unreadAfterRead,
  type PendingMessage,
} from "../../core/model/chat-model";
import {
  CHAT_LIMITS,
  type ChatChannel,
  type ChatChannelDetail,
  type ChatCreateChannelInput,
  type ChatFollowedThreadPage,
  type ChatHistoryAnchor,
  type ChatMentionInput,
  type ChatMentionPayload,
  type ChatMessage,
  type ChatMessageCursor,
  type ChatMessagePage,
  type ChatNotificationLevel,
  type ChatPinPage,
  type ChatReactorPage,
  type ChatThreadCursor,
  type ChatThreadRepliesPage,
  type ChatThreadSummary,
  type ChatUnreadSummary,
  type ChatUpdateChannelInput,
} from "../../core/types/chat";
import { chatQueryKeys } from "../../query/keys";
import {
  appendToNewest,
  replaceMessage,
  tombstoneMessage,
  type ChatMessagesCache,
} from "./message-cache";
import {
  patchMessageInData,
  patchThreadSummary,
  toggleReaction,
  type ThreadCounts,
} from "./interaction-cache";
import { applyStoredReply, seedThreadCount } from "./thread-writes";
import { createConversationHooks } from "./create-conversation-hooks";

export interface ChatHookDeps {
  readonly api: ChatApiClient;
  readonly endpoints: ChatEndpoints;
}

const CHANNEL_STALE_MS = 30_000;
/** The badge refreshes on this interval until the realtime stream (S1.8) pushes it. */
const UNREAD_REFRESH_MS = 60_000;

function anchorKey(anchor: ChatHistoryAnchor): string {
  return anchor.kind === "latest" ? "latest" : `around-${anchor.sequence}`;
}

function newClientMessageId(): string {
  return crypto.randomUUID();
}

/** The pending sends of one channel (client-only projection; the server is authoritative). */
function readPending(
  queryClient: QueryClient,
  key: readonly unknown[],
): PendingMessage[] {
  return queryClient.getQueryData<PendingMessage[]>(key) ?? [];
}

/**
 * The Chat hooks of one host. Built once per runtime (`useMemo` on the client),
 * the same way the other feature packages expose their hooks.
 */
export function createChatHooks(deps: ChatHookDeps) {
  const service = createChatService(deps.api, deps.endpoints);

  // ─── Queries ───────────────────────────────────────────────────────────────

  /** The caller's channels in sidebar order (Chat.Channels.List). */
  function useChannels(workspaceId: string) {
    return useQuery({
      queryKey: chatQueryKeys.channels(workspaceId),
      queryFn: () => service.listChannels(workspaceId),
      staleTime: CHANNEL_STALE_MS,
      enabled: workspaceId.length > 0,
    });
  }

  /** The public channels the caller is not in (browse=true). */
  function useBrowseChannels(workspaceId: string, enabled: boolean) {
    return useQuery({
      queryKey: chatQueryKeys.browse(workspaceId),
      queryFn: () => service.browseChannels(workspaceId),
      staleTime: CHANNEL_STALE_MS,
      enabled: enabled && workspaceId.length > 0,
    });
  }

  /** One channel with its members (Chat.Channels.Get). */
  function useChannel(workspaceId: string, channelId: string | undefined) {
    return useQuery({
      queryKey: chatQueryKeys.channel(workspaceId, channelId ?? ""),
      queryFn: () => service.getChannel(workspaceId, channelId!),
      staleTime: CHANNEL_STALE_MS,
      enabled: Boolean(channelId) && workspaceId.length > 0,
    });
  }

  /**
   * The mention picker of a space (Chat.Mentions.ListMentionables): who a message can mention, whether the caller
   * may broadcast and the member count above which a broadcast needs confirmation.
   */
  function useMentionables(
    workspaceId: string,
    spaceId: string | undefined,
    enabled = true,
  ) {
    return useQuery({
      queryKey: chatQueryKeys.mentionables(workspaceId, spaceId ?? ""),
      queryFn: () => service.listMentionables(workspaceId, spaceId!),
      staleTime: CHANNEL_STALE_MS,
      enabled: enabled && Boolean(spaceId) && workspaceId.length > 0,
    });
  }

  /** Unread counts per channel and in total (Chat.ReadState.Unread), for the badges. */
  function useUnreadSummary(workspaceId: string) {
    return useQuery({
      queryKey: chatQueryKeys.unread(workspaceId),
      queryFn: () => service.getUnread(workspaceId),
      refetchInterval: UNREAD_REFRESH_MS,
      refetchOnWindowFocus: true,
      enabled: workspaceId.length > 0,
    });
  }

  /**
   * The history of one channel by cursor. The first page is the newest one, or
   * a window around the first unread message (D03). Older pages come from
   * `fetchPreviousPage` (scroll up); newer ones from `fetchNextPage`.
   */
  function useMessages(
    workspaceId: string,
    channelId: string,
    anchor: ChatHistoryAnchor,
  ) {
    return useInfiniteQuery<
      ChatMessagePage,
      Error,
      InfiniteData<ChatMessagePage, ChatMessageCursor>,
      QueryKey,
      ChatMessageCursor
    >({
      queryKey: [
        ...chatQueryKeys.messages(workspaceId, channelId),
        anchorKey(anchor),
      ],
      queryFn: ({ pageParam }) =>
        service.listMessages(
          workspaceId,
          channelId,
          pageParam,
          CHAT_LIMITS.pageSize,
        ),
      initialPageParam: (anchor.kind === "around"
        ? { around: anchor.sequence }
        : {}) as ChatMessageCursor,
      getPreviousPageParam: (
        first: ChatMessagePage,
      ): ChatMessageCursor | undefined =>
        first.hasOlder && first.items.length > 0
          ? { before: first.items[0]!.sequence }
          : undefined,
      getNextPageParam: (
        last: ChatMessagePage,
      ): ChatMessageCursor | undefined =>
        last.hasNewer && last.items.length > 0
          ? { after: last.items[last.items.length - 1]!.sequence }
          : undefined,
      enabled: workspaceId.length > 0 && channelId.length > 0,
      staleTime: CHANNEL_STALE_MS,
    });
  }

  /** The pending sends of a channel, read from the client-only projection. */
  function usePendingSends(workspaceId: string, channelId: string) {
    return useQuery<PendingMessage[]>({
      queryKey: chatQueryKeys.pending(workspaceId, channelId),
      queryFn: () => [],
      initialData: [],
      staleTime: Infinity,
      gcTime: Infinity,
      enabled: false,
    });
  }

  // ─── Message writes ────────────────────────────────────────────────────────

  /**
   * Optimistic send (UC-CHT-B01, B06). The message shows at once as a pending
   * entry; the server's copy joins the newest page when it is stored; a failed
   * send stays visible with a retry (the same clientMessageId, so the server
   * stores it once) or is refused.
   */
  function useSendMessage(
    workspaceId: string,
    channelId: string,
    viewerId: string,
  ) {
    const queryClient = useQueryClient();
    const pendingKey = chatQueryKeys.pending(workspaceId, channelId);
    const messagesKey = chatQueryKeys.messages(workspaceId, channelId);

    const mutation = useMutation({
      mutationFn: (input: {
        clientMessageId: string;
        content: string;
        replyToId?: string | null;
        mentions?: readonly ChatMentionInput[];
        confirmBroadcast?: boolean;
      }) =>
        service.sendMessage(workspaceId, channelId, {
          content: input.content,
          clientMessageId: input.clientMessageId,
          replyToId: input.replyToId ?? null,
          mentions: input.mentions,
          confirmBroadcast: input.confirmBroadcast,
        }),
      onMutate: async (input) => {
        await queryClient.cancelQueries({ queryKey: messagesKey });
        const current = readPending(queryClient, pendingKey);
        const exists = current.some(
          (entry) => entry.clientMessageId === input.clientMessageId,
        );
        const next: PendingMessage = {
          clientMessageId: input.clientMessageId,
          spaceId: channelId,
          content: input.content,
          authorId: viewerId,
          createdAt: new Date().toISOString(),
          status: "sending",
          failure: null,
          mentions: input.mentions,
          confirmBroadcast: input.confirmBroadcast,
        };
        queryClient.setQueryData<PendingMessage[]>(
          pendingKey,
          exists
            ? current.map((entry) =>
                entry.clientMessageId === input.clientMessageId
                  ? { ...entry, status: "sending", failure: null }
                  : entry,
              )
            : [...current, next],
        );
      },
      onSuccess: (message: ChatMessage, input) => {
        queryClient.setQueryData<PendingMessage[]>(
          pendingKey,
          readPending(queryClient, pendingKey).filter(
            (entry) => entry.clientMessageId !== input.clientMessageId,
          ),
        );
        queryClient.setQueriesData<ChatMessagesCache>(
          { queryKey: messagesKey },
          (cache) => (cache ? appendToNewest(cache, message) : cache),
        );
        // A conversation's list row shows its newest message: refresh it (a no-op when no list is mounted).
        void queryClient.invalidateQueries({
          queryKey: chatQueryKeys.conversations(workspaceId),
          refetchType: "active",
        });
      },
      onError: (error, input) => {
        const failure: ChatFailure = chatFailureOf(error);
        queryClient.setQueryData<PendingMessage[]>(
          pendingKey,
          readPending(queryClient, pendingKey).map((entry) =>
            entry.clientMessageId === input.clientMessageId
              ? {
                  ...entry,
                  status: "failed",
                  failure: failure.retryable ? "retryable" : "refused",
                }
              : entry,
          ),
        );
      },
    });

    /** Starts a send; returns the clientMessageId so a retry can reuse it. */
    function send(
      content: string,
      replyToId?: string | null,
      mentionPayload?: ChatMentionPayload,
    ): string {
      const clientMessageId = newClientMessageId();
      mutation.mutate({
        clientMessageId,
        content,
        replyToId,
        mentions: mentionPayload?.mentions,
        confirmBroadcast: mentionPayload?.confirmBroadcast,
      });
      return clientMessageId;
    }

    /** Sends again the failed entry with the same clientMessageId. */
    function retry(entry: PendingMessage): void {
      mutation.mutate({
        clientMessageId: entry.clientMessageId,
        content: entry.content,
        mentions: entry.mentions,
        confirmBroadcast: entry.confirmBroadcast,
      });
    }

    /** Drops a pending entry the user no longer wants to send. */
    function discard(clientMessageId: string): void {
      queryClient.setQueryData<PendingMessage[]>(
        pendingKey,
        readPending(queryClient, pendingKey).filter(
          (entry) => entry.clientMessageId !== clientMessageId,
        ),
      );
    }

    return { send, retry, discard, isPending: mutation.isPending };
  }

  /** Edit of the caller's own message; a stale version refetches the history (412). */
  function useEditMessage(workspaceId: string, channelId: string) {
    const queryClient = useQueryClient();
    const messagesKey = chatQueryKeys.messages(workspaceId, channelId);
    return useMutation({
      mutationFn: (input: {
        messageId: string;
        content: string;
        expectedVersion: number;
        mentions?: readonly ChatMentionInput[];
        confirmBroadcast?: boolean;
      }) =>
        service.editMessage(workspaceId, input.messageId, {
          content: input.content,
          expectedVersion: input.expectedVersion,
          mentions: input.mentions,
          confirmBroadcast: input.confirmBroadcast,
        }),
      onSuccess: (message: ChatMessage) => {
        queryClient.setQueriesData<ChatMessagesCache>(
          { queryKey: messagesKey },
          (cache) => (cache ? replaceMessage(cache, message) : cache),
        );
      },
      onError: (error) => {
        if (chatFailureOf(error).kind === "stale") {
          void queryClient.invalidateQueries({ queryKey: messagesKey });
        }
      },
    });
  }

  /** Delete of the caller's own message, which leaves a tombstone (412 refetches). */
  function useDeleteMessage(workspaceId: string, channelId: string) {
    const queryClient = useQueryClient();
    const messagesKey = chatQueryKeys.messages(workspaceId, channelId);
    return useMutation({
      mutationFn: (input: { messageId: string; expectedVersion: number }) =>
        service.deleteMessage(
          workspaceId,
          input.messageId,
          input.expectedVersion,
        ),
      onSuccess: (_result, input) => {
        const deletedAt = new Date().toISOString();
        queryClient.setQueriesData<ChatMessagesCache>(
          { queryKey: messagesKey },
          (cache) =>
            cache ? tombstoneMessage(cache, input.messageId, deletedAt) : cache,
        );
      },
      onError: (error) => {
        if (chatFailureOf(error).kind === "stale") {
          void queryClient.invalidateQueries({ queryKey: messagesKey });
        }
      },
    });
  }

  // ─── Read state ────────────────────────────────────────────────────────────

  /**
   * Mark read (UC-CHT-D02). The boundary only moves forward: a candidate at or
   * below the cached or the already-sent boundary is not sent at all.
   */
  function useMarkRead(workspaceId: string) {
    const queryClient = useQueryClient();
    const unreadKey = chatQueryKeys.unread(workspaceId);
    /** The highest boundary this hook has sent per channel (the cache may lag behind). */
    const sentRef = useRef(new Map<string, number>());

    const mutation = useMutation({
      mutationFn: (input: { spaceId: string; upToSequence: number }) =>
        service.markRead(workspaceId, input.spaceId, input.upToSequence),
      onMutate: async (input) => {
        await queryClient.cancelQueries({ queryKey: unreadKey });
        queryClient.setQueryData<ChatUnreadSummary>(unreadKey, (summary) =>
          summary
            ? {
                ...summary,
                spaces: summary.spaces.map((row) =>
                  row.spaceId === input.spaceId
                    ? unreadAfterRead(row, input.upToSequence)
                    : row,
                ),
              }
            : summary,
        );
      },
      onSuccess: (boundary) => {
        queryClient.setQueryData<ChatUnreadSummary>(unreadKey, (summary) =>
          summary
            ? {
                ...summary,
                spaces: summary.spaces.map((row) =>
                  row.spaceId === boundary.spaceId
                    ? {
                        ...row,
                        readSequence: nextReadBoundary(
                          row.readSequence,
                          boundary.readSequence,
                        ),
                      }
                    : row,
                ),
              }
            : summary,
        );
      },
      onError: () => {
        void queryClient.invalidateQueries({ queryKey: unreadKey });
      },
    });

    /** Reports the sequence the user has seen; a no-op when it would not move the boundary. */
    function markRead(spaceId: string, upToSequence: number): boolean {
      const summary = queryClient.getQueryData<ChatUnreadSummary>(unreadKey);
      const row = summary?.spaces.find((entry) => entry.spaceId === spaceId);
      const known = {
        cached: row?.readSequence ?? 0,
        sent: sentRef.current.get(spaceId) ?? 0,
      };
      if (!shouldReportRead(upToSequence, known)) return false;
      sentRef.current.set(spaceId, nextReadBoundary(known.sent, upToSequence));
      mutation.mutate({ spaceId, upToSequence });
      return true;
    }

    return { markRead, isPending: mutation.isPending };
  }

  /** Mark every channel as read (UC-CHT-D07). */
  function useMarkAllRead(workspaceId: string) {
    const queryClient = useQueryClient();
    return useMutation({
      mutationFn: () => service.markAllRead(workspaceId),
      onSuccess: (summary) => {
        queryClient.setQueryData(chatQueryKeys.unread(workspaceId), summary);
      },
    });
  }

  /** Mute or set the notification level of a channel (UC-CHT-D05, D06). */
  function useSetNotificationLevel(workspaceId: string) {
    const queryClient = useQueryClient();
    const unreadKey = chatQueryKeys.unread(workspaceId);
    return useMutation({
      mutationFn: (input: {
        spaceId: string;
        level: ChatNotificationLevel;
        mutedUntil?: string | null;
      }) =>
        service.setNotificationLevel(workspaceId, input.spaceId, {
          level: input.level,
          mutedUntil: input.mutedUntil ?? null,
        }),
      onMutate: async (input) => {
        await queryClient.cancelQueries({ queryKey: unreadKey });
        queryClient.setQueryData<ChatUnreadSummary>(unreadKey, (summary) =>
          summary
            ? {
                ...summary,
                spaces: summary.spaces.map((row) =>
                  row.spaceId === input.spaceId
                    ? {
                        ...row,
                        notificationLevel: input.level,
                        muted: input.level === "none",
                      }
                    : row,
                ),
              }
            : summary,
        );
      },
      onSuccess: () => {
        void queryClient.invalidateQueries({ queryKey: unreadKey });
        void queryClient.invalidateQueries({
          queryKey: chatQueryKeys.channels(workspaceId),
        });
      },
    });
  }

  // ─── Channel writes ────────────────────────────────────────────────────────

  function refreshChannels(queryClient: QueryClient, workspaceId: string) {
    void queryClient.invalidateQueries({
      queryKey: chatQueryKeys.channels(workspaceId),
    });
    void queryClient.invalidateQueries({
      queryKey: chatQueryKeys.browse(workspaceId),
    });
    void queryClient.invalidateQueries({
      queryKey: chatQueryKeys.unread(workspaceId),
    });
  }

  /** Create a channel. Each call names itself with a fresh Idempotency-Key. */
  function useCreateChannel(workspaceId: string) {
    const queryClient = useQueryClient();
    return useMutation({
      mutationFn: (input: ChatCreateChannelInput) =>
        service.createChannel(workspaceId, input, newClientMessageId()),
      onSuccess: () => refreshChannels(queryClient, workspaceId),
    });
  }

  function useUpdateChannel(workspaceId: string, channelId: string) {
    const queryClient = useQueryClient();
    return useMutation({
      mutationFn: (input: ChatUpdateChannelInput) =>
        service.updateChannel(workspaceId, channelId, input),
      onSuccess: (channel: ChatChannel) => {
        queryClient.setQueryData<ChatChannelDetail>(
          chatQueryKeys.channel(workspaceId, channelId),
          (detail) => (detail ? { ...detail, channel } : detail),
        );
        refreshChannels(queryClient, workspaceId);
      },
    });
  }

  function useArchiveChannel(workspaceId: string, channelId: string) {
    const queryClient = useQueryClient();
    return useMutation({
      mutationFn: (input: { archived: boolean; expectedVersion: number }) =>
        service.archiveChannel(workspaceId, channelId, input),
      onSuccess: () => {
        void queryClient.invalidateQueries({
          queryKey: chatQueryKeys.channel(workspaceId, channelId),
        });
        refreshChannels(queryClient, workspaceId);
      },
    });
  }

  /** Join a public channel; the id is passed per call (the sidebar joins one it is not in). */
  function useJoinChannel(workspaceId: string) {
    const queryClient = useQueryClient();
    return useMutation({
      mutationFn: (channelId: string) =>
        service.joinChannel(workspaceId, channelId),
      onSuccess: (_channel, channelId) => {
        void queryClient.invalidateQueries({
          queryKey: chatQueryKeys.channel(workspaceId, channelId),
        });
        refreshChannels(queryClient, workspaceId);
      },
    });
  }

  function useAddMembers(workspaceId: string, channelId: string) {
    const queryClient = useQueryClient();
    return useMutation({
      mutationFn: (userIds: readonly string[]) =>
        service.addMembers(workspaceId, channelId, userIds),
      onSuccess: () => {
        void queryClient.invalidateQueries({
          queryKey: chatQueryKeys.channel(workspaceId, channelId),
        });
        // The people just added are in the space now: the picker and its warnings follow.
        void queryClient.invalidateQueries({
          queryKey: chatQueryKeys.mentionables(workspaceId, channelId),
        });
      },
    });
  }

  // ─── Threads ───────────────────────────────────────────────────────────────

  /**
   * The replies of one thread, paged forward by thread sequence (the root
   * rides on the first page). Every loaded page refreshes the reply count of
   * the thread in the channel's client-side count projection.
   */
  function useThreadReplies(workspaceId: string, rootId: string | undefined) {
    const queryClient = useQueryClient();
    return useInfiniteQuery<
      ChatThreadRepliesPage,
      Error,
      InfiniteData<ChatThreadRepliesPage, ChatThreadCursor>,
      QueryKey,
      ChatThreadCursor
    >({
      queryKey: chatQueryKeys.thread(workspaceId, rootId ?? ""),
      queryFn: async ({ pageParam }) => {
        const page = await service.listThreadReplies(
          workspaceId,
          rootId!,
          pageParam,
          CHAT_LIMITS.pageSize,
        );
        seedThreadCount(queryClient, workspaceId, page.thread);
        return page;
      },
      initialPageParam: {} as ChatThreadCursor,
      getNextPageParam: (
        last: ChatThreadRepliesPage,
      ): ChatThreadCursor | undefined => {
        const tail = last.items[last.items.length - 1];
        return last.hasNewer && tail
          ? { after: tail.threadSequence ?? undefined }
          : undefined;
      },
      enabled: workspaceId.length > 0 && Boolean(rootId),
      staleTime: CHANNEL_STALE_MS,
    });
  }

  /**
   * The reply counts of the channel's threads (client-side projection). The
   * server's thread summary is read with the replies, and the realtime stream
   * keeps the count moving after that.
   */
  function useThreadCounts(workspaceId: string, channelId: string) {
    return useQuery<ThreadCounts>({
      queryKey: chatQueryKeys.threadCounts(workspaceId, channelId),
      queryFn: () => ({}),
      initialData: {},
      staleTime: Infinity,
      gcTime: Infinity,
      enabled: false,
    });
  }

  /**
   * Sends a reply in a thread. The stored reply joins the open thread, bumps
   * its root's count and, when the author also sent it to the channel, joins
   * the channel history. The realtime frame of the same reply is deduplicated by id.
   */
  function useReplyInThread(workspaceId: string, rootId: string) {
    const queryClient = useQueryClient();
    return useMutation({
      mutationFn: (input: {
        content: string;
        alsoSendToChannel: boolean;
        clientMessageId?: string;
        mentions?: readonly ChatMentionInput[];
        confirmBroadcast?: boolean;
      }) =>
        service.replyInThread(workspaceId, rootId, {
          content: input.content,
          clientMessageId: input.clientMessageId ?? newClientMessageId(),
          alsoSendToChannel: input.alsoSendToChannel,
          mentions: input.mentions,
          confirmBroadcast: input.confirmBroadcast,
        }),
      onSuccess: (reply: ChatMessage) => {
        applyStoredReply(queryClient, workspaceId, reply);
      },
    });
  }

  /** Follow, mute or unfollow a thread (the state comes back in the thread summary). */
  function useFollowThread(workspaceId: string) {
    const queryClient = useQueryClient();
    return useMutation({
      mutationFn: (input: {
        threadId: string;
        following: boolean;
        muted: boolean;
      }) =>
        service.setThreadFollow(workspaceId, input.threadId, {
          following: input.following,
          muted: input.muted,
        }),
      onSuccess: (state, input) => {
        queryClient.setQueryData(
          chatQueryKeys.thread(workspaceId, input.threadId),
          (data: unknown) =>
            patchThreadSummary(data, (summary: ChatThreadSummary) => ({
              ...summary,
              following: state.following,
              muted: state.muted,
              unreadReplies: state.following ? summary.unreadReplies : 0,
            })),
        );
        void queryClient.invalidateQueries({
          queryKey: chatQueryKeys.followedThreads(workspaceId),
        });
      },
    });
  }

  /** Moves the read pointer of a followed thread to its newest reply (the count left comes back). */
  function useMarkThreadRead(workspaceId: string) {
    const queryClient = useQueryClient();
    return useMutation({
      mutationFn: (input: { threadId: string; upToReplySequence: number }) =>
        service.markThreadRead(
          workspaceId,
          input.threadId,
          input.upToReplySequence,
        ),
      onSuccess: (result, input) => {
        queryClient.setQueryData(
          chatQueryKeys.thread(workspaceId, input.threadId),
          (data: unknown) =>
            patchThreadSummary(data, (summary: ChatThreadSummary) => ({
              ...summary,
              unreadReplies: result.unreadReplies,
            })),
        );
        void queryClient.invalidateQueries({
          queryKey: chatQueryKeys.followedThreads(workspaceId),
        });
      },
    });
  }

  /** The threads the caller follows, paged by the server's cursor (most recent reply first). */
  function useFollowedThreads(workspaceId: string, enabled: boolean) {
    const queryClient = useQueryClient();
    return useInfiniteQuery<
      ChatFollowedThreadPage,
      Error,
      InfiniteData<ChatFollowedThreadPage, string | null>,
      QueryKey,
      string | null
    >({
      queryKey: chatQueryKeys.followedThreads(workspaceId),
      queryFn: async ({ pageParam }) => {
        const page = await service.listFollowedThreads(
          workspaceId,
          pageParam,
          CHAT_LIMITS.pageSize,
        );
        for (const entry of page.items) {
          seedThreadCount(queryClient, workspaceId, entry.thread);
        }
        return page;
      },
      initialPageParam: null,
      getNextPageParam: (last: ChatFollowedThreadPage) =>
        last.nextCursor ?? undefined,
      enabled: enabled && workspaceId.length > 0,
      staleTime: CHANNEL_STALE_MS,
    });
  }

  // ─── Pins ──────────────────────────────────────────────────────────────────

  /**
   * The pins of one channel, newest first. A channel holds at most 50 pins, so
   * the first page is the whole set the header and the toolbar need.
   */
  function usePins(workspaceId: string, channelId: string) {
    return useInfiniteQuery<
      ChatPinPage,
      Error,
      InfiniteData<ChatPinPage, string | null>,
      QueryKey,
      string | null
    >({
      queryKey: chatQueryKeys.pins(workspaceId, channelId),
      queryFn: ({ pageParam }) =>
        service.listPins(
          workspaceId,
          channelId,
          pageParam,
          CHAT_LIMITS.pageSize,
        ),
      initialPageParam: null,
      getNextPageParam: (last: ChatPinPage) => last.nextCursor ?? undefined,
      enabled: workspaceId.length > 0 && channelId.length > 0,
      staleTime: CHANNEL_STALE_MS,
    });
  }

  /** Pins or unpins a message; the pin list of the channel is read again after the change. */
  function usePinMessage(workspaceId: string, channelId: string) {
    const queryClient = useQueryClient();
    return useMutation({
      mutationFn: async (input: { messageId: string; pinned: boolean }) => {
        if (input.pinned)
          await service.pinMessage(workspaceId, input.messageId);
        else await service.unpinMessage(workspaceId, input.messageId);
      },
      onSuccess: () => {
        void queryClient.invalidateQueries({
          queryKey: chatQueryKeys.pins(workspaceId, channelId),
        });
      },
    });
  }

  // ─── Reactions ─────────────────────────────────────────────────────────────

  /** Who reacted to a message with one emoji (or any emoji), earliest first. */
  function useReactors(
    workspaceId: string,
    messageId: string | undefined,
    kind: string | undefined,
    enabled: boolean,
  ) {
    return useInfiniteQuery<
      ChatReactorPage,
      Error,
      InfiniteData<ChatReactorPage, string | null>,
      QueryKey,
      string | null
    >({
      queryKey: [
        ...chatQueryKeys.all(workspaceId),
        "reactors",
        messageId ?? "",
        kind ?? "",
      ],
      queryFn: ({ pageParam }) =>
        service.listReactors(workspaceId, messageId!, {
          kind,
          cursor: pageParam,
          limit: CHAT_LIMITS.pageSize,
        }),
      initialPageParam: null,
      getNextPageParam: (last: ChatReactorPage) => last.nextCursor ?? undefined,
      enabled: enabled && workspaceId.length > 0 && Boolean(messageId),
      staleTime: CHANNEL_STALE_MS,
    });
  }

  /**
   * Adds or removes the caller's palette reaction. The summary of the message
   * changes at once in every loaded cache (channel, thread, pins) and is put
   * back when the server refuses it; the server's answer then replaces the guess.
   */
  function useReaction(workspaceId: string) {
    const queryClient = useQueryClient();
    const scope = chatQueryKeys.all(workspaceId);
    return useMutation({
      mutationFn: (input: { messageId: string; kind: string; add: boolean }) =>
        input.add
          ? service.addReaction(workspaceId, input.messageId, input.kind)
          : service.removeReaction(workspaceId, input.messageId, input.kind),
      onMutate: async (input) => {
        await queryClient.cancelQueries({ queryKey: scope });
        const snapshot = queryClient.getQueriesData({ queryKey: scope });
        queryClient.setQueriesData({ queryKey: scope }, (data: unknown) =>
          patchMessageInData(data, input.messageId, (message) => ({
            ...message,
            reactions: toggleReaction(
              message.reactions ?? [],
              input.kind,
              input.add,
            ),
          })),
        );
        return { snapshot };
      },
      onError: (_error, _input, context) => {
        for (const [key, data] of context?.snapshot ?? []) {
          queryClient.setQueryData(key, data);
        }
      },
      onSuccess: (result) => {
        queryClient.setQueriesData({ queryKey: scope }, (data: unknown) =>
          patchMessageInData(data, result.messageId, (message) => ({
            ...message,
            reactions: result.reactions,
          })),
        );
      },
    });
  }

  return {
    service,
    ...createConversationHooks(service),
    useChannels,
    useBrowseChannels,
    useChannel,
    useUnreadSummary,
    useMessages,
    usePendingSends,
    useMentionables,
    useSendMessage,
    useEditMessage,
    useDeleteMessage,
    useMarkRead,
    useMarkAllRead,
    useSetNotificationLevel,
    useCreateChannel,
    useUpdateChannel,
    useArchiveChannel,
    useJoinChannel,
    useAddMembers,
    useThreadReplies,
    useThreadCounts,
    useReplyInThread,
    useFollowThread,
    useMarkThreadRead,
    useFollowedThreads,
    usePins,
    usePinMessage,
    useReactors,
    useReaction,
  };
}

export type ChatHooks = ReturnType<typeof createChatHooks>;

/** The loaded message pages as one ordered list (oldest first, once per id). */
export function flattenPages(
  data: InfiniteData<ChatMessagePage, ChatMessageCursor> | undefined,
): ChatMessage[] {
  return data ? mergeMessages(data.pages) : [];
}
