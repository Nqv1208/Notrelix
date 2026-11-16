import { useState } from "react";
import { MessagesSquare } from "lucide-react";
import { Avatar, AvatarFallback, Button, Skeleton, cn } from "@notrelix/ui-web";
import type { ChatFollowedThread } from "../../core/types/chat";
import type { ChatHooks } from "../query/create-chat-hooks";
import type { ChatCopy } from "./chat.copy";
import { avatarToneOf, initialsOf } from "./chat-initials";
import { formatClock } from "./chat-message-row";
import { ChatPanelHeader } from "./chat-panel-header";
import { pluralCopy } from "./use-chat-copy";

export interface FollowedThreadsPanelProps {
  readonly hooks: ChatHooks;
  readonly workspaceId: string;
  readonly copy: ChatCopy;
  readonly authorName: (userId: string) => string;
  readonly channelName: (spaceId: string) => string;
  readonly onOpenThread: (rootId: string) => void;
  readonly onClose: () => void;
  readonly locale?: string;
  readonly timeZone?: string;
}

/**
 * The threads the caller follows, most recent reply first, with the unread
 * replies of each. Opening one moves to the thread panel; "mark as read" moves
 * its read pointer to the newest reply. Chat-Followed-M and -T draw it with a title and a count, two tabs (following,
 * archived), rows of 12 16 padding and an empty state; the board's full page is the side panel here, like the pins.
 */
export function FollowedThreadsPanel({
  hooks,
  workspaceId,
  copy,
  authorName,
  channelName,
  onOpenThread,
  onClose,
  locale = "vi",
  timeZone,
}: FollowedThreadsPanelProps) {
  const followed = hooks.useFollowedThreads(workspaceId, true);
  const reader = hooks.useMarkThreadRead(workspaceId);
  const all: ChatFollowedThread[] =
    followed.data?.pages.flatMap((page) => page.items) ?? [];
  const [tab, setTab] = useState<"following" | "archived">("following");
  // The following tab lists every thread (an archived one dimmed with its chip); the archived tab lists the archived ones.
  const items =
    tab === "archived" ? all.filter((entry) => entry.thread.archived) : all;
  const count = (key: "followedCount" | "followedArchivedCount") =>
    pluralCopy(
      { one: copy.thread[key], other: copy.thread[key] },
      items.length,
      { count: items.length },
    );

  return (
    <section
      aria-label={copy.thread.followedTitle}
      className="flex min-h-0 flex-1 flex-col"
      data-testid="chat-followed-panel"
    >
      <ChatPanelHeader
        title={copy.thread.followedTitle}
        subtitle={
          followed.data
            ? count(
                tab === "archived" ? "followedArchivedCount" : "followedCount",
              )
            : undefined
        }
        subtitleAlways
        closeLabel={copy.pins.close}
        onClose={onClose}
      />
      <div
        role="tablist"
        aria-label={copy.thread.followedTitle}
        className="flex shrink-0 gap-1 border-b border-border px-4"
      >
        {(["following", "archived"] as const).map((key) => (
          <button
            key={key}
            type="button"
            role="tab"
            aria-selected={tab === key}
            onClick={() => setTab(key)}
            className={cn(
              "-mb-px h-11 border-b-2 px-3 text-(length:--type-scale-13p5) font-semibold focus-visible:ring-2 focus-visible:ring-focus-ring focus-visible:outline-none",
              tab === key
                ? "border-primary text-link"
                : "border-transparent text-muted-foreground",
            )}
          >
            {key === "following"
              ? copy.thread.followedTab
              : copy.thread.followedArchivedTab}
          </button>
        ))}
      </div>
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
        <div
          className="flex flex-1 flex-col items-center justify-center gap-2.5 p-6 text-center"
          data-testid="chat-followed-empty"
        >
          <span className="flex size-14 items-center justify-center rounded-[16px] bg-muted text-muted-foreground">
            <MessagesSquare className="size-5" aria-hidden="true" />
          </span>
          <h3 className="text-lg font-[650]">
            {tab === "archived"
              ? copy.thread.followedArchivedEmpty
              : copy.thread.followedEmptyTitle}
          </h3>
          {tab === "following" ? (
            <p className="max-w-[340px] leading-normal text-muted-foreground">
              {copy.thread.followedEmptyBody}
            </p>
          ) : null}
        </div>
      ) : (
        <ul
          className="flex min-h-0 flex-1 flex-col overflow-y-auto"
          aria-label={copy.thread.followedTitle}
        >
          {items.map((entry) => {
            const unread = entry.thread.unreadReplies;
            const archived = entry.thread.archived;
            const author = authorName(entry.root.authorId);
            const when = entry.thread.lastReplyAt;
            return (
              <li
                key={entry.root.id}
                className={cn(
                  "flex items-start gap-3 px-4 py-3",
                  archived && tab === "following" && "opacity-70",
                )}
                data-testid="chat-followed-item"
                data-unread={unread > 0 ? "true" : "false"}
              >
                <Avatar className="size-9" aria-hidden="true">
                  <AvatarFallback
                    className={cn(
                      "text-(length:--type-scale-13) font-[650]",
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
                  <span className="truncate text-xs leading-[20.3px] text-muted-foreground">
                    {channelName(entry.root.spaceId)}
                  </span>
                  <span
                    className={cn(
                      "line-clamp-2 text-sm leading-[20.3px] break-words",
                      unread > 0 && "font-[650]",
                    )}
                  >
                    {entry.root.deleted
                      ? copy.list.deleted
                      : entry.root.content}
                  </span>
                  <span className="text-(length:--type-scale-12p5) leading-[20.3px] text-muted-foreground tabular-nums">
                    {archived
                      ? tab === "archived"
                        ? copy.thread.archivedReopenMeta
                        : copy.thread.archivedMeta
                      : pluralCopy(
                          {
                            one: copy.thread.replies_one,
                            other: copy.thread.replies_other,
                          },
                          entry.thread.replyCount,
                          { count: entry.thread.replyCount },
                        ) +
                        (when
                          ? ` · ${formatClock(when, locale, timeZone)}`
                          : "")}
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
                      className="grid h-5 min-w-5 place-items-center rounded-full bg-primary px-1.5 text-(length:--type-scale-11p5) font-bold text-primary-foreground tabular-nums"
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
                ) : archived && tab === "archived" ? (
                  <Button
                    type="button"
                    size="sm"
                    variant="outline"
                    onClick={() => onOpenThread(entry.root.id)}
                  >
                    {copy.thread.reopen}
                  </Button>
                ) : archived ? (
                  <span className="inline-flex h-[22px] shrink-0 items-center rounded-full bg-muted px-[9px] text-xs font-semibold text-muted-foreground">
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
