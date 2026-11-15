import { useState } from "react";
import { Archive, Check, Loader2 } from "lucide-react";
import { Button, Skeleton, cn } from "@notrelix/ui-web";
import type { ChatMentionPayload, ChatMessage } from "../../core/types/chat";
import type { ChatHooks } from "../query/create-chat-hooks";
import type { ChatCopy } from "./chat.copy";
import { ChatComposer, type ChatComposerMentions } from "./chat-composer";
import { ChatMessageRow } from "./chat-message-row";
import { ChatPanelHeader } from "./chat-panel-header";
import { ThreadMoreMenu } from "./chat-thread-menu";
import { chatFailureOf } from "../../core/api/chat-failure";
import { AUTHOR_RUN_MINUTES } from "../../core/model/chat-model";
import {
  RowActivityProvider,
  createRowActivityStore,
} from "./chat-row-activity";
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
  /** The channel as the header names it, such as "# erp-rollout". */
  readonly spaceLabel: string;
  /** The caller, the names and the channels a reply can mention; without it @ and # are plain text. */
  readonly mentionBase?: Pick<
    ChatComposerMentions,
    "selfId" | "people" | "channels"
  >;
  readonly onClose: () => void;
  /** Without a handler (an archived or read-only space) the action is not offered. */
  readonly onReact?: (
    message: ChatMessage,
    emoji: string,
    add: boolean,
  ) => void;
  readonly onTogglePin?: (message: ChatMessage, pinned: boolean) => void;
  /** An archived channel is read-only: its files cannot be removed. */
  readonly attachmentsReadOnly?: boolean;
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
  spaceLabel,
  mentionBase,
  onClose,
  onReact,
  onTogglePin,
  attachmentsReadOnly = false,
}: ChatThreadPanelProps) {
  const thread = hooks.useThreadReplies(workspaceId, rootId);
  const replier = hooks.useReplyInThread(workspaceId, rootId);
  const follower = hooks.useFollowThread(workspaceId);
  const reader = hooks.useMarkThreadRead(workspaceId);
  const [draft, setDraft] = useState("");
  const [alsoSend, setAlsoSend] = useState(false);
  const [notice, setNotice] = useState<string | null>(null);
  const [activity] = useState(createRowActivityStore);

  const root = thread.data?.pages[0]?.root ?? null;
  const summary = thread.data?.pages[0]?.thread ?? null;
  const replies = thread.data?.pages.flatMap((page) => page.items) ?? [];
  const unread = summary?.unreadReplies ?? 0;

  const mentionables = hooks.useMentionables(
    workspaceId,
    root?.spaceId,
    mentionBase !== undefined && root !== null,
  );

  function send(
    content: string,
    payload?: ChatMentionPayload,
    files?: readonly File[],
  ) {
    setNotice(null);
    replier.mutate(
      {
        content,
        files,
        alsoSendToChannel: alsoSend,
        mentions: payload?.mentions,
        confirmBroadcast: payload?.confirmBroadcast,
      },
      {
        onSuccess: () => {
          setDraft("");
        },
        onError: (error) =>
          // A 429 says to wait; the draft stays either way.
          setNotice(
            chatFailureOf(error).kind === "rate-limited"
              ? copy.feedback.rateLimited
              : copy.thread.sendFailed,
          ),
      },
    );
  }

  function toggleFollow() {
    if (!summary) return;
    setNotice(null);
    follower.mutate(
      { threadId: rootId, following: !summary.following, muted: false },
      { onError: () => setNotice(copy.thread.actionFailed) },
    );
  }

  function toggleMute() {
    if (!summary) return;
    setNotice(null);
    follower.mutate(
      { threadId: rootId, following: true, muted: !summary.muted },
      { onError: () => setNotice(copy.thread.actionFailed) },
    );
  }

  return (
    <section
      aria-label={copy.thread.panelLabel}
      className="flex min-h-0 flex-1 flex-col"
      data-testid="chat-thread-panel"
    >
      <ChatPanelHeader
        title={copy.thread.title}
        subtitle={spaceLabel}
        closeLabel={copy.thread.close}
        onClose={onClose}
      >
        {summary ? (
          <span
            className={cn(
              "inline-flex h-[22px] shrink-0 items-center rounded-full px-[9px] text-xs font-semibold",
              summary.following
                ? "bg-primary/10 text-link"
                : "bg-muted text-muted-foreground",
            )}
            data-testid="chat-thread-follow-state"
          >
            {summary.following
              ? copy.thread.following
              : copy.thread.notFollowing}
          </span>
        ) : null}
        {summary ? (
          <ThreadMoreMenu
            copy={copy}
            following={summary.following}
            muted={summary.muted}
            busy={follower.isPending}
            onToggleFollow={toggleFollow}
            onToggleMute={toggleMute}
          />
        ) : null}
      </ChatPanelHeader>

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
          className="min-h-0 flex-1 overflow-y-auto pt-1 pb-2"
          data-testid="chat-thread-replies"
        >
          <RowActivityProvider value={activity}>
            {root ? (
              <div>
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
                  attachmentsReadOnly={attachmentsReadOnly}
                />
              </div>
            ) : null}
            {root && replies.length > 0 ? (
              <div
                role="separator"
                data-testid="chat-thread-separator"
                className="my-1.5 flex h-6 items-center gap-3 px-6 before:h-px before:flex-1 before:bg-border before:content-[''] after:h-px after:flex-1 after:bg-border after:content-['']"
              >
                <span className="text-xs leading-[1.45] font-semibold whitespace-nowrap text-muted-foreground">
                  {pluralCopy(
                    {
                      one: copy.thread.replies_one,
                      other: copy.thread.replies_other,
                    },
                    Math.max(summary?.replyCount ?? 0, replies.length),
                  )}
                </span>
              </div>
            ) : null}
            {replies.length === 0 && !thread.isFetching ? (
              <p className="px-4 py-3 text-sm text-muted-foreground">
                {copy.thread.empty}
              </p>
            ) : null}
            <ol className="flex flex-col pb-2">
              {replies.map((reply, index) => {
                const continued = sameGroup(replies[index - 1], reply);
                const groupEnd = !sameGroup(reply, replies[index + 1]);
                return (
                  <li key={reply.id} id={`thread-reply-${reply.id}`}>
                    <ChatMessageRow
                      message={reply}
                      author={{ name: authorName(reply.authorId) }}
                      continued={continued}
                      groupEnd={groupEnd}
                      isOwn={reply.authorId === viewerId}
                      locale={locale}
                      timeZone={timeZone}
                      copy={copy}
                      pinned={pinnedIds.has(reply.id)}
                      reactions={reply.reactions ?? []}
                      workspaceId={workspaceId}
                      hooks={hooks}
                      authorName={authorName}
                      onReact={onReact}
                      onTogglePin={onTogglePin}
                      attachmentsReadOnly={attachmentsReadOnly}
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
          </RowActivityProvider>
        </div>
      )}

      {summary?.archived ? (
        <p
          role="status"
          data-testid="chat-thread-archived"
          className="mx-0 flex items-start gap-2 bg-primary/10 px-4 py-2.5 text-xs leading-[1.45] font-semibold text-link"
        >
          <Archive className="mt-0.5 size-3.5 shrink-0" aria-hidden="true" />
          {copy.thread.archivedBanner}
        </p>
      ) : null}
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
        labelOverride={
          summary?.archived
            ? copy.thread.reopenLabel
            : copy.thread.composerLabel
        }
        busy={replier.isPending}
        onChange={setDraft}
        onSend={send}
        attachments={{ draftKey: `thread-${rootId}`, voice: false }}
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

/** Two replies in a row of one author within five minutes share a group. */
function sameGroup(
  previous: ChatMessage | undefined,
  next: ChatMessage | undefined,
): boolean {
  if (!previous || !next) return false;
  if (previous.kind !== "user" || next.kind !== "user") return false;
  if (previous.authorId !== next.authorId) return false;
  const gap = Date.parse(next.createdAt) - Date.parse(previous.createdAt);
  return gap >= 0 && gap <= AUTHOR_RUN_MINUTES * 60_000;
}
