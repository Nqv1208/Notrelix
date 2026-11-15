import { Avatar, AvatarFallback, Button, Skeleton, cn } from "@notrelix/ui-web";
import type { ChatFollowedThread } from "../../core/types/chat";
import type { ChatHooks } from "../query/create-chat-hooks";
import type { ChatCopy } from "./chat.copy";
import { avatarToneOf, initialsOf } from "./chat-initials";
import { formatClock } from "./chat-message-row";
import { pluralCopy } from "./use-chat-copy";

export interface FollowedThreadsPanelProps {
  readonly hooks: ChatHooks;
  readonly workspaceId: string;
  readonly copy: ChatCopy;
  readonly authorName: (userId: string) => string;
  readonly channelName: (spaceId: string) => string;
  readonly onOpenThread: (rootId: string) => void;
  readonly locale?: string;
  readonly timeZone?: string;
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
  locale = "vi",
  timeZone,
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
        {items.length > 0 ? (
          <span className="ms-2 text-xs font-normal text-muted-foreground tabular-nums">
            {pluralCopy(
              {
                one: copy.thread.followedCount,
                other: copy.thread.followedCount,
              },
              items.length,
              { count: items.length },
            )}
          </span>
        ) : null}
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
          className="flex min-h-0 flex-1 flex-col overflow-y-auto"
          aria-label={copy.thread.followedTitle}
        >
          {items.map((entry) => {
            const unread = entry.thread.unreadReplies;
            const author = authorName(entry.root.authorId);
            const when = entry.thread.lastReplyAt;
            return (
              <li
                key={entry.root.id}
                className={cn(
                  "flex items-start gap-3 border-b border-border px-4 py-3",
                  entry.thread.archived && "opacity-70",
                )}
                data-testid="chat-followed-item"
                data-unread={unread > 0 ? "true" : "false"}
              >
                <Avatar className="size-9" aria-hidden="true">
                  <AvatarFallback
                    className={cn(
                      "text-compact font-[650]",
                      avatarToneOf(entry.root.authorId),
                    )}
                  >
                    {initialsOf(author)}
                  </AvatarFallback>
                </Avatar>
                <button
                  type="button"
                  onClick={() => onOpenThread(entry.root.id)}
                  className="flex min-w-0 flex-1 flex-col rounded-md text-start focus-visible:ring-2 focus-visible:ring-focus-ring focus-visible:outline-none"
                >
                  <span className="truncate text-xs text-muted-foreground">
                    {channelName(entry.root.spaceId)}
                  </span>
                  <span
                    className={cn(
                      "line-clamp-1 text-sm break-words",
                      unread > 0 && "font-[650]",
                    )}
                  >
                    {entry.root.deleted
                      ? copy.list.deleted
                      : entry.root.content}
                  </span>
                  <span className="text-xs text-muted-foreground tabular-nums">
                    {pluralCopy(
                      {
                        one: copy.thread.replies_one,
                        other: copy.thread.replies_other,
                      },
                      entry.thread.replyCount,
                      { count: entry.thread.replyCount },
                    )}
                    {when ? ` · ${formatClock(when, locale, timeZone)}` : null}
                    {entry.thread.archived
                      ? ` · ${copy.thread.archived}`
                      : null}
                  </span>
                  <span className="sr-only">
                    {author}
                    {unread > 0
                      ? `, ${pluralCopy(
                          {
                            one: copy.thread.unread_one,
                            other: copy.thread.unread_other,
                          },
                          unread,
                          { count: unread },
                        )}`
                      : ""}
                    , {copy.thread.openThread}
                  </span>
                </button>
                {unread > 0 ? (
                  <span className="flex shrink-0 flex-col items-end gap-1">
                    <span
                      data-testid="chat-followed-unread"
                      className="grid h-5 min-w-5 place-items-center rounded-full bg-primary px-1.5 text-mark-xs font-bold text-primary-foreground tabular-nums"
                      aria-hidden="true"
                    >
                      {unread}
                    </span>
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
                  </span>
                ) : entry.thread.archived ? (
                  <span className="shrink-0 rounded-full bg-muted px-2 py-0.5 text-micro text-muted-foreground">
                    {copy.thread.archivedChip}
                  </span>
                ) : null}
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
