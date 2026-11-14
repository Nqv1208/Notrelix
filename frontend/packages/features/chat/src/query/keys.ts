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
  /** The preview of one link, resolved with the viewer's own access (the url or kind and id it names). */
  linkPreview: (workspaceId: string, ref: string) =>
    workspaceQueryKey(workspaceId, "chat", "link-preview", ref),
  /** The channels the caller can see that are linked to one resource (UC-CHT-H10). */
  linkedChannels: (workspaceId: string, kind: string, resourceId: string) =>
    workspaceQueryKey(workspaceId, "chat", "linked-channels", kind, resourceId),
  /** Every linked-channel list of the Workspace (a link change refreshes them all). */
  allLinkedChannels: (workspaceId: string) =>
    workspaceQueryKey(workspaceId, "chat", "linked-channels"),
  /** The threads the caller follows (most recent reply first). */
  followedThreads: (workspaceId: string) =>
    workspaceQueryKey(workspaceId, "chat", "followed-threads"),
  /** The pins of one channel (newest pin first). */
  /** The caller's direct and group conversations (hidden ones are not in the server list). */
  conversations: (workspaceId: string) =>
    workspaceQueryKey(workspaceId, "chat", "conversations"),
  /** The sidebar categories with their names. */
  channelGroups: (workspaceId: string) =>
    workspaceQueryKey(workspaceId, "chat", "channel-groups"),
  /** The people the caller blocked (`Chat.Blocks.List`). */
  blocks: (workspaceId: string) =>
    workspaceQueryKey(workspaceId, "chat", "blocks"),
  pins: (workspaceId: string, channelId: string) =>
    workspaceQueryKey(workspaceId, "chat", "pins", channelId),
  /** The permission overrides of one channel (the settings Permissions tab). */
  permissions: (workspaceId: string, channelId: string) =>
    workspaceQueryKey(workspaceId, "chat", "permissions", channelId),
  /** The Workspace retention rule (Chat settings). */
  retention: (workspaceId: string) =>
    workspaceQueryKey(workspaceId, "chat", "retention"),
  /** The results of one search (the filters and the typed text are in the key; never persisted). */
  search: (workspaceId: string, filters: unknown) =>
    workspaceQueryKey(workspaceId, "chat", "search", filters),
  /** One export's status (polled while it runs). */
  chatExport: (workspaceId: string, exportId: string) =>
    workspaceQueryKey(workspaceId, "chat", "export", exportId),
} as const;
