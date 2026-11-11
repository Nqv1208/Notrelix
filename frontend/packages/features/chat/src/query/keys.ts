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
  unread: (workspaceId: string) =>
    workspaceQueryKey(workspaceId, "chat", "unread"),
} as const;
