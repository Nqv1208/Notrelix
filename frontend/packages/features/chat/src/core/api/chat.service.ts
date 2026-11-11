import type {
  ChatAddMembersResult,
  ChatChannel,
  ChatChannelDetail,
  ChatCreateChannelInput,
  ChatMessage,
  ChatMessagePage,
  ChatMessageCursor,
  ChatNotificationLevel,
  ChatNotificationState,
  ChatReadBoundary,
  ChatUnreadSummary,
  ChatUpdateChannelInput,
} from "../types/chat";

/** The transport the host passes in: the generated client's `api` (`createNotrelixClient`). */
export interface ChatRequestOptions {
  readonly idempotencyKey?: string;
  readonly signal?: AbortSignal;
}

export interface ChatApiClient {
  get<T>(url: string, options?: ChatRequestOptions): Promise<T>;
  post<T>(
    url: string,
    body?: unknown,
    options?: ChatRequestOptions,
  ): Promise<T>;
  put<T>(url: string, body?: unknown, options?: ChatRequestOptions): Promise<T>;
  patch<T>(
    url: string,
    body?: unknown,
    options?: ChatRequestOptions,
  ): Promise<T>;
  delete<T>(url: string, options?: ChatRequestOptions): Promise<T>;
}

/** The Chat endpoints of `@notrelix/contracts` (`endpoints.chat`). */
export interface ChatEndpoints {
  chat: {
    channels: (workspaceId: string) => string;
    channel: (workspaceId: string, channelId: string) => string;
    channelArchive: (workspaceId: string, channelId: string) => string;
    channelMembers: (workspaceId: string, channelId: string) => string;
    channelJoin: (workspaceId: string, channelId: string) => string;
    channelMember: (
      workspaceId: string,
      channelId: string,
      userId: string,
    ) => string;
    messages: (workspaceId: string, spaceId: string) => string;
    message: (workspaceId: string, messageId: string) => string;
    unread: (workspaceId: string) => string;
    markRead: (workspaceId: string, spaceId: string) => string;
    markAllRead: (workspaceId: string) => string;
    notificationLevel: (workspaceId: string, spaceId: string) => string;
  };
}

/** Appends a query string with the defined values only. */
function withQuery(
  url: string,
  params: Record<string, string | number | boolean | undefined>,
): string {
  const search = new URLSearchParams();
  for (const [name, value] of Object.entries(params)) {
    if (value !== undefined) search.set(name, String(value));
  }
  const query = search.toString();
  return query ? `${url}?${query}` : url;
}

/**
 * Chat REST operations. Each method is one Chat.* operation of the generated
 * contract; the server stays authoritative and the cache is only a view of it.
 */
export function createChatService(
  api: ChatApiClient,
  endpoints: ChatEndpoints,
) {
  const paths = endpoints.chat;
  return {
    /** `Chat.Channels.List`: the caller's own channels in sidebar order. */
    listChannels(workspaceId: string): Promise<ChatChannel[]> {
      return api.get<ChatChannel[]>(paths.channels(workspaceId));
    },

    /** `Chat.Channels.List` with browse=true: the public channels the caller is not in. */
    browseChannels(workspaceId: string): Promise<ChatChannel[]> {
      return api.get<ChatChannel[]>(
        withQuery(paths.channels(workspaceId), { browse: true }),
      );
    },

    /** `Chat.Channels.Get`: a private channel the caller is not in is NotFound. */
    getChannel(
      workspaceId: string,
      channelId: string,
    ): Promise<ChatChannelDetail> {
      return api.get<ChatChannelDetail>(paths.channel(workspaceId, channelId));
    },

    /** `Chat.Channels.Create`: the Idempotency-Key is required; a retry replays the stored channel. */
    createChannel(
      workspaceId: string,
      input: ChatCreateChannelInput,
      idempotencyKey: string,
    ): Promise<ChatChannel> {
      return api.post<ChatChannel>(paths.channels(workspaceId), input, {
        idempotencyKey,
      });
    },

    /** `Chat.Channels.Update`: a stale expectedVersion is 412. */
    updateChannel(
      workspaceId: string,
      channelId: string,
      input: ChatUpdateChannelInput,
    ): Promise<ChatChannel> {
      return api.patch<ChatChannel>(
        paths.channel(workspaceId, channelId),
        input,
      );
    },

    /** `Chat.Channels.Archive`: an archived channel is read-only. */
    archiveChannel(
      workspaceId: string,
      channelId: string,
      input: { archived: boolean; expectedVersion: number },
    ): Promise<ChatChannel> {
      return api.post<ChatChannel>(
        paths.channelArchive(workspaceId, channelId),
        input,
      );
    },

    /** `Chat.Channels.Delete`: the expected version travels in the query string. */
    deleteChannel(
      workspaceId: string,
      channelId: string,
      expectedVersion: number,
    ): Promise<void> {
      return api.delete<void>(
        withQuery(paths.channel(workspaceId, channelId), { expectedVersion }),
      );
    },

    /** `Chat.Members.Join`: joining again changes nothing. */
    joinChannel(workspaceId: string, channelId: string): Promise<ChatChannel> {
      return api.post<ChatChannel>(paths.channelJoin(workspaceId, channelId));
    },

    /** `Chat.Members.Add`: a person outside the Workspace comes back under skipped. */
    addMembers(
      workspaceId: string,
      channelId: string,
      userIds: readonly string[],
    ): Promise<ChatAddMembersResult> {
      return api.post<ChatAddMembersResult>(
        paths.channelMembers(workspaceId, channelId),
        { userIds: [...userIds] },
      );
    },

    /** `Chat.Members.Remove`: leave (own id) or remove a member. */
    removeMember(
      workspaceId: string,
      channelId: string,
      userId: string,
    ): Promise<void> {
      return api.delete<void>(
        paths.channelMember(workspaceId, channelId, userId),
      );
    },

    /** `Chat.Messages.List`: at most one of before, after and around. */
    listMessages(
      workspaceId: string,
      spaceId: string,
      cursor: ChatMessageCursor,
      limit: number,
    ): Promise<ChatMessagePage> {
      return api.get<ChatMessagePage>(
        withQuery(paths.messages(workspaceId, spaceId), {
          before: cursor.before,
          after: cursor.after,
          around: cursor.around,
          limit,
        }),
      );
    },

    /**
     * `Chat.Messages.Send`. The clientMessageId is the sender's key for the
     * message and doubles as the Idempotency-Key, so a retry replays the stored
     * message (200) instead of storing a second one.
     */
    sendMessage(
      workspaceId: string,
      spaceId: string,
      input: {
        content: string;
        clientMessageId: string;
        replyToId?: string | null;
      },
    ): Promise<ChatMessage> {
      return api.post<ChatMessage>(
        paths.messages(workspaceId, spaceId),
        input,
        { idempotencyKey: input.clientMessageId },
      );
    },

    /** `Chat.Messages.Edit`: only the author; a stale expectedVersion is 412. */
    editMessage(
      workspaceId: string,
      messageId: string,
      input: { content: string; expectedVersion: number },
    ): Promise<ChatMessage> {
      return api.patch<ChatMessage>(
        paths.message(workspaceId, messageId),
        input,
      );
    },

    /** `Chat.Messages.Delete`: leaves a tombstone; the expected version is a query parameter. */
    deleteMessage(
      workspaceId: string,
      messageId: string,
      expectedVersion: number,
    ): Promise<void> {
      return api.delete<void>(
        withQuery(paths.message(workspaceId, messageId), { expectedVersion }),
      );
    },

    /** `Chat.ReadState.Unread`. */
    getUnread(workspaceId: string): Promise<ChatUnreadSummary> {
      return api.get<ChatUnreadSummary>(paths.unread(workspaceId));
    },

    /** `Chat.ReadState.MarkRead`: idempotent; a sequence below the boundary changes nothing. */
    markRead(
      workspaceId: string,
      spaceId: string,
      upToSequence: number,
    ): Promise<ChatReadBoundary> {
      return api.put<ChatReadBoundary>(paths.markRead(workspaceId, spaceId), {
        upToSequence,
      });
    },

    /** `Chat.ReadState.MarkAllRead`: returns the unread summary after the change. */
    markAllRead(workspaceId: string): Promise<ChatUnreadSummary> {
      return api.post<ChatUnreadSummary>(paths.markAllRead(workspaceId));
    },

    /** `Chat.ReadState.SetNotificationLevel`: a timed mute is level none with mutedUntil. */
    setNotificationLevel(
      workspaceId: string,
      spaceId: string,
      input: { level: ChatNotificationLevel; mutedUntil?: string | null },
    ): Promise<ChatNotificationState> {
      return api.put<ChatNotificationState>(
        paths.notificationLevel(workspaceId, spaceId),
        input,
      );
    },
  };
}

export type ChatService = ReturnType<typeof createChatService>;
