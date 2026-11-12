import { workspaceQueryKey } from "@notrelix/query";

/**
 * Chat query keys. Every key is workspace-scoped (`["workspace", id, "chat", ...]`)
 * and the channel-scoped keys carry the channel (space) id, so one channel's
 * data is never read for another. Realtime (S1.8) patches these same keys.
 */
export const chatQueryKeys = {
  all: (workspaceId: string) => workspaceQueryKey(workspaceId, "chat"),
  channels: (workspaceId: string) =>
    workspaceQueryKey(workspaceId, "chat", "channels"),
  browse: (workspaceId: string) =>
    workspaceQueryKey(workspaceId, "chat", "channels", "browse"),
  channel: (workspaceId: string, channelId: string) =>
    workspaceQueryKey(workspaceId, "chat", "channel", channelId),
  /** The history of one channel; the anchor is appended by the infinite query. */
  messages: (workspaceId: string, channelId: string) =>
    workspaceQueryKey(workspaceId, "chat", "messages", channelId),
  /** Client-only projection of the sends not yet confirmed (converges on the server copy). */
  pending: (workspaceId: string, channelId: string) =>
    workspaceQueryKey(workspaceId, "chat", "pending", channelId),
  /** The mention picker of one space (people, broadcast permission, member count). */
  mentionables: (workspaceId: string, spaceId: string) =>
    workspaceQueryKey(workspaceId, "chat", "mentionables", spaceId),
  unread: (workspaceId: string) =>
    workspaceQueryKey(workspaceId, "chat", "unread"),
  /** The replies of one thread (the root message id); the thread summary rides with its pages. */
  thread: (workspaceId: string, rootId: string) =>
    workspaceQueryKey(workspaceId, "chat", "thread", rootId),
  /** Client-only projection: the reply count and last reply of each thread of a channel, by root id. */
  threadCounts: (workspaceId: string, channelId: string) =>
    workspaceQueryKey(workspaceId, "chat", "thread-counts", channelId),
  /** Client-only projection: the files on their way to stored messages (uploading, queued or failed). */
  uploads: (workspaceId: string) =>
    workspaceQueryKey(workspaceId, "chat", "uploads"),
  /** The bytes of one attachment (image preview); read with the session, never shared between messages. */
  attachmentContent: (
    workspaceId: string,
    messageId: string,
    attachmentId: string,
  ) =>
    workspaceQueryKey(
      workspaceId,
      "chat",
      "attachment-content",
      messageId,
      attachmentId,
    ),
  /** The threads the caller follows (most recent reply first). */
  followedThreads: (workspaceId: string) =>
    workspaceQueryKey(workspaceId, "chat", "followed-threads"),
  /** The pins of one channel (newest pin first). */
  /** The caller's direct and group conversations (hidden ones are not in the server list). */
  conversations: (workspaceId: string) =>
    workspaceQueryKey(workspaceId, "chat", "conversations"),
  /** Client-only projection: the people the caller blocked this session (the API has no read of blocks). */
  blocks: (workspaceId: string) =>
    workspaceQueryKey(workspaceId, "chat", "blocks"),
  pins: (workspaceId: string, channelId: string) =>
    workspaceQueryKey(workspaceId, "chat", "pins", channelId),
  /** The permission overrides of one channel (the settings Permissions tab). */
  permissions: (workspaceId: string, channelId: string) =>
    workspaceQueryKey(workspaceId, "chat", "permissions", channelId),
} as const;
