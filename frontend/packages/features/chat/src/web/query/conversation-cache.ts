import type { QueryClient } from "@tanstack/react-query";
import type {
  ChatChannel,
  ChatConversation,
  ChatConversationPreview,
} from "../../core/types/chat";
import { chatQueryKeys } from "../../query/keys";

/**
 * In-place updates of the conversation list for a message or an unread change. The list is not read again for
 * them: the preview, the order and the unread count move in the cache, and the periodic read stays the
 * source of truth. A space that is in neither list may be a conversation the caller was just added to, so only
 * that case asks the server.
 */

function isKnownChannel(
  queryClient: QueryClient,
  workspaceId: string,
  spaceId: string,
): boolean {
  const channels = queryClient.getQueryData<ChatChannel[]>(
    chatQueryKeys.channels(workspaceId),
  );
  return channels?.some((channel) => channel.id === spaceId) ?? false;
}

/** A list is asked about unknown spaces at most this often: a busy stream must not turn into a request per frame. */
const UNKNOWN_READ_WINDOW_MS = 10_000;
const reads = new WeakMap<
  QueryClient,
  Map<string, { at: number; timer: ReturnType<typeof setTimeout> | null }>
>();

/** Runs `read` now, then at most once per window: frames inside the window share one read at the window's end. */
export function readOncePerWindow(
  queryClient: QueryClient,
  scope: string,
  read: () => void,
): void {
  let byScope = reads.get(queryClient);
  if (!byScope) {
    byScope = new Map();
    reads.set(queryClient, byScope);
  }
  const state = byScope.get(scope) ?? { at: -Infinity, timer: null };
  byScope.set(scope, state);
  const run = () => {
    state.at = Date.now();
    state.timer = null;
    read();
  };
  const wait = state.at + UNKNOWN_READ_WINDOW_MS - Date.now();
  if (wait <= 0) run();
  else if (state.timer === null) state.timer = setTimeout(run, wait);
}

function readUnknown(
  queryClient: QueryClient,
  workspaceId: string,
  spaceId: string,
): void {
  if (isKnownChannel(queryClient, workspaceId, spaceId)) return;
  readOncePerWindow(queryClient, `conversations:${workspaceId}`, () => {
    void queryClient.invalidateQueries({
      queryKey: chatQueryKeys.conversations(workspaceId),
      refetchType: "active",
    });
  });
}

/** A message was stored in `spaceId`: it becomes the conversation's preview and the conversation moves to the top. */
export function touchConversation(
  queryClient: QueryClient,
  workspaceId: string,
  spaceId: string,
  preview: ChatConversationPreview,
): void {
  const key = chatQueryKeys.conversations(workspaceId);
  const list = queryClient.getQueryData<ChatConversation[]>(key);
  if (!list) return;
  const at = list.findIndex((entry) => entry.id === spaceId);
  if (at < 0) {
    readUnknown(queryClient, workspaceId, spaceId);
    return;
  }
  const current = list[at]!;
  if (current.lastMessage && current.lastMessage.sequence >= preview.sequence) {
    return;
  }
  const next: ChatConversation = {
    ...current,
    lastMessage: preview,
    lastActivityAt: preview.createdAt,
  };
  queryClient.setQueryData<ChatConversation[]>(key, [
    next,
    ...list.filter((_, index) => index !== at),
  ]);
}

/**
 * The unread count of a conversation moved (a frame of the caller's own stream, or a read). A higher count means
 * new activity: the conversation moves to the top, with its preview left until the next read of the list.
 */
export function setConversationUnread(
  queryClient: QueryClient,
  workspaceId: string,
  spaceId: string,
  unread: number,
  now: () => string = () => new Date().toISOString(),
): void {
  const key = chatQueryKeys.conversations(workspaceId);
  const list = queryClient.getQueryData<ChatConversation[]>(key);
  if (!list) return;
  const at = list.findIndex((entry) => entry.id === spaceId);
  if (at < 0) {
    if (unread > 0) readUnknown(queryClient, workspaceId, spaceId);
    return;
  }
  const current = list[at]!;
  if (current.unreadCount === unread) return;
  if (unread < current.unreadCount) {
    queryClient.setQueryData<ChatConversation[]>(
      key,
      list.map((entry, index) =>
        index === at ? { ...current, unreadCount: unread } : entry,
      ),
    );
    return;
  }
  queryClient.setQueryData<ChatConversation[]>(key, [
    { ...current, unreadCount: unread, lastActivityAt: now() },
    ...list.filter((_, index) => index !== at),
  ]);
}
