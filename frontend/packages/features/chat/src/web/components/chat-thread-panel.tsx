import { useState } from "react";
import { BellOff, BellRing, Check, Loader2, X } from "lucide-react";
import { Button, Skeleton, cn } from "@notrelix/ui-web";
import type { ChatMentionPayload, ChatMessage } from "../../core/types/chat";
import type { ChatHooks } from "../query/create-chat-hooks";
import type { ChatCopy } from "./chat.copy";
import { ChatComposer, type ChatComposerMentions } from "./chat-composer";
import { ChatMessageRow } from "./chat-message-row";
import { fillCopy, pluralCopy } from "./use-chat-copy";

export interface ChatThreadPanelProps {
  readonly hooks: ChatHooks;
  readonly workspaceId: string;
  readonly rootId: string;
  readonly viewerId: string;
  readonly authorName: (userId: string) => string;
  readonly copy: ChatCopy;
  readonly locale: string;
  readonly timeZone?: string;
  readonly pinnedIds: ReadonlySet<string>;
  /** The caller, the names and the channels a reply can mention; without it @ and # are plain text. */
  readonly mentionBase?: Pick<
    ChatComposerMentions,
    "selfId" | "people" | "channels"
  >;
  readonly onClose: () => void;
  readonly onReact: (message: ChatMessage, emoji: string, add: boolean) => void;
  readonly onTogglePin: (message: ChatMessage, pinned: boolean) => void;
}

/**
 * The right-hand thread panel (UC-CHT-F01 to F04, F08): the root and its replies
 * in thread order, a page at a time, the follow and mute toggles, mark as read,
 * and a composer that can also send the reply to the channel.
 */
export function ChatThreadPanel({
  hooks,
  workspaceId,
  rootId,
  viewerId,
  authorName,
  copy,
  locale,
  timeZone,
  pinnedIds,
  mentionBase,
  onClose,
  onReact,
  onTogglePin,
}: ChatThreadPanelProps) {
  const thread = hooks.useThreadReplies(workspaceId, rootId);
  const replier = hooks.useReplyInThread(workspaceId, rootId);
  const follower = hooks.useFollowThread(workspaceId);
  const reader = hooks.useMarkThreadRead(workspaceId);
  const [draft, setDraft] = useState("");
  const [alsoSend, setAlsoSend] = useState(false);
  const [notice, setNotice] = useState<string | null>(null);

  const root = thread.data?.pages[0]?.root ?? null;
  const summary = thread.data?.pages[0]?.thread ?? null;
  const replies = thread.data?.pages.flatMap((page) => page.items) ?? [];
  const unread = summary?.unreadReplies ?? 0;

  const mentionables = hooks.useMentionables(
    workspaceId,
    root?.spaceId,
    mentionBase !== undefined && root !== null,
  );

  function send(content: string, payload?: ChatMentionPayload) {
    setNotice(null);
    replier.mutate(
      {
        content,
        alsoSendToChannel: alsoSend,
        mentions: payload?.mentions,
        confirmBroadcast: payload?.confirmBroadcast,
      },
      {
        onSuccess: () => {
          setDraft("");
        },
        onError: () => setNotice(copy.thread.sendFailed),
      },
    );
  }

  function toggleFollow() {
    if (!summary) return;
    follower.mutate({
      threadId: rootId,
      following: !summary.following,
      muted: false,
    });
  }

  function toggleMute() {
    if (!summary) return;
    follower.mutate({
      threadId: rootId,
      following: true,
      muted: !summary.muted,
    });
  }

  return (
    <section
      aria-label={copy.thread.panelLabel}
      className="flex min-h-0 flex-1 flex-col"
      data-testid="chat-thread-panel"
    >
      <header className="flex items-center gap-2 border-b border-border px-4 py-2.5">
        <h2 className="min-w-0 flex-1 truncate text-sm font-semibold">
          {copy.thread.title}
        </h2>
        {summary ? (
          <Button
            type="button"
            size="sm"
            variant={summary.following ? "secondary" : "outline"}
            aria-pressed={summary.following}
            onClick={toggleFollow}
            disabled={follower.isPending}
          >
            {summary.following ? copy.thread.following : copy.thread.follow}
          </Button>
        ) : null}
        {summary?.following ? (
          <Button
            type="button"
            size="icon-sm"
            variant="ghost"
            aria-pressed={summary.muted}
            aria-label={summary.muted ? copy.thread.unmute : copy.thread.mute}
            title={summary.muted ? copy.thread.unmute : copy.thread.mute}
            onClick={toggleMute}
            disabled={follower.isPending}
          >
            {summary.muted ? (
              <BellOff aria-hidden="true" />
            ) : (
              <BellRing aria-hidden="true" />
            )}
          </Button>
        ) : null}
        <Button
          type="button"
          size="icon-sm"
          variant="ghost"
          aria-label={copy.thread.close}
          title={copy.thread.close}
          onClick={onClose}
        >
          <X aria-hidden="true" />
        </Button>
      </header>

      {summary?.following ? (
        <div className="flex flex-wrap items-center gap-2 border-b border-border px-4 py-1.5 text-xs text-muted-foreground">
          {summary.muted ? <span>{copy.thread.muted}</span> : null}
          {unread > 0 ? (
            <>
              <span className="tabular-nums">
                {pluralCopy(
                  {
                    one: copy.thread.unread_one,
                    other: copy.thread.unread_other,
                  },
                  unread,
                  { count: unread },
                )}
              </span>
              <Button
                type="button"
                size="xs"
                variant="ghost"
                disabled={reader.isPending}
                onClick={() =>
                  reader.mutate({
                    threadId: rootId,
                    upToReplySequence: summary.replyCount,
                  })
                }
              >
                <Check aria-hidden="true" />
                {copy.thread.markRead}
              </Button>
            </>
          ) : null}
        </div>
      ) : null}

      {thread.isPending ? (
        <div role="status" className="flex flex-col gap-4 p-4">
          <span className="sr-only">{copy.thread.loading}</span>
          <Skeleton className="h-4 w-2/3" />
          <Skeleton className="h-4 w-full" />
        </div>
      ) : thread.isError ? (
        <div className="flex flex-1 flex-col items-start gap-2 p-4">
          <p role="alert" className="text-sm text-muted-foreground">
            {copy.thread.error}
          </p>
          <Button
            type="button"
            size="sm"
            variant="outline"
            onClick={() => void thread.refetch()}
          >
            {copy.list.retry}
          </Button>
        </div>
      ) : (
        <div
          role="log"
          aria-label={copy.thread.panelLabel}
          aria-live="off"
          className="min-h-0 flex-1 overflow-y-auto py-2"
          data-testid="chat-thread-replies"
        >
          {root ? (
            <div className="border-b border-border pb-2">
              <ChatMessageRow
                message={root}
                author={{ name: authorName(root.authorId) }}
                continued={false}
                isOwn={root.authorId === viewerId}
                locale={locale}
                timeZone={timeZone}
                copy={copy}
                pinned={pinnedIds.has(root.id)}
                reactions={root.reactions ?? []}
                workspaceId={workspaceId}
                hooks={hooks}
                authorName={authorName}
                onReact={onReact}
                onTogglePin={onTogglePin}
              />
            </div>
          ) : null}
          {replies.length === 0 && !thread.isFetching ? (
            <p className="px-4 py-3 text-sm text-muted-foreground">
              {copy.thread.empty}
            </p>
          ) : null}
          <ol className="flex flex-col">
            {replies.map((reply, index) => {
              const previous = replies[index - 1];
              const continued =
                previous !== undefined &&
                previous.authorId === reply.authorId &&
                Date.parse(reply.createdAt) - Date.parse(previous.createdAt) <
                  5 * 60 * 1000;
              return (
                <li key={reply.id} id={`thread-reply-${reply.id}`}>
                  <ChatMessageRow
                    message={reply}
                    author={{ name: authorName(reply.authorId) }}
                    continued={continued}
                    isOwn={reply.authorId === viewerId}
                    locale={locale}
                    timeZone={timeZone}
                    copy={copy}
                    onEdit={() => undefined}
                    onDelete={() => undefined}
                    pinned={pinnedIds.has(reply.id)}
                    reactions={reply.reactions ?? []}
                    workspaceId={workspaceId}
                    hooks={hooks}
                    authorName={authorName}
                    onReact={onReact}
                    onTogglePin={onTogglePin}
                  />
                </li>
              );
            })}
          </ol>
          {thread.hasNextPage ? (
            <div className="flex justify-center px-4 py-2">
              <Button
                type="button"
                size="sm"
                variant="ghost"
                disabled={thread.isFetchingNextPage}
                onClick={() => void thread.fetchNextPage()}
              >
                {thread.isFetchingNextPage ? (
                  <Loader2 className="animate-spin" aria-hidden="true" />
                ) : null}
                {copy.thread.loadMore}
              </Button>
            </div>
          ) : null}
        </div>
      )}

      {notice ? (
        <p role="alert" className="mx-3 mb-1 text-xs text-destructive">
          {notice}
        </p>
      ) : null}
      <ChatComposer
        channelName={
          root
            ? fillCopy(copy.thread.rootOf, { name: authorName(root.authorId) })
            : ""
        }
        value={draft}
        disabledReason={null}
        copy={copy}
        labelOverride={copy.thread.composerLabel}
        busy={replier.isPending}
        onChange={setDraft}
        onSend={send}
        mentions={
          mentionBase
            ? {
                ...mentionBase,
                mentionables: mentionables.data,
                status: mentionables.isError
                  ? "error"
                  : mentionables.isPending
                    ? "loading"
                    : "ready",
                draftKey: `thread-${rootId}`,
              }
            : undefined
        }
        footerExtra={
          <label
            className={cn(
              "flex items-center gap-1.5 text-xs text-muted-foreground",
            )}
          >
            <input
              type="checkbox"
              className="size-4 accent-primary"
              checked={alsoSend}
              onChange={(event) => setAlsoSend(event.target.checked)}
            />
            {copy.thread.alsoSend}
          </label>
        }
      />
    </section>
  );
}
