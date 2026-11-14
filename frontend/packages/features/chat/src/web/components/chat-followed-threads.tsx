import { Button, Skeleton } from "@notrelix/ui-web";
import { MessagesSquare } from "lucide-react";
import type { ChatFollowedThread } from "../../core/types/chat";
import type { ChatHooks } from "../query/create-chat-hooks";
import type { ChatCopy } from "./chat.copy";
import { pluralCopy } from "./use-chat-copy";

export interface FollowedThreadsPanelProps {
  readonly hooks: ChatHooks;
  readonly workspaceId: string;
  readonly copy: ChatCopy;
  readonly authorName: (userId: string) => string;
  readonly channelName: (spaceId: string) => string;
  readonly onOpenThread: (rootId: string) => void;
}

/**
 * The threads the caller follows, most recent reply first, with the unread
 * replies of each. Opening one moves to the thread panel; "mark as read" moves
 * its read pointer to the newest reply.
 */
export function FollowedThreadsPanel({
  hooks,
  workspaceId,
  copy,
  authorName,
  channelName,
  onOpenThread,
}: FollowedThreadsPanelProps) {
  const followed = hooks.useFollowedThreads(workspaceId, true);
  const reader = hooks.useMarkThreadRead(workspaceId);
  const items: ChatFollowedThread[] =
    followed.data?.pages.flatMap((page) => page.items) ?? [];

  return (
    <section
      aria-label={copy.thread.followedTitle}
      className="flex min-h-0 flex-1 flex-col"
      data-testid="chat-followed-panel"
    >
      <h2 className="border-b border-border px-4 py-2.5 text-sm font-semibold">
        {copy.thread.followedTitle}
      </h2>
      {followed.isPending ? (
        <div role="status" className="flex flex-col gap-3 p-4">
          <span className="sr-only">{copy.thread.followedTitle}</span>
          <Skeleton className="h-4 w-full" />
          <Skeleton className="h-4 w-2/3" />
        </div>
      ) : followed.isError ? (
        <div className="flex flex-col items-start gap-2 p-4">
          <p role="alert" className="text-sm text-muted-foreground">
            {copy.thread.followedError}
          </p>
          <Button
            type="button"
            size="sm"
            variant="outline"
            onClick={() => void followed.refetch()}
          >
            {copy.list.retry}
          </Button>
        </div>
      ) : items.length === 0 ? (
        <p className="p-4 text-sm text-muted-foreground">
          {copy.thread.followedEmpty}
        </p>
      ) : (
        <ul
          className="flex min-h-0 flex-1 flex-col gap-1 overflow-y-auto p-2"
          aria-label={copy.thread.followedTitle}
        >
          {items.map((entry) => {
            const unread = entry.thread.unreadReplies;
            return (
              <li
                key={entry.root.id}
                className="flex flex-col gap-1.5 rounded-md border border-border p-2.5"
                data-testid="chat-followed-item"
              >
                <div className="flex items-baseline gap-2 text-xs text-muted-foreground">
                  <MessagesSquare
                    className="size-3.5 shrink-0 self-center"
                    aria-hidden="true"
                  />
                  <span className="min-w-0 truncate">
                    {channelName(entry.root.spaceId)}
                  </span>
                  <span className="shrink-0 font-semibold text-foreground">
                    {authorName(entry.root.authorId)}
                  </span>
                </div>
                <p className="line-clamp-2 whitespace-pre-wrap break-words text-sm">
                  {entry.root.deleted ? copy.list.deleted : entry.root.content}
                </p>
                <div className="flex flex-wrap items-center gap-2 text-xs text-muted-foreground">
                  <span className="tabular-nums">
                    {pluralCopy(
                      {
                        one: copy.thread.replies_one,
                        other: copy.thread.replies_other,
                      },
                      entry.thread.replyCount,
                      { count: entry.thread.replyCount },
                    )}
                  </span>
                  {unread > 0 ? (
                    <span className="font-semibold text-primary tabular-nums">
                      {pluralCopy(
                        {
                          one: copy.thread.unread_one,
                          other: copy.thread.unread_other,
                        },
                        unread,
                        { count: unread },
                      )}
                    </span>
                  ) : null}
                  <span className="ms-auto flex gap-1">
                    <Button
                      type="button"
                      size="xs"
                      variant="outline"
                      onClick={() => onOpenThread(entry.root.id)}
                    >
                      {copy.thread.openThread}
                    </Button>
                    {unread > 0 ? (
                      <Button
                        type="button"
                        size="xs"
                        variant="ghost"
                        disabled={reader.isPending}
                        onClick={() =>
                          reader.mutate({
                            threadId: entry.root.id,
                            upToReplySequence: entry.thread.replyCount,
                          })
                        }
                      >
                        {copy.thread.markRead}
                      </Button>
                    ) : null}
                  </span>
                </div>
              </li>
            );
          })}
        </ul>
      )}
      {followed.hasNextPage ? (
        <div className="flex justify-center p-2">
          <Button
            type="button"
            size="sm"
            variant="ghost"
            disabled={followed.isFetchingNextPage}
            onClick={() => void followed.fetchNextPage()}
          >
            {copy.reactions.loadMore}
          </Button>
        </div>
      ) : null}
    </section>
  );
}
