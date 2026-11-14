/**
 * Thread writes shared by the hooks and the realtime adapter: a stored reply
 * joins its open thread, moves its root's reply count and, when it is also in
 * the channel, the channel history. Each step ignores what it already holds, so
 * the reply of this client (REST answer) and its realtime frame do not double up.
 */

import type { QueryClient } from "@tanstack/react-query";
import type { ChatMessage, ChatThreadSummary } from "../../core/types/chat";
import { chatQueryKeys } from "../../query/keys";
import { appendToNewest, type ChatMessagesCache } from "./message-cache";
import {
  appendThreadMessage,
  bumpThreadCount,
  patchMessageInData,
  type ThreadCounts,
} from "./interaction-cache";

/**
 * Moves the count and last reply carried by a cached root message (history, thread
 * page, pins): the highest count and the latest reply time win, so a stale frame
 * never lowers what the server already told this client.
 */
function patchRoot(
  queryClient: QueryClient,
  workspaceId: string,
  summary: ChatThreadSummary,
): void {
  const patch = (root: ChatMessage): ChatMessage => {
    const current = root.threadReplyCount ?? 0;
    if (current >= summary.replyCount) return root;
    return {
      ...root,
      threadReplyCount: summary.replyCount,
      threadLastReplyAt: summary.lastReplyAt,
    };
  };
  for (const key of [
    chatQueryKeys.messages(workspaceId, summary.spaceId),
    chatQueryKeys.thread(workspaceId, summary.rootMessageId),
  ]) {
    queryClient.setQueriesData({ queryKey: key }, (data: unknown) =>
      patchMessageInData(data, summary.rootMessageId, patch),
    );
  }
}

/** Records a thread's reply count and last reply in the channel's client-side count projection. */
export function seedThreadCount(
  queryClient: QueryClient,
  workspaceId: string,
  summary: ChatThreadSummary,
): void {
  queryClient.setQueryData<ThreadCounts>(
    chatQueryKeys.threadCounts(workspaceId, summary.spaceId),
    (counts) =>
      bumpThreadCount(
        counts ?? {},
        summary.rootMessageId,
        summary.replyCount,
        summary.lastReplyAt,
      ),
  );
  patchRoot(queryClient, workspaceId, summary);
}

/**
 * A reply the server stored (from this client's send or the realtime frame):
 * it joins its open thread, bumps the root's count and, when it is also in the
 * channel, the channel history. Each step ignores what it already holds.
 */
export function applyStoredReply(
  queryClient: QueryClient,
  workspaceId: string,
  reply: ChatMessage,
): void {
  const rootId = reply.threadRootId;
  if (rootId) {
    queryClient.setQueriesData(
      { queryKey: chatQueryKeys.thread(workspaceId, rootId) },
      (data: unknown) => appendThreadMessage(data, reply),
    );
    seedThreadCount(queryClient, workspaceId, {
      rootMessageId: rootId,
      spaceId: reply.spaceId,
      replyCount: reply.threadSequence ?? 0,
      lastReplyAt: reply.createdAt,
      archived: false,
      following: false,
      muted: false,
      unreadReplies: 0,
    });
  }
  if (!rootId || reply.alsoSentToChannel) {
    queryClient.setQueriesData<ChatMessagesCache>(
      { queryKey: chatQueryKeys.messages(workspaceId, reply.spaceId) },
      (cache) => (cache ? appendToNewest(cache, reply) : cache),
    );
  }
}
