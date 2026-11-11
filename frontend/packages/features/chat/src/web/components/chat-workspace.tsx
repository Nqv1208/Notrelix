import { useEffect, useMemo, useRef, useState } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { useLocale } from "@notrelix/i18n";
import {
  Button,
  Empty,
  EmptyDescription,
  EmptyHeader,
  EmptyMedia,
  EmptyTitle,
  Sheet,
  SheetContent,
  SheetDescription,
  SheetHeader,
  SheetTitle,
  useIsMobile,
} from "@notrelix/ui-web";
import {
  Archive,
  MessageSquareText,
  MessagesSquare,
  Pin,
  Plus,
  WifiOff,
  X,
} from "lucide-react";
import {
  chatFailureOf,
  type ChatFailureKind,
} from "../../core/api/chat-failure";
import { chatQueryKeys } from "../../query/keys";
import {
  buildChatRows,
  checkMessageContent,
  unconfirmedPending,
  type PendingMessage,
} from "../../core/model/chat-model";
import {
  type ChatChannel,
  type ChatHistoryAnchor,
  type ChatMessage,
  type ChatNotificationLevel,
} from "../../core/types/chat";
import { cn } from "@notrelix/ui-web";
import { ReplyChip } from "./chat-quote";
import { ChatThreadPanel } from "./chat-thread-panel";
import { PinnedBar, PinsPanel } from "./chat-pins";
import { FollowedThreadsPanel } from "./chat-followed-threads";
import type { QuoteView } from "./chat-message-row";
import {
  createChatHooks,
  flattenPages,
  type ChatHookDeps,
} from "../query/create-chat-hooks";
import type { ChatCopy } from "./chat.copy";
import { ChatComposer } from "./chat-composer";
import {
  ChatMessageList,
  type ChatListStatus,
  type ChatScrollRequest,
} from "./chat-message-list";
import { ChatHeader } from "./chat-header";
import { ChatStatusBanner } from "./chat-status-banner";
import {
  ChatSidebar,
  type ChannelListStatus,
  type ChannelUnread,
} from "./chat-sidebar";
import {
  ConfirmDialog,
  CreateChannelDialog,
  RenameChannelDialog,
  type CreateChannelValues,
} from "./chat-dialogs";
import { useChatCopy } from "./use-chat-copy";

export type ChatViewerRole = "owner" | "admin" | "member" | "guest";

/** Whether the realtime stream is live; the host reports a drop while it reconnects. */
export type ChatConnectionState = "live" | "reconnecting";

export interface ChatWorkspaceProps {
  readonly deps: ChatHookDeps;
  readonly workspaceId: string;
  readonly viewerId: string;
  /** The caller's Workspace role. It only shapes the controls; the server authorizes every request. */
  readonly viewerRole: ChatViewerRole | null;
  /** The open channel, or null on the bare `/chat` route. */
  readonly channelId: string | null;
  /** Names of the Workspace members by user id (the host supplies them). */
  readonly people: ReadonlyMap<string, string>;
  readonly onOpenChannel: (channelId: string) => void;
  /** Narrow screens: returns to the channel list (the host's route). Without it the list opens as a sheet. */
  readonly onBackToList?: () => void;
  /** The realtime stream state; a reconnecting stream shows a banner above the messages. */
  readonly connection?: ChatConnectionState;
  readonly timeZone?: string;
}

const LATEST: ChatHistoryAnchor = { kind: "latest" };

interface ChannelLock {
  readonly channelId: string;
  readonly anchor: ChatHistoryAnchor;
  /** The first unread sequence when the channel opened (the "New" divider). */
  readonly firstUnread: number | null;
}

/** The right-hand panel: one thread, the channel's pins, or the threads the caller follows. */
type SidePanel =
  | { readonly kind: "thread"; readonly rootId: string }
  | { readonly kind: "pins" }
  | { readonly kind: "following" };

interface EditState {
  readonly messageId: string;
  readonly draft: string;
  readonly expectedVersion: number;
  readonly original: string;
}

function failureText(kind: ChatFailureKind, copy: ChatCopy): string {
  switch (kind) {
    case "stale":
      return copy.feedback.stale;
    case "network":
      return copy.feedback.network;
    case "rate-limited":
      return copy.feedback.rateLimited;
    case "forbidden":
      return copy.feedback.forbidden;
    case "name-conflict":
      return copy.create.nameTaken;
    case "archived":
      return copy.composer.archived;
    default:
      return copy.feedback.failed;
  }
}

/**
 * The Chat workspace: the channel column, the open channel (header, history,
 * composer) and the channel dialogs. It owns the server state through the
 * query hooks and only shows what the cache holds.
 */
export function ChatWorkspace({
  deps,
  workspaceId,
  viewerId,
  viewerRole,
  channelId,
  people,
  onOpenChannel,
  onBackToList,
  connection = "live",
  timeZone,
}: ChatWorkspaceProps) {
  const copy = useChatCopy();
  const { locale } = useLocale();
  const isMobile = useIsMobile();
  const queryClient = useQueryClient();
  const hooks = useMemo(
    () => createChatHooks(deps),
    // The hooks follow the transport identity, not the object literal.
    [deps.api, deps.endpoints],
  );
  const ws = workspaceId;
  const ch = channelId ?? "";
  const canBrowse = viewerRole !== null && viewerRole !== "guest";
  const canManageWorkspace = viewerRole === "owner" || viewerRole === "admin";

  // ─── Server state ─────────────────────────────────────────────────────────
  const channels = hooks.useChannels(ws);
  const browse = hooks.useBrowseChannels(ws, canBrowse);
  const unread = hooks.useUnreadSummary(ws);
  const detail = hooks.useChannel(ws, channelId ?? undefined);
  const channel: ChatChannel | null =
    detail.data?.channel ??
    channels.data?.find((entry) => entry.id === channelId) ??
    null;
  const members = detail.data?.members ?? [];
  const unreadRow = unread.data?.spaces.find(
    (row) => row.spaceId === channelId,
  );

  // The history opens once per visit: at the first unread message when there is
  // one (D03), otherwise at the newest. The choice does not move while the reader
  // is in the channel, even when the unread summary refreshes.
  const lockRef = useRef<ChannelLock | null>(null);
  if (
    channelId &&
    lockRef.current?.channelId !== channelId &&
    (unread.isFetched || unread.isError)
  ) {
    const first = unreadRow?.firstUnreadSequence ?? null;
    lockRef.current = {
      channelId,
      anchor: first !== null ? { kind: "around", sequence: first } : LATEST,
      firstUnread: first,
    };
  }
  const lock =
    channelId && lockRef.current?.channelId === channelId
      ? lockRef.current
      : null;
  const anchor = lock?.anchor ?? LATEST;

  const messages = hooks.useMessages(ws, ch, anchor);
  const flat = useMemo(() => flattenPages(messages.data), [messages.data]);
  const pendingQuery = hooks.usePendingSends(ws, ch);
  const pendingAll = pendingQuery.data ?? [];
  const pendingHere = useMemo(
    () =>
      unconfirmedPending(
        pendingAll.filter((entry) => entry.spaceId === ch),
        flat,
      ),
    [pendingAll, flat, ch],
  );
  // The divider stays where the visit found it, even as messages arrive.
  const firstUnread = lock ? lock.firstUnread : null;
  const rows = useMemo(
    () =>
      buildChatRows({
        messages: flat,
        pending: pendingHere,
        firstUnread,
        timeZone,
      }),
    [flat, pendingHere, firstUnread, timeZone],
  );

  // ─── Writes ───────────────────────────────────────────────────────────────
  const sender = hooks.useSendMessage(ws, ch, viewerId);
  const editor = hooks.useEditMessage(ws, ch);
  const deleter = hooks.useDeleteMessage(ws, ch);
  const reader = hooks.useMarkRead(ws);
  const levelWriter = hooks.useSetNotificationLevel(ws);
  const creator = hooks.useCreateChannel(ws);
  const updater = hooks.useUpdateChannel(ws, ch);
  const archiver = hooks.useArchiveChannel(ws, ch);
  const joiner = hooks.useJoinChannel(ws);
  const reactor = hooks.useReaction(ws);
  const pinner = hooks.usePinMessage(ws, ch);
  const pinQuery = hooks.usePins(ws, ch);
  const threadCountQuery = hooks.useThreadCounts(ws, ch);
  const pinnedNewest = pinQuery.data?.pages[0]?.items[0]?.message ?? null;
  const pinnedIds = useMemo(
    () =>
      new Set(
        (pinQuery.data?.pages.flatMap((page) => page.items) ?? []).map(
          (pin) => pin.message.id,
        ),
      ),
    [pinQuery.data],
  );
  const pinCount =
    pinQuery.data?.pages.reduce((sum, page) => sum + page.items.length, 0) ?? 0;

  // ─── Local interaction state ──────────────────────────────────────────────
  // Drafts are kept per space while the page is open (UC-CHT-B07). A feature package does not touch browser
  // storage, so keeping them across a reload needs a host-provided store: owner decision, not built here.
  const [drafts, setDrafts] = useState<Readonly<Record<string, string>>>({});
  const [scrollRequest, setScrollRequest] = useState<ChatScrollRequest | null>(
    null,
  );
  const [atBottom, setAtBottom] = useState(true);
  const [editing, setEditing] = useState<EditState | null>(null);
  const [confirmDelete, setConfirmDelete] = useState<ChatMessage | null>(null);
  const [notice, setNotice] = useState<string | null>(null);
  const [createOpen, setCreateOpen] = useState(false);
  const [createError, setCreateError] = useState<string | null>(null);
  const [renameOpen, setRenameOpen] = useState(false);
  const [renameError, setRenameError] = useState<string | null>(null);
  const [archiveOpen, setArchiveOpen] = useState(false);
  const [listOpen, setListOpen] = useState(false);
  const [replyTo, setReplyTo] = useState<ChatMessage | null>(null);
  const [panel, setPanel] = useState<SidePanel | null>(null);
  const requestedFor = useRef<string | null>(null);
  const nonce = useRef(0);

  function request(target: "divider" | "bottom") {
    nonce.current += 1;
    setScrollRequest({ nonce: nonce.current, target });
  }

  // Open the channel at its divider (or the newest) once its history has loaded.
  useEffect(() => {
    if (!channelId || !messages.isSuccess || !lock) return;
    const key = `${channelId}|${JSON.stringify(anchor)}`;
    if (requestedFor.current === key) return;
    requestedFor.current = key;
    nonce.current += 1;
    setScrollRequest({
      nonce: nonce.current,
      target: lock.firstUnread !== null ? "divider" : "bottom",
    });
    setAtBottom(true);
  }, [channelId, messages.isSuccess, lock, anchor]);

  // Read state: the boundary moves to the newest message only when it is
  // loaded and on screen (the reader is at the bottom and the page is visible).
  const newest = flat[flat.length - 1];
  const hasNewer = messages.hasNextPage === true;
  useEffect(() => {
    if (!channelId || !newest || hasNewer || !atBottom) return;
    if (
      typeof document !== "undefined" &&
      document.visibilityState === "hidden"
    )
      return;
    reader.markRead(channelId, newest.sequence);
    // reader is stable per hook call; the sequence and the position are the inputs.
  }, [channelId, newest?.sequence, hasNewer, atBottom]);

  function jumpToLatest() {
    if (channelId) {
      lockRef.current = { channelId, anchor: LATEST, firstUnread: null };
      requestedFor.current = null;
    }
    request("bottom");
  }

  // ─── Handlers ─────────────────────────────────────────────────────────────
  const composerDisabledReason = !channel
    ? null
    : channel.isArchived
      ? copy.composer.archived
      : !channel.isMember
        ? copy.composer.notMember
        : null;
  const draft = drafts[ch] ?? "";

  function setDraft(value: string) {
    if (!channelId) return;
    setDrafts((prev) => {
      const next = { ...prev };
      if (value.length === 0) delete next[channelId];
      else next[channelId] = value;
      return next;
    });
  }

  // A reply and an open panel belong to one channel: both close when the channel changes.
  useEffect(() => {
    setReplyTo(null);
    setPanel(null);
  }, [channelId]);

  function handleSend(content: string) {
    if (!channelId) return;
    if (hasNewer) jumpToLatest();
    sender.send(content, replyTo?.id ?? null);
    setReplyTo(null);
    setDraft("");
    request("bottom");
  }

  function handleReact(message: ChatMessage, emoji: string, add: boolean) {
    reactor.mutate(
      { messageId: message.id, kind: emoji, add },
      { onError: () => setNotice(copy.reactions.failed) },
    );
  }

  function handleTogglePin(message: ChatMessage, pinned: boolean) {
    pinner.mutate(
      { messageId: message.id, pinned },
      {
        onError: () =>
          setNotice(pinned ? copy.pins.failed : copy.pins.unpinFailed),
      },
    );
  }

  function handleOpenThread(message: ChatMessage) {
    setPanel({ kind: "thread", rootId: message.threadRootId ?? message.id });
  }

  function togglePanel(next: SidePanel) {
    setPanel((current) =>
      current !== null && current.kind === next.kind ? null : next,
    );
  }

  function quoteOf(message: ChatMessage): QuoteView | null {
    if (!message.replyToId) return null;
    const target = flat.find((item) => item.id === message.replyToId);
    if (target) {
      return {
        name: authorName(target.authorId),
        text: target.deleted ? null : target.content,
        deleted: target.deleted,
        unavailable: false,
      };
    }
    if (message.replyPreview) {
      return {
        name: authorName(message.replyPreview.authorId),
        text: message.replyPreview.content,
        deleted: message.replyPreview.deleted,
        unavailable: false,
      };
    }
    return {
      name: copy.list.unknownAuthor,
      text: null,
      deleted: false,
      unavailable: true,
    };
  }

  function handleEditStart(message: ChatMessage) {
    if (message.content === null || message.version === undefined) return;
    setEditing({
      messageId: message.id,
      draft: message.content,
      expectedVersion: message.version,
      original: message.content,
    });
  }

  function handleEditSave() {
    if (!editing) return;
    const check = checkMessageContent(editing.draft);
    if (!check.ok) {
      setNotice(copy.composer.emptyHint);
      return;
    }
    if (check.content === editing.original) {
      setEditing(null);
      return;
    }
    editor.mutate(
      {
        messageId: editing.messageId,
        content: check.content,
        expectedVersion: editing.expectedVersion,
      },
      {
        onSuccess: () => {
          setEditing(null);
          setNotice(null);
        },
        onError: (error) => {
          const kind = chatFailureOf(error).kind;
          setEditing(null);
          setNotice(failureText(kind, copy));
        },
      },
    );
  }

  function handleDeleteConfirm() {
    if (!confirmDelete) return;
    const target = confirmDelete;
    deleter.mutate(
      { messageId: target.id, expectedVersion: target.version },
      {
        onSuccess: () => {
          setConfirmDelete(null);
          setNotice(null);
        },
        onError: (error) => {
          setConfirmDelete(null);
          setNotice(failureText(chatFailureOf(error).kind, copy));
        },
      },
    );
  }

  function handleCreate(values: CreateChannelValues) {
    setCreateError(null);
    creator.mutate(
      {
        name: values.name,
        topic: values.topic.length > 0 ? values.topic : null,
        isPrivate: values.isPrivate,
      },
      {
        onSuccess: (created) => {
          setCreateOpen(false);
          onOpenChannel(created.id);
        },
        onError: (error) =>
          setCreateError(failureText(chatFailureOf(error).kind, copy)),
      },
    );
  }

  function handleRename(values: { name: string; topic: string }) {
    if (!channel) return;
    setRenameError(null);
    updater.mutate(
      {
        name: values.name,
        topic: values.topic.length > 0 ? values.topic : null,
        expectedVersion: channel.version,
      },
      {
        onSuccess: () => setRenameOpen(false),
        onError: (error) => {
          const kind = chatFailureOf(error).kind;
          if (kind === "stale") {
            setRenameOpen(false);
            setNotice(copy.feedback.stale);
            void queryClient.invalidateQueries({
              queryKey: chatQueryKeys.channel(ws, ch),
            });
            return;
          }
          setRenameError(
            kind === "name-conflict" ? copy.edit.nameTaken : copy.edit.failed,
          );
        },
      },
    );
  }

  function handleArchive() {
    if (!channel) return;
    archiver.mutate(
      { archived: !channel.isArchived, expectedVersion: channel.version },
      {
        onSuccess: () => setArchiveOpen(false),
        onError: (error) => {
          setArchiveOpen(false);
          setNotice(failureText(chatFailureOf(error).kind, copy));
        },
      },
    );
  }

  function handleJoin(targetId: string, onJoined?: () => void) {
    joiner.mutate(targetId, {
      onSuccess: () => onJoined?.(),
      onError: () => setNotice(copy.feedback.failed),
    });
  }

  function handleLevel(level: ChatNotificationLevel) {
    if (!channelId) return;
    levelWriter.mutate({ spaceId: channelId, level });
  }

  // ─── View ─────────────────────────────────────────────────────────────────
  const channelsStatus: ChannelListStatus = channels.isPending
    ? "loading"
    : channels.isError
      ? "error"
      : "ready";
  const browseView = canBrowse
    ? {
        channels: browse.data ?? [],
        status: (browse.isPending
          ? "loading"
          : browse.isError
            ? "error"
            : "ready") as ChannelListStatus,
      }
    : null;
  const unreadBySpace = useMemo(() => {
    const map = new Map<string, ChannelUnread>();
    for (const row of unread.data?.spaces ?? []) {
      map.set(row.spaceId, { count: row.unreadCount, muted: row.muted });
    }
    return map;
  }, [unread.data]);
  const authorName = (userId: string) =>
    people.get(userId) ?? copy.list.unknownAuthor;
  const channelNameOf = (spaceId: string) =>
    channels.data?.find((entry) => entry.id === spaceId)?.name ??
    browse.data?.find((entry) => entry.id === spaceId)?.name ??
    "";

  const detailFailure = detail.isError
    ? chatFailureOf(detail.error).kind
    : null;
  const messagesFailure = messages.isError
    ? chatFailureOf(messages.error).kind
    : null;
  const listStatus: ChatListStatus =
    detailFailure === "not-found" || detailFailure === "forbidden"
      ? "no-access"
      : messagesFailure === "not-found" || messagesFailure === "forbidden"
        ? "no-access"
        : messages.isPending || !channelId
          ? "loading"
          : messages.isError
            ? "error"
            : "ready";
  const canManage = canManageWorkspace || channel?.role === "manager";

  const viewerName = people.get(viewerId);
  const viewer =
    viewerId && viewerName ? { id: viewerId, name: viewerName } : null;
  // On a narrow screen with no channel open, the list is the page.
  const listOnly =
    isMobile && channelId === null && (channels.data?.length ?? 0) > 0;
  const openList = () => (onBackToList ? onBackToList() : setListOpen(true));

  const sidebar = (
    <ChatSidebar
      channels={channels.data ?? []}
      status={channelsStatus}
      activeChannelId={channelId}
      unreadBySpace={unreadBySpace}
      browse={browseView}
      canCreate={viewerRole !== null && viewerRole !== "guest"}
      copy={copy}
      joiningId={joiner.isPending ? ch : null}
      onSelect={(id) => {
        setListOpen(false);
        onOpenChannel(id);
      }}
      onCreate={() => {
        setCreateError(null);
        setCreateOpen(true);
      }}
      onJoin={(id) => handleJoin(id, () => onOpenChannel(id))}
      onRetry={() => void channels.refetch()}
      viewer={viewer}
    />
  );

  return (
    <div className="flex h-full min-h-0 w-full">
      {listOnly ? (
        <div className="flex min-h-0 w-full flex-col bg-sidebar">{sidebar}</div>
      ) : isMobile ? (
        <Sheet open={listOpen} onOpenChange={setListOpen}>
          <SheetContent side="left" className="w-72 p-0">
            <SheetHeader className="sr-only">
              <SheetTitle>{copy.sidebar.listTitle}</SheetTitle>
              <SheetDescription>{copy.sidebar.navLabel}</SheetDescription>
            </SheetHeader>
            <div className="flex h-full flex-col">{sidebar}</div>
          </SheetContent>
        </Sheet>
      ) : (
        <aside className="flex w-64 shrink-0 flex-col border-e border-border bg-sidebar xl:w-72">
          {sidebar}
        </aside>
      )}

      {listOnly ? null : (
        <section
          className={cn(
            "flex min-h-0 min-w-0 flex-1 flex-col",
            isMobile && panel !== null && "hidden",
          )}
          aria-label={copy.page.title}
        >
          {!channelId ? (
            <div className="flex flex-1 items-center justify-center p-6">
              {channels.data && channels.data.length === 0 ? (
                <Empty>
                  <EmptyHeader>
                    <EmptyMedia variant="icon">
                      <MessageSquareText aria-hidden="true" />
                    </EmptyMedia>
                    <EmptyTitle>{copy.page.noChannelsTitle}</EmptyTitle>
                    <EmptyDescription>
                      {copy.page.noChannelsBody}
                    </EmptyDescription>
                  </EmptyHeader>
                  {viewerRole !== null && viewerRole !== "guest" ? (
                    <Button onClick={() => setCreateOpen(true)}>
                      <Plus aria-hidden="true" />
                      {copy.page.createChannel}
                    </Button>
                  ) : null}
                </Empty>
              ) : (
                <p className="text-sm text-muted-foreground">
                  {copy.page.chooseChannel}
                </p>
              )}
            </div>
          ) : channel === null && detailFailure !== null ? (
            <Empty className="flex-1">
              <EmptyHeader>
                <EmptyMedia variant="icon">
                  <MessageSquareText aria-hidden="true" />
                </EmptyMedia>
                <EmptyTitle>{copy.list.noAccess}</EmptyTitle>
                <EmptyDescription>{copy.list.noAccessBody}</EmptyDescription>
              </EmptyHeader>
            </Empty>
          ) : channel === null ? (
            <div
              role="status"
              className="flex flex-1 items-center justify-center text-sm text-muted-foreground"
            >
              {copy.page.loadingChannel}
            </div>
          ) : (
            <>
              <ChatHeader
                channel={channel}
                members={members}
                viewerId={viewerId}
                canManage={canManage}
                joining={joiner.isPending}
                authorName={authorName}
                copy={copy}
                onOpenList={openList}
                onRename={() => {
                  setRenameError(null);
                  setRenameOpen(true);
                }}
                onToggleArchive={() => setArchiveOpen(true)}
                onJoin={() => handleJoin(ch)}
                onSetLevel={handleLevel}
              />
              {connection === "reconnecting" ? (
                <ChatStatusBanner tone="warning" icon={<WifiOff />}>
                  {copy.list.reconnecting}
                </ChatStatusBanner>
              ) : null}
              {channel.isArchived ? (
                <ChatStatusBanner tone="info" icon={<Archive />}>
                  {copy.header.archivedNotice}
                </ChatStatusBanner>
              ) : null}
              <PinnedBar
                copy={copy}
                count={pinCount}
                newest={pinnedNewest}
                newestAuthor={
                  pinnedNewest ? authorName(pinnedNewest.authorId) : ""
                }
                onOpen={() => togglePanel({ kind: "pins" })}
              />
              <div className="flex items-center justify-end gap-1 border-b border-border px-3 py-1">
                <Button
                  type="button"
                  size="sm"
                  variant={panel?.kind === "pins" ? "secondary" : "ghost"}
                  aria-pressed={panel?.kind === "pins"}
                  onClick={() => togglePanel({ kind: "pins" })}
                >
                  <Pin aria-hidden="true" />
                  {copy.pins.tab}
                </Button>
                <Button
                  type="button"
                  size="sm"
                  variant={panel?.kind === "following" ? "secondary" : "ghost"}
                  aria-pressed={panel?.kind === "following"}
                  onClick={() => togglePanel({ kind: "following" })}
                >
                  <MessagesSquare aria-hidden="true" />
                  {copy.thread.followedTab}
                </Button>
              </div>
              <ChatMessageList
                channelName={channel.name}
                rows={rows}
                status={listStatus}
                hasOlder={messages.hasPreviousPage === true}
                isLoadingOlder={messages.isFetchingPreviousPage}
                hasNewer={hasNewer}
                isLoadingNewer={messages.isFetchingNextPage}
                scrollRequest={scrollRequest}
                editing={
                  editing
                    ? {
                        messageId: editing.messageId,
                        draft: editing.draft,
                        busy: editor.isPending,
                      }
                    : null
                }
                staleNotice={notice}
                viewerId={viewerId}
                authorName={authorName}
                locale={locale}
                timeZone={timeZone}
                copy={copy}
                pendingEntries={pendingHere}
                onRetryLoad={() => void messages.refetch()}
                onOlderNeeded={() => {
                  if (!messages.isFetchingPreviousPage)
                    void messages.fetchPreviousPage();
                }}
                onNewerNeeded={() => {
                  if (!messages.isFetchingNextPage)
                    void messages.fetchNextPage();
                }}
                onAtBottomChange={setAtBottom}
                onJumpLatest={jumpToLatest}
                onEdit={handleEditStart}
                onEditDraftChange={(value) =>
                  setEditing((current) =>
                    current ? { ...current, draft: value } : current,
                  )
                }
                onEditSave={handleEditSave}
                onEditCancel={() => setEditing(null)}
                onDelete={(message) => setConfirmDelete(message)}
                onRetryPending={(entry: PendingMessage) => sender.retry(entry)}
                onDiscardPending={(id) => sender.discard(id)}
                onBackToList={isMobile ? openList : undefined}
                workspaceId={ws}
                hooks={hooks}
                pinnedIds={pinnedIds}
                threadCounts={threadCountQuery.data}
                quoteOf={quoteOf}
                onReact={handleReact}
                onReply={(message) => setReplyTo(message)}
                onOpenThread={handleOpenThread}
                onTogglePin={handleTogglePin}
                onOpenQuote={(id) =>
                  document
                    .getElementById(`message-${id}`)
                    ?.scrollIntoView({ block: "center" })
                }
              />
              <ChatComposer
                channelName={channel.name}
                value={draft}
                disabledReason={composerDisabledReason}
                copy={copy}
                onChange={setDraft}
                onSend={handleSend}
                leading={
                  replyTo ? (
                    <ReplyChip
                      copy={copy}
                      name={authorName(replyTo.authorId)}
                      text={
                        replyTo.deleted
                          ? copy.list.deleted
                          : (replyTo.content ?? "")
                      }
                      onCancel={() => setReplyTo(null)}
                    />
                  ) : null
                }
              />
            </>
          )}
        </section>
      )}

      {channelId && channel && panel !== null ? (
        <aside
          className={cn(
            "flex min-h-0 w-full flex-col border-s border-border bg-background md:w-96 md:shrink-0",
          )}
          aria-label={copy.panel.label}
          data-testid="chat-side-panel"
        >
          <div
            role="tablist"
            aria-label={copy.panel.label}
            className="flex items-center gap-1 border-b border-border px-2 py-1"
          >
            {panel.kind === "thread" ? (
              <button
                type="button"
                role="tab"
                aria-selected
                className="rounded-md bg-muted px-2.5 py-1.5 text-sm font-medium"
              >
                {copy.thread.title}
              </button>
            ) : null}
            <button
              type="button"
              role="tab"
              aria-selected={panel.kind === "pins"}
              onClick={() => setPanel({ kind: "pins" })}
              className={cn(
                "rounded-md px-2.5 py-1.5 text-sm font-medium",
                panel.kind === "pins"
                  ? "bg-muted"
                  : "text-muted-foreground hover:bg-muted",
              )}
            >
              {copy.pins.tab}
            </button>
            <button
              type="button"
              role="tab"
              aria-selected={panel.kind === "following"}
              onClick={() => setPanel({ kind: "following" })}
              className={cn(
                "rounded-md px-2.5 py-1.5 text-sm font-medium",
                panel.kind === "following"
                  ? "bg-muted"
                  : "text-muted-foreground hover:bg-muted",
              )}
            >
              {copy.thread.followedTab}
            </button>
            <Button
              type="button"
              size="icon-sm"
              variant="ghost"
              className="ms-auto"
              aria-label={copy.pins.close}
              title={copy.pins.close}
              onClick={() => setPanel(null)}
            >
              <X aria-hidden="true" />
            </Button>
          </div>
          {panel.kind === "thread" ? (
            <ChatThreadPanel
              key={panel.rootId}
              hooks={hooks}
              workspaceId={ws}
              rootId={panel.rootId}
              viewerId={viewerId}
              authorName={authorName}
              copy={copy}
              locale={locale}
              timeZone={timeZone}
              pinnedIds={pinnedIds}
              onClose={() => setPanel(null)}
              onReact={handleReact}
              onTogglePin={handleTogglePin}
            />
          ) : null}
          {panel.kind === "pins" ? (
            <PinsPanel
              hooks={hooks}
              workspaceId={ws}
              channelId={ch}
              copy={copy}
              locale={locale}
              timeZone={timeZone}
              authorName={authorName}
              canUnpin={() => true}
              unpinBusy={pinner.isPending}
              onUnpin={(message) => handleTogglePin(message, false)}
            />
          ) : null}
          {panel.kind === "following" ? (
            <FollowedThreadsPanel
              hooks={hooks}
              workspaceId={ws}
              copy={copy}
              authorName={authorName}
              channelName={channelNameOf}
              onOpenThread={(rootId) => setPanel({ kind: "thread", rootId })}
            />
          ) : null}
        </aside>
      ) : null}

      <CreateChannelDialog
        open={createOpen}
        busy={creator.isPending}
        error={createError}
        copy={copy}
        onOpenChange={setCreateOpen}
        onSubmit={handleCreate}
      />
      {channel ? (
        <RenameChannelDialog
          key={channel.id}
          open={renameOpen}
          busy={updater.isPending}
          initialName={channel.name}
          initialTopic={channel.topic}
          error={renameError}
          copy={copy}
          onOpenChange={setRenameOpen}
          onSubmit={handleRename}
        />
      ) : null}
      {channel ? (
        <ConfirmDialog
          open={archiveOpen}
          title={
            channel.isArchived
              ? copy.archive.titleUnarchive
              : copy.archive.titleArchive
          }
          body={
            channel.isArchived
              ? copy.archive.bodyUnarchive
              : copy.archive.bodyArchive
          }
          confirmLabel={
            channel.isArchived
              ? copy.archive.confirmUnarchive
              : copy.archive.confirmArchive
          }
          cancelLabel={copy.archive.cancel}
          busy={archiver.isPending}
          onOpenChange={setArchiveOpen}
          onConfirm={handleArchive}
        />
      ) : null}
      <ConfirmDialog
        open={confirmDelete !== null}
        title={copy.list.deleteTitle}
        body={copy.list.deleteBody}
        confirmLabel={copy.list.confirmDelete}
        cancelLabel={copy.list.cancel}
        destructive
        busy={deleter.isPending}
        onOpenChange={(open) => {
          if (!open) setConfirmDelete(null);
        }}
        onConfirm={handleDeleteConfirm}
      />
    </div>
  );
}
