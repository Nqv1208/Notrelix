import type {
  ChatBlockedPerson,
  ChatChannelGroup,
  ChatAddMembersResult,
  ChatAttachment,
  ChatChannel,
  ChatChannelDetail,
  ChatConversation,
  ChatLinkPreview,
  ChatLinkRef,
  ChatLinkKind,
  ChatLinkChannelInput,
  ChatLinkedChannel,
  ChatConversationVisibility,
  ChatPersonBlock,
  ChatStartConversationInput,
  ChatStartConversationResult,
  ChatCreateChannelInput,
  ChatFollowedThreadPage,
  ChatMentionables,
  ChatMentionPayload,
  ChatMessage,
  ChatMessagePage,
  ChatMessageCursor,
  ChatMessageReactions,
  ChatNotificationLevel,
  ChatNotificationState,
  ChatChannelPermissions,
  ChatExport,
  ChatRetentionMode,
  ChatRetentionPolicy,
  ChatPermissionEntry,
  ChatPin,
  ChatSearchFilters,
  ChatSearchPage,
  ChatPinPage,
  ChatReactorPage,
  ChatReadBoundary,
  ChatThreadCursor,
  ChatThreadRepliesPage,
  ChatUnreadSummary,
  ChatUpdateChannelInput,
} from "../types/chat";
import { voiceFactsOf } from "../model/voice-model";

/** The transport the host passes in: the generated client's `api` (`createNotrelixClient`). */
export interface ChatRequestOptions {
  readonly idempotencyKey?: string;
  readonly signal?: AbortSignal;
}

/** What a multipart upload reports while it runs. */
export interface ChatUploadProgress {
  readonly loaded: number;
  readonly total: number;
}

export interface ChatUploadOptions {
  /** One per user action; the same key only to retry that action. */
  readonly idempotencyKey: string;
  readonly signal?: AbortSignal;
  readonly onProgress?: (progress: ChatUploadProgress) => void;
}

export interface ChatApiClient {
  /**
   * The generated client's multipart upload and authenticated binary read (the same transport Documents
   * and Board attachments use). A host that does not give them cannot attach or download files.
   */
  upload?<T>(
    url: string,
    form: FormData,
    options: ChatUploadOptions,
  ): Promise<T>;
  getBlob?(url: string): Promise<Blob>;
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
    spaceMentionables: (workspaceId: string, spaceId: string) => string;
    message: (workspaceId: string, messageId: string) => string;
    unread: (workspaceId: string) => string;
    markRead: (workspaceId: string, spaceId: string) => string;
    markAllRead: (workspaceId: string) => string;
    notificationLevel: (workspaceId: string, spaceId: string) => string;
    threadReplies: (workspaceId: string, rootId: string) => string;
    threadFollow: (workspaceId: string, threadId: string) => string;
    threadRead: (workspaceId: string, threadId: string) => string;
    followedThreads: (workspaceId: string) => string;
    messageAttachments: (workspaceId: string, messageId: string) => string;
    messageAttachment: (
      workspaceId: string,
      messageId: string,
      attachmentId: string,
    ) => string;
    messageAttachmentContent: (
      workspaceId: string,
      messageId: string,
      attachmentId: string,
    ) => string;
    linkResolve: (workspaceId: string) => string;
    channelLink: (workspaceId: string, channelId: string) => string;
    linkedChannels: (workspaceId: string) => string;
    messagePin: (workspaceId: string, messageId: string) => string;
    spacePins: (workspaceId: string, spaceId: string) => string;
    channelPermissions: (workspaceId: string, channelId: string) => string;
    messageModeration: (workspaceId: string, messageId: string) => string;
    retention: (workspaceId: string) => string;
    search: (workspaceId: string) => string;
    exports: (workspaceId: string) => string;
    chatExport: (workspaceId: string, exportId: string) => string;
    chatExportContent: (workspaceId: string, exportId: string) => string;
    messageReaction: (
      workspaceId: string,
      messageId: string,
      kind: string,
    ) => string;
    messageReactors: (workspaceId: string, messageId: string) => string;
    conversations: (workspaceId: string) => string;
    conversationParticipants: (
      workspaceId: string,
      conversationId: string,
    ) => string;
    conversationLeave: (workspaceId: string, conversationId: string) => string;
    conversationVisibility: (
      workspaceId: string,
      conversationId: string,
    ) => string;
    blocks: (workspaceId: string) => string;
    channelGroups: (workspaceId: string) => string;
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

    /** `Chat.ChannelGroups.List`: the sidebar categories with their names, in sidebar order. */
    listChannelGroups(workspaceId: string): Promise<ChatChannelGroup[]> {
      return api.get<ChatChannelGroup[]>(paths.channelGroups(workspaceId));
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
        /** A voice message carries no text; its audio is uploaded to it next (UC-CHT-I04). */
        voiceMessage?: boolean;
      } & ChatMentionPayload,
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
      input: {
        content: string;
        expectedVersion: number;
      } & ChatMentionPayload,
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

    /**
     * `Chat.Mentions.ListMentionables`: the people a message in the space can mention (inSpace false: not notified),
     * whether the caller may broadcast and the member count above which a broadcast needs confirmation.
     */
    listMentionables(
      workspaceId: string,
      spaceId: string,
    ): Promise<ChatMentionables> {
      return api.get<ChatMentionables>(
        paths.spaceMentionables(workspaceId, spaceId),
      );
    },

    /**
     * `Chat.Links.Resolve`: the preview of a link to a Board item or Page, with the caller's own access. The title is
     * there only when the caller can view the resource now; otherwise `accessible` is false. Never changes the resource.
     */
    resolveLink(
      workspaceId: string,
      ref: ChatLinkRef,
      options?: ChatRequestOptions,
    ): Promise<ChatLinkPreview> {
      return api.get<ChatLinkPreview>(
        withQuery(
          paths.linkResolve(workspaceId),
          "url" in ref
            ? { url: ref.url }
            : { kind: ref.kind, resourceId: ref.resourceId },
        ),
        options,
      );
    },

    /**
     * `Chat.Channels.Link`: links a channel to a Board or Page the manager can view (403 otherwise; the resource is never
     * changed). A stale expectedVersion is 412.
     */
    linkChannel(
      workspaceId: string,
      channelId: string,
      input: ChatLinkChannelInput,
    ): Promise<ChatChannel> {
      return api.put<ChatChannel>(paths.channelLink(workspaceId, channelId), {
        kind: input.kind,
        resourceId: input.resourceId,
        expectedVersion: input.expectedVersion,
      });
    },

    /** `Chat.Channels.Unlink`: the version travels in the query string. */
    unlinkChannel(
      workspaceId: string,
      channelId: string,
      expectedVersion: number,
    ): Promise<ChatChannel> {
      return api.delete<ChatChannel>(
        withQuery(paths.channelLink(workspaceId, channelId), {
          expectedVersion,
        }),
      );
    },

    /** `Chat.Channels.ListLinked`: the channels the caller can see that are linked to one resource. */
    listLinkedChannels(
      workspaceId: string,
      kind: ChatLinkKind,
      resourceId: string,
    ): Promise<ChatLinkedChannel[]> {
      return api.get<ChatLinkedChannel[]>(
        withQuery(paths.linkedChannels(workspaceId), { kind, id: resourceId }),
      );
    },

    /** `Chat.Attachments.List`: the files of a message, oldest first. */
    listAttachments(
      workspaceId: string,
      messageId: string,
    ): Promise<ChatAttachment[]> {
      return api.get<ChatAttachment[]>(
        paths.messageAttachments(workspaceId, messageId),
      );
    },

    /**
     * `Chat.Attachments.Upload`: multipart part `file`, streamed, for the caller's own message (sent first).
     * The Idempotency-Key belongs to one file of one user action; a retry reuses it. 415 blocked type, 413
     * too large, 409 attachment.limit_exceeded.
     */
    uploadAttachment(
      workspaceId: string,
      messageId: string,
      file: File,
      options: ChatUploadOptions,
    ): Promise<ChatAttachment> {
      if (!api.upload) {
        return Promise.reject(new Error("The API client cannot upload files."));
      }
      const form = new FormData();
      // The voice fields go before the file part: the server reads them only ahead of it (UC-CHT-I04).
      const voice = voiceFactsOf(file);
      if (voice) {
        form.append("voice", "true");
        form.append("durationMs", String(voice.durationMs));
        form.append("waveform", voice.waveform.join(","));
      }
      form.append("file", file, file.name);
      return api.upload<ChatAttachment>(
        paths.messageAttachments(workspaceId, messageId),
        form,
        options,
      );
    },

    /**
     * `Chat.Attachments.GetContent`: the bytes, read with the session. The route is built from the ids (the
     * `downloadPath` of the view carries the API prefix the client already adds).
     */
    getAttachmentContent(
      workspaceId: string,
      messageId: string,
      attachmentId: string,
    ): Promise<Blob> {
      if (!api.getBlob) {
        return Promise.reject(new Error("The API client cannot read files."));
      }
      return api.getBlob(
        paths.messageAttachmentContent(workspaceId, messageId, attachmentId),
      );
    },

    /** `Chat.Attachments.Delete`: the author removes a file of their own message. */
    deleteAttachment(
      workspaceId: string,
      messageId: string,
      attachmentId: string,
    ): Promise<void> {
      return api.delete<void>(
        paths.messageAttachment(workspaceId, messageId, attachmentId),
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

    /**
     * `Chat.Threads.ListReplies`: the root, its summary and a page of replies. `after` is the thread
     * sequence of the last reply already held (forward paging from the first reply).
     */
    listThreadReplies(
      workspaceId: string,
      rootId: string,
      cursor: ChatThreadCursor,
      limit: number,
    ): Promise<ChatThreadRepliesPage> {
      return api.get<ChatThreadRepliesPage>(
        withQuery(paths.threadReplies(workspaceId, rootId), {
          after: cursor.after,
          limit,
        }),
      );
    },

    /**
     * `Chat.Threads.Reply`. The clientMessageId is the sender's key and the Idempotency-Key, so a retry
     * replays the stored reply (200) instead of storing a second one.
     */
    replyInThread(
      workspaceId: string,
      rootId: string,
      input: {
        content: string;
        clientMessageId: string;
        alsoSendToChannel: boolean;
      } & ChatMentionPayload,
    ): Promise<ChatMessage> {
      return api.post<ChatMessage>(
        paths.threadReplies(workspaceId, rootId),
        input,
        { idempotencyKey: input.clientMessageId },
      );
    },

    /** `Chat.Threads.Follow`: following true follows (muted true without notifications); false unfollows. */
    setThreadFollow(
      workspaceId: string,
      threadId: string,
      input: { following: boolean; muted: boolean },
    ): Promise<{ rootMessageId: string; following: boolean; muted: boolean }> {
      return api.put<{
        rootMessageId: string;
        following: boolean;
        muted: boolean;
      }>(paths.threadFollow(workspaceId, threadId), input);
    },

    /** `Chat.Threads.MarkRead`: moves the read pointer forward; returns the unread replies left. */
    markThreadRead(
      workspaceId: string,
      threadId: string,
      upToReplySequence: number,
    ): Promise<{ rootMessageId: string; unreadReplies: number }> {
      return api.put<{ rootMessageId: string; unreadReplies: number }>(
        paths.threadRead(workspaceId, threadId),
        { upToReplySequence },
      );
    },

    /** `Chat.Threads.Mine`: the threads the caller follows, most recent reply first. */
    listFollowedThreads(
      workspaceId: string,
      cursor: string | null,
      limit: number,
    ): Promise<ChatFollowedThreadPage> {
      return api.get<ChatFollowedThreadPage>(
        withQuery(paths.followedThreads(workspaceId), {
          cursor: cursor ?? undefined,
          limit,
        }),
      );
    },

    /** `Chat.Permissions.Get`: the overrides of a channel; any member who can see it reads them. */
    getChannelPermissions(
      workspaceId: string,
      channelId: string,
    ): Promise<ChatChannelPermissions> {
      return api.get<ChatChannelPermissions>(
        paths.channelPermissions(workspaceId, channelId),
      );
    },

    /** `Chat.Permissions.Set`: the set is the whole truth; a stale expectedVersion is 412. */
    setChannelPermissions(
      workspaceId: string,
      channelId: string,
      input: {
        entries: readonly ChatPermissionEntry[];
        expectedVersion: number;
      },
    ): Promise<ChatChannelPermissions> {
      return api.put<ChatChannelPermissions>(
        paths.channelPermissions(workspaceId, channelId),
        { entries: [...input.entries], expectedVersion: input.expectedVersion },
      );
    },

    /** `Chat.Permissions.Delete`: every role back on its default; the version travels in the query string. */
    clearChannelPermissions(
      workspaceId: string,
      channelId: string,
      expectedVersion: number,
    ): Promise<ChatChannelPermissions> {
      return api.delete<ChatChannelPermissions>(
        withQuery(paths.channelPermissions(workspaceId, channelId), {
          expectedVersion,
        }),
      );
    },

    /** `Chat.Retention.Get`: the Workspace rule; any member reads it, `canChange` tells who may set it. */
    getRetention(workspaceId: string): Promise<ChatRetentionPolicy> {
      return api.get<ChatRetentionPolicy>(paths.retention(workspaceId));
    },

    /** `Chat.Retention.Set`: Owner only; a stale expectedVersion is 412. */
    setRetention(
      workspaceId: string,
      input: {
        retentionDays: number | null;
        mode: ChatRetentionMode;
        expectedVersion: number;
      },
    ): Promise<ChatRetentionPolicy> {
      return api.put<ChatRetentionPolicy>(paths.retention(workspaceId), input);
    },

    /**
     * `Chat.Search.Query`: the messages of the spaces the caller can read now, newest first. The server never reveals a space the caller
     * cannot read; a filter on one gives an empty page.
     */
    searchMessages(
      workspaceId: string,
      filters: ChatSearchFilters,
      cursor: string | null,
      limit: number,
      signal?: AbortSignal,
    ): Promise<ChatSearchPage> {
      return api.get<ChatSearchPage>(
        withQuery(paths.search(workspaceId), {
          q: filters.q,
          spaceId: filters.spaceId,
          authorId: filters.authorId,
          from: filters.from,
          to: filters.to,
          hasAttachment: filters.hasAttachment ? true : undefined,
          mentionsMe: filters.mentionsMe ? true : undefined,
          cursor: cursor ?? undefined,
          limit,
        }),
        signal ? { signal } : undefined,
      );
    },

    /** `Chat.Exports.Create`: Owner or Admin asks for the export of one person's chat data (202, queued). */
    createExport(
      workspaceId: string,
      input: {
        subjectUserId: string;
        rangeFrom?: string | null;
        rangeTo?: string | null;
      },
    ): Promise<ChatExport> {
      return api.post<ChatExport>(paths.exports(workspaceId), input);
    },

    /** `Chat.Exports.Get`: the status of an export the caller asked for. */
    getExport(workspaceId: string, exportId: string): Promise<ChatExport> {
      return api.get<ChatExport>(paths.chatExport(workspaceId, exportId));
    },

    /** `Chat.Exports.GetContent`: the file bytes, read with the session. */
    getExportContent(workspaceId: string, exportId: string): Promise<Blob> {
      if (!api.getBlob) {
        return Promise.reject(new Error("The API client cannot read files."));
      }
      return api.getBlob(paths.chatExportContent(workspaceId, exportId));
    },

    /** `Chat.Messages.Moderate`: removes another person's message; the reason goes to the Audit record. */
    moderateDeleteMessage(
      workspaceId: string,
      messageId: string,
      input: { reason: string; expectedVersion: number },
    ): Promise<void> {
      return api.post<void>(
        paths.messageModeration(workspaceId, messageId),
        input,
      );
    },

    /** `Chat.Pins.Pin`: idempotent; a message already pinned is answered with its pin. */
    pinMessage(workspaceId: string, messageId: string): Promise<ChatPin> {
      return api.put<ChatPin>(paths.messagePin(workspaceId, messageId));
    },

    /** `Chat.Pins.Unpin`: idempotent (204 for a message that is not pinned). */
    unpinMessage(workspaceId: string, messageId: string): Promise<void> {
      return api.delete<void>(paths.messagePin(workspaceId, messageId));
    },

    /** `Chat.Pins.List`: the newest pin first. */
    listPins(
      workspaceId: string,
      spaceId: string,
      cursor: string | null,
      limit: number,
    ): Promise<ChatPinPage> {
      return api.get<ChatPinPage>(
        withQuery(paths.spacePins(workspaceId, spaceId), {
          cursor: cursor ?? undefined,
          limit,
        }),
      );
    },

    /** `Chat.Reactions.Add`: a palette emoji; idempotent (the summary answers either way). */
    addReaction(
      workspaceId: string,
      messageId: string,
      kind: string,
    ): Promise<ChatMessageReactions> {
      return api.put<ChatMessageReactions>(
        paths.messageReaction(workspaceId, messageId, kind),
      );
    },

    /** `Chat.Reactions.Remove`: idempotent; returns the summary after the change. */
    removeReaction(
      workspaceId: string,
      messageId: string,
      kind: string,
    ): Promise<ChatMessageReactions> {
      return api.delete<ChatMessageReactions>(
        paths.messageReaction(workspaceId, messageId, kind),
      );
    },

    /** `Chat.Reactions.ListReactors`: earliest reaction first; `kind` narrows to one emoji. */
    listReactors(
      workspaceId: string,
      messageId: string,
      input: { kind?: string; cursor?: string | null; limit: number },
    ): Promise<ChatReactorPage> {
      return api.get<ChatReactorPage>(
        withQuery(paths.messageReactors(workspaceId, messageId), {
          kind: input.kind,
          cursor: input.cursor ?? undefined,
          limit: input.limit,
        }),
      );
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

    /** `Chat.Conversations.List`: newest activity first; the ones the caller hid are left out. */
    listConversations(workspaceId: string): Promise<ChatConversation[]> {
      return api.get<ChatConversation[]>(paths.conversations(workspaceId));
    },

    /**
     * `Chat.Conversations.Start`: a direct conversation is one per pair (starting it again returns it and
     * shows it again in the caller's list); a group needs two other people or more.
     */
    startConversation(
      workspaceId: string,
      input: ChatStartConversationInput,
      idempotencyKey: string,
    ): Promise<ChatStartConversationResult> {
      const body =
        input.kind === "direct"
          ? { kind: "direct", userId: input.userId }
          : {
              kind: "group",
              userIds: [...input.userIds],
              name: input.name ?? null,
            };
      return api.post<ChatStartConversationResult>(
        paths.conversations(workspaceId),
        body,
        { idempotencyKey },
      );
    },

    /** `Chat.Conversations.AddParticipants`: a group only; a person outside the Workspace comes back under skipped. */
    addConversationParticipants(
      workspaceId: string,
      conversationId: string,
      userIds: readonly string[],
    ): Promise<ChatAddMembersResult> {
      return api.post<ChatAddMembersResult>(
        paths.conversationParticipants(workspaceId, conversationId),
        { userIds: [...userIds] },
      );
    },

    /** `Chat.Conversations.Leave`: a group only; a direct conversation cannot be left (hide it instead). */
    leaveConversation(
      workspaceId: string,
      conversationId: string,
    ): Promise<void> {
      return api.delete<void>(
        paths.conversationLeave(workspaceId, conversationId),
      );
    },

    /** `Chat.Conversations.SetVisibility`: only the caller's own list changes; idempotent. */
    setConversationVisibility(
      workspaceId: string,
      conversationId: string,
      hidden: boolean,
    ): Promise<ChatConversationVisibility> {
      return api.put<ChatConversationVisibility>(
        paths.conversationVisibility(workspaceId, conversationId),
        { hidden },
      );
    },

    /** `Chat.Blocks.List`: the people the caller blocked, newest first (the caller's own list only). */
    listPersonBlocks(workspaceId: string): Promise<ChatBlockedPerson[]> {
      return api.get<ChatBlockedPerson[]>(paths.blocks(workspaceId));
    },

    /** `Chat.Blocks.Set`: blocks or unblocks a person for the caller; idempotent. */
    setPersonBlock(
      workspaceId: string,
      userId: string,
      blocked: boolean,
    ): Promise<ChatPersonBlock> {
      return api.put<ChatPersonBlock>(paths.blocks(workspaceId), {
        userId,
        blocked,
      });
    },
  };
}

export type ChatService = ReturnType<typeof createChatService>;
