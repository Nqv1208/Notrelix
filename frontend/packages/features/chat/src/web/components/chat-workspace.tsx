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
  Ban,
  EyeOff,
  Lock,
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
  conversationTitle,
  directCounterpart,
  groupRoomLeft,
} from "../../core/model/conversation-model";
import {
  buildMentionInputs,
  trackedFromMessage,
  type TrackedMention,
} from "../../core/model/mention-model";
import {
  CHAT_LIMITS,
  type ChatChannel,
  type ChatMentionPayload,
  type ChatConversation,
  type ChatStartConversationInput,
  type ChatHistoryAnchor,
  type ChatSearchHit,
  type ChatMessage,
  type ChatPermissionEntry,
  type ChatNotificationLevel,
} from "../../core/types/chat";
import { cn } from "@notrelix/ui-web";
import { ReplyChip } from "./chat-quote";
import { ChatThreadPanel } from "./chat-thread-panel";
import { PinnedBar, PinsPanel } from "./chat-pins";
import { FollowedThreadsPanel } from "./chat-followed-threads";
import { MentionsPanel } from "./chat-mentions-panel";
import { ChatMentionProvider } from "./chat-mention-context";
import type { QuoteView } from "./chat-message-row";
import {
  createChatHooks,
  flattenPages,
  type ChatHookDeps,
} from "../query/create-chat-hooks";
import type { ChatCopy } from "./chat.copy";
import { ChatComposer, type ChatComposerMentions } from "./chat-composer";
import {
  ChatMessageList,
  type ChatListStatus,
  type ChatScrollRequest,
} from "./chat-message-list";
import { ChatHeader } from "./chat-header";
import { ChatHighlightProvider } from "./chat-highlight-context";
import { ChatSearchPanel, type ChatSearchSpace } from "./chat-search-panel";
import { ChatStatusBanner } from "./chat-status-banner";
import { ChatRateLimitNotice } from "./chat-rate-limit";
import {
  ChatSidebar,
  type ChannelListStatus,
  unreadBadge,
  type ChannelUnread,
} from "./chat-sidebar";
import {
  ConfirmDialog,
  CreateChannelDialog,
  ModerateMessageDialog,
  RenameChannelDialog,
  type CreateChannelValues,
} from "./chat-dialogs";
import { ChatPermissionsDialog } from "./chat-permissions-dialog";
import { ChatAdminSettings } from "./chat-admin-settings-host";
import { ChatNewMessageDialog } from "./chat-new-message-dialog";
import {
  ChatAddPeopleDialog,
  ChatConversationDetails,
  ChatConversationHeader,
} from "./chat-conversation-view";
import type { ChatConversationListView } from "./chat-conversation-list";
import { fillCopy, pluralCopy, useChatCopy } from "./use-chat-copy";
import { ChatTypingIndicator } from "./chat-typing-indicator";
import type { ChatEphemeralPort } from "../../core/model/ephemeral-model";
import {
  usePresenceHeartbeat,
  usePresenceOf,
  useChatEphemeral,
  useTypingPeople,
  useTypingSignal,
} from "../realtime/use-ephemeral";

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
  /** The open direct or group conversation (it takes the place of the channel), or null. */
  readonly conversationId?: string | null;
  /** Opens a conversation (the host's route). Without it the workspace offers no direct messages. */
  readonly onOpenConversation?: (conversationId: string) => void;
  /** Leaves the open conversation for the bare chat page (after leaving a group). */
  readonly onCloseConversation?: () => void;
  /** Names of the Workspace members by user id (the host supplies them). */
  readonly people: ReadonlyMap<string, string>;
  readonly onOpenChannel: (channelId: string) => void;
  /** Narrow screens: returns to the channel list (the host's route). Without it the list opens as a sheet. */
  readonly onBackToList?: () => void;
  /** The realtime stream state; a reconnecting stream shows a banner above the messages. */
  readonly connection?: ChatConnectionState;
  /**
   * Typing and presence (UC-CHT-C07, C08), supplied by the host over its realtime client. Without it the workspace
   * shows no typing row and no presence dots, and sends nothing.
   */
  readonly ephemeral?: ChatEphemeralPort;
  readonly timeZone?: string;
}

const LATEST: ChatHistoryAnchor = { kind: "latest" };

interface ChannelLock {
  readonly channelId: string;
  readonly anchor: ChatHistoryAnchor;
  /** The first unread sequence when the channel opened (the "New" divider). */
  readonly firstUnread: number | null;
}

/** A search hit being opened: the space to open, the message to land on and, for a thread reply, the thread to show. */
interface SearchJump {
  readonly spaceId: string;
  readonly messageId: string;
  readonly sequence: number;
  readonly threadRootId: string | null;
  readonly applied: boolean;
}

/** The right-hand panel: one thread, the channel's pins, or the threads the caller follows. */
type SidePanel =
  | { readonly kind: "thread"; readonly rootId: string }
  | { readonly kind: "pins" }
  | { readonly kind: "following" }
  | { readonly kind: "mentions" }
  | { readonly kind: "details" };

interface EditState {
  readonly messageId: string;
  readonly draft: string;
  readonly expectedVersion: number;
  readonly original: string;
  /** The mentions the message had; the edit keeps the ones still in the text. */
  readonly tracked: readonly TrackedMention[];
  readonly hadBroadcast: boolean;
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
    case "broadcast-confirmation":
      return copy.mentions.broadcastConfirmFailed;
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
  channelId: channelProp,
  conversationId = null,
  onOpenConversation,
  onCloseConversation,
  people,
  onOpenChannel,
  onBackToList,
  connection = "live",
  ephemeral,
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
  // A conversation is a space like a channel: from here on `channelId` is the id of the open space,
  // whether it is a channel or a conversation.
  const channelId = conversationId ?? channelProp;
  const ch = channelId ?? "";
  const offersConversations = onOpenConversation !== undefined;
  const isGuest = viewerRole === "guest";
  const canBrowse = viewerRole !== null && !isGuest;
  const canManageWorkspace = viewerRole === "owner" || viewerRole === "admin";

  const [permissionsOpen, setPermissionsOpen] = useState(false);
  const [adminOpen, setAdminOpen] = useState(false);
  const [searchOpen, setSearchOpen] = useState(false);
  const [jump, setJump] = useState<SearchJump | null>(null);
  const [highlightId, setHighlightId] = useState<string | null>(null);

  // ─── Server state ─────────────────────────────────────────────────────────
  const channels = hooks.useChannels(ws);
  const browse = hooks.useBrowseChannels(ws, canBrowse);
  const unread = hooks.useUnreadSummary(ws);
  const detail = hooks.useChannel(
    ws,
    conversationId ? undefined : (channelId ?? undefined),
  );
  const conversationsQuery = hooks.useConversations(ws);
  const blockedPeople = hooks.useBlockedPeople(ws).data;
  const channel: ChatChannel | null =
    detail.data?.channel ??
    channels.data?.find((entry) => entry.id === channelId) ??
    null;
  const members = detail.data?.members ?? [];
  const unreadRow = unread.data?.spaces.find(
    (row) => row.spaceId === channelId,
  );

  // The conversation list leaves out a hidden conversation after a refetch; the open one is kept as last
  // seen so the view can offer to show it again.
  const knownConversations = useRef(new Map<string, ChatConversation>());
  for (const entry of conversationsQuery.data ?? []) {
    knownConversations.current.set(entry.id, entry);
  }
  const conversation: ChatConversation | null = conversationId
    ? (conversationsQuery.data?.find((entry) => entry.id === conversationId) ??
      knownConversations.current.get(conversationId) ??
      null)
    : null;

  // A new message in a conversation moves its unread count; the list row (preview, order) follows it.
  const unreadSignature = unread.data?.totalUnread ?? null;
  const signatureSeen = useRef(unreadSignature);
  useEffect(() => {
    if (!offersConversations || signatureSeen.current === unreadSignature)
      return;
    signatureSeen.current = unreadSignature;
    void queryClient.invalidateQueries({
      queryKey: chatQueryKeys.conversations(ws),
      refetchType: "active",
    });
  }, [unreadSignature, offersConversations, queryClient, ws]);

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
  // Sends the server answered with 429: held as they are, retried when the wait is over (UC-CHT-H08).
  const limitedEntries = useMemo(
    () =>
      pendingHere.filter(
        (entry) => entry.status === "failed" && entry.limit != null,
      ),
    [pendingHere],
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
  const channelAdder = hooks.useAddMembers(ws, ch);
  const editor = hooks.useEditMessage(ws, ch);
  const deleter = hooks.useDeleteMessage(ws, ch);
  const moderator = hooks.useModerateMessage(ws, ch);
  const permissionsWriter = hooks.useSetChannelPermissions(ws, ch);
  const reader = hooks.useMarkRead(ws);
  const levelWriter = hooks.useSetNotificationLevel(ws);
  const starter = hooks.useStartConversation(ws);
  const participantAdder = hooks.useAddParticipants(ws, ch);
  const leaver = hooks.useLeaveConversation(ws);
  const visibility = hooks.useSetConversationVisibility(ws);
  const blocker = hooks.useBlockPerson(ws);
  const creator = hooks.useCreateChannel(ws);
  const updater = hooks.useUpdateChannel(ws, ch);
  const archiver = hooks.useArchiveChannel(ws, ch);
  const joiner = hooks.useJoinChannel(ws);
  const reactor = hooks.useReaction(ws);
  const pinner = hooks.usePinMessage(ws, ch);
  const pinQuery = hooks.usePins(ws, ch);
  const permissionsQuery = hooks.useChannelPermissions(
    ws,
    channelId ?? undefined,
    permissionsOpen && !conversationId,
  );
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
  const [permissionsError, setPermissionsError] = useState<string | null>(null);
  const [moderateTarget, setModerateTarget] = useState<ChatMessage | null>(
    null,
  );
  const [moderateError, setModerateError] = useState<string | null>(null);
  const [listOpen, setListOpen] = useState(false);
  const [newMessageOpen, setNewMessageOpen] = useState(false);
  const [newMessageError, setNewMessageError] = useState<string | null>(null);
  const [addPeopleOpen, setAddPeopleOpen] = useState(false);
  const [addPeopleError, setAddPeopleError] = useState<string | null>(null);
  const [leaveOpen, setLeaveOpen] = useState(false);
  const [blockTarget, setBlockTarget] = useState<{
    readonly userId: string;
    readonly blocked: boolean;
  } | null>(null);
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

  // A search hit (UC-CHT-H07): once its space is open, show the message. A message already loaded is only highlighted; otherwise the
  // history reopens around its sequence. A thread reply opens its thread. The highlight fades from state after a few seconds.
  useEffect(() => {
    if (!jump || jump.applied || !lock || channelId !== jump.spaceId) return;
    if (!flat.some((entry) => entry.id === jump.messageId)) {
      const target: ChatHistoryAnchor = {
        kind: "around",
        sequence: jump.sequence,
      };
      lockRef.current = {
        channelId: jump.spaceId,
        anchor: target,
        firstUnread: null,
      };
      requestedFor.current = `${jump.spaceId}|${JSON.stringify(target)}`;
    }
    if (jump.threadRootId !== null) {
      setPanel({ kind: "thread", rootId: jump.threadRootId });
    }
    setJump({ ...jump, applied: true });
  }, [jump, lock, flat, channelId]);

  useEffect(() => {
    if (highlightId === null) return;
    const timer = setTimeout(() => setHighlightId(null), 6000);
    return () => clearTimeout(timer);
  }, [highlightId]);

  function openSearchHit(hit: ChatSearchHit) {
    setHighlightId(hit.messageId);
    setJump({
      spaceId: hit.spaceId,
      messageId: hit.messageId,
      sequence: hit.sequence,
      threadRootId: hit.alsoSentToChannel ? null : hit.threadRootId,
      applied: false,
    });
    setListOpen(false);
    if (hit.spaceId === channelId) return;
    if (hit.spaceKind === "direct" || hit.spaceKind === "group") {
      if (onOpenConversation) onOpenConversation(hit.spaceId);
      else onOpenChannel(hit.spaceId);
    } else {
      onOpenChannel(hit.spaceId);
    }
  }

  // ─── Handlers ─────────────────────────────────────────────────────────────
  const counterpartId = conversation
    ? directCounterpart(conversation, viewerId)
    : null;
  const counterpartBlocked =
    counterpartId !== null && (blockedPeople ?? []).includes(counterpartId);
  const composerDisabledReason = conversation
    ? counterpartBlocked
      ? copy.dm.view.blockedComposer
      : null
    : !channel
      ? null
      : channel.isArchived
        ? copy.composer.archived
        : !channel.isMember
          ? copy.composer.notMember
          : channel.capabilities?.canPost === false
            ? copy.composer.readOnly
            : null;
  const draft = drafts[ch] ?? "";
  // The mention picker of the open space (UC-CHT-G01); only where the caller may post.
  const mentionablesQuery = hooks.useMentionables(
    ws,
    channelId ?? undefined,
    composerDisabledReason === null &&
      (channel !== null || conversation !== null),
  );
  const mentionables = mentionablesQuery.data;
  const [addMemberFailed, setAddMemberFailed] = useState<string | null>(null);

  // Typing and presence: the open space plus the direct conversations in the list are listened to; the
  // stores live in memory only and expire on their own.
  const conversationIds = (conversationsQuery.data ?? [])
    .filter((entry) => !entry.isHidden && entry.kind === "direct")
    .slice(0, 30)
    .map((entry) => entry.id);
  const ephemeralStores = useChatEphemeral(
    ephemeral,
    channelId ? [channelId, ...conversationIds] : conversationIds,
  );
  usePresenceHeartbeat(ephemeral, ephemeral !== undefined);
  const presenceOf = usePresenceOf(ephemeralStores.presence);
  const newestMessage = flat[flat.length - 1];
  const typingIds = useTypingPeople(
    ephemeralStores.typing,
    channelId,
    newestMessage
      ? { id: newestMessage.id, authorId: newestMessage.authorId }
      : null,
  );
  const notifyTyping = useTypingSignal(ephemeral, channelId);

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

  function handleSend(
    content: string,
    payload?: ChatMentionPayload,
    files?: readonly File[],
  ) {
    if (!channelId) return;
    if (hasNewer) jumpToLatest();
    sender.send(content, replyTo?.id ?? null, payload, files);
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
      tracked: trackedFromMessage(message.content, message.mentions),
      hadBroadcast: (message.mentions ?? []).some(
        (mention) =>
          mention.kind === "broadcast-channel" ||
          mention.kind === "broadcast-here",
      ),
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
    // The text replaces the message's mentions: the ones the edit left in place are sent again with their new positions.
    const editMentions = buildMentionInputs(check.content, editing.tracked, {
      canBroadcast: mentionables?.canBroadcast ?? editing.hadBroadcast,
    });
    editor.mutate(
      {
        messageId: editing.messageId,
        content: check.content,
        expectedVersion: editing.expectedVersion,
        mentions: editMentions,
        // A broadcast the message already carried was confirmed when it was sent.
        confirmBroadcast: editing.hadBroadcast ? true : undefined,
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

  function handleModerate(reason: string) {
    if (!moderateTarget) return;
    const target = moderateTarget;
    setModerateError(null);
    moderator.mutate(
      { messageId: target.id, reason, expectedVersion: target.version },
      {
        onSuccess: () => {
          setModerateTarget(null);
          setNotice(null);
        },
        onError: (error) => {
          const kind = chatFailureOf(error).kind;
          if (kind === "stale") {
            setModerateTarget(null);
            setNotice(copy.moderation.stale);
            return;
          }
          setModerateError(
            kind === "forbidden"
              ? copy.moderation.forbidden
              : copy.moderation.failed,
          );
        },
      },
    );
  }

  function handleSavePermissions(
    entries: readonly ChatPermissionEntry[],
    expectedVersion: number,
  ) {
    setPermissionsError(null);
    permissionsWriter.mutate(
      { entries, expectedVersion },
      {
        onSuccess: () => {
          setPermissionsOpen(false);
          setNotice(copy.permissions.saved);
        },
        onError: (error) => {
          const kind = chatFailureOf(error).kind;
          setPermissionsError(
            kind === "stale"
              ? copy.permissions.stale
              : kind === "permission-exceeds-role"
                ? copy.permissions.exceedsRole
                : kind === "permission-lockout"
                  ? copy.permissions.lockout
                  : kind === "forbidden"
                    ? copy.permissions.forbidden
                    : copy.permissions.failed,
          );
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

  function handleRename(values: {
    name: string;
    topic: string;
    slowModeSeconds: number;
  }) {
    if (!channel) return;
    setRenameError(null);
    updater.mutate(
      {
        name: values.name,
        topic: values.topic.length > 0 ? values.topic : null,
        slowModeSeconds: values.slowModeSeconds,
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

  // ─── Conversations ────────────────────────────────────────────────────────
  const nameOf = (userId: string) =>
    people.get(userId) ?? copy.list.unknownAuthor;
  const pickable = useMemo(
    () =>
      [...people.entries()]
        .filter(([id]) => id !== viewerId)
        .map(([id, name]) => ({ id, name })),
    [people, viewerId],
  );
  const addCandidates = useMemo(() => {
    const inside = new Set(conversation?.participants.map((p) => p.userId));
    return pickable.filter((person) => !inside.has(person.id));
  }, [pickable, conversation]);

  function handleStart(input: ChatStartConversationInput) {
    if (!onOpenConversation) return;
    setNewMessageError(null);
    starter.mutate(input, {
      onSuccess: (result) => {
        setNewMessageOpen(false);
        setListOpen(false);
        if (result.skipped.length > 0) {
          setNotice(
            pluralCopy(
              {
                one: copy.dm.details.addSkipped_one,
                other: copy.dm.details.addSkipped_other,
              },
              result.skipped.length,
            ),
          );
        }
        onOpenConversation(result.conversation.id);
      },
      onError: (error) => {
        const failure = chatFailureOf(error);
        setNewMessageError(
          failure.kind === "not-found"
            ? copy.dm.dialog.notFound
            : failure.code === "limit-reached"
              ? fillCopy(copy.dm.dialog.limitReached, {
                  max: CHAT_LIMITS.groupCap,
                })
              : copy.dm.dialog.failed,
        );
      },
    });
  }

  function handleAddPeople(userIds: readonly string[]) {
    setAddPeopleError(null);
    participantAdder.mutate(userIds, {
      onSuccess: (result) => {
        setAddPeopleOpen(false);
        if (result.skipped.length > 0) {
          setNotice(
            pluralCopy(
              {
                one: copy.dm.details.addSkipped_one,
                other: copy.dm.details.addSkipped_other,
              },
              result.skipped.length,
            ),
          );
        }
      },
      onError: (error) =>
        setAddPeopleError(
          chatFailureOf(error).code === "limit-reached"
            ? fillCopy(copy.dm.details.addFull, { max: CHAT_LIMITS.groupCap })
            : copy.dm.details.addFailed,
        ),
    });
  }

  function handleLeave() {
    if (!conversationId) return;
    leaver.mutate(conversationId, {
      onSuccess: () => {
        setLeaveOpen(false);
        knownConversations.current.delete(conversationId);
        onCloseConversation?.();
      },
      onError: () => {
        setLeaveOpen(false);
        setNotice(copy.dm.details.leaveFailed);
      },
    });
  }

  function handleVisibility(hidden: boolean) {
    if (!conversationId) return;
    // The list drops a hidden conversation on its next refetch: keep the view's copy in step by hand.
    if (conversation) {
      knownConversations.current.set(conversationId, {
        ...conversation,
        isHidden: hidden,
      });
    }
    visibility.mutate(
      { conversationId, hidden },
      {
        onError: () =>
          setNotice(hidden ? copy.dm.view.hideFailed : copy.dm.view.showFailed),
      },
    );
  }

  function handleBlockConfirm() {
    if (!blockTarget) return;
    const target = blockTarget;
    blocker.mutate(target, {
      onSuccess: () => setBlockTarget(null),
      onError: () => {
        setBlockTarget(null);
        setNotice(copy.dm.details.blockFailed);
      },
    });
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
      map.set(row.spaceId, {
        count: row.unreadCount,
        muted: row.muted,
        mentions: row.mentionCount,
      });
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

  function handleAddMember(userId: string) {
    setAddMemberFailed(null);
    channelAdder.mutate([userId], {
      onSuccess: (result) => {
        if (result.skipped.includes(userId)) setAddMemberFailed(userId);
      },
      onError: () => setAddMemberFailed(userId),
    });
  }
  const mentionChannels = useMemo(
    () =>
      [...(channels.data ?? []), ...(browse.data ?? [])]
        .filter((entry) => !entry.isArchived)
        .map((entry) => ({ id: entry.id, name: entry.name })),
    [channels.data, browse.data],
  );
  const composerMentions: ChatComposerMentions = {
    selfId: viewerId,
    mentionables,
    status: mentionablesQuery.isError
      ? "error"
      : mentionablesQuery.isPending
        ? "loading"
        : "ready",
    people,
    channels: mentionChannels,
    draftKey: ch,
    // Adding someone is the channel's add-member action: the managers and the Workspace's owners and admins.
    onAddMember:
      canManage && !conversation && !channel?.isArchived
        ? handleAddMember
        : undefined,
    addingUserId: channelAdder.isPending
      ? (channelAdder.variables?.[0] ?? null)
      : null,
    addFailedUserId: addMemberFailed,
  };
  const mentionContext = useMemo(
    () => ({
      viewerId,
      personName: (userId: string) => people.get(userId) ?? null,
      channelName: (spaceId: string) =>
        channels.data?.find((entry) => entry.id === spaceId)?.name ??
        browse.data?.find((entry) => entry.id === spaceId)?.name ??
        null,
      onOpenChannel,
    }),
    [viewerId, people, channels.data, browse.data, onOpenChannel],
  );

  const viewerName = people.get(viewerId);
  const viewer =
    viewerId && viewerName ? { id: viewerId, name: viewerName } : null;
  // On a narrow screen with no channel open, the list is the page.
  const listOnly =
    isMobile && channelId === null && (channels.data?.length ?? 0) > 0;
  const openList = () => (onBackToList ? onBackToList() : setListOpen(true));

  const conversationList: ChatConversationListView | null = offersConversations
    ? {
        conversations: (conversationsQuery.data ?? []).filter(
          (entry) => !entry.isHidden,
        ),
        status: conversationsQuery.isPending
          ? "loading"
          : conversationsQuery.isError
            ? "error"
            : "ready",
        activeId: conversationId,
        viewerId,
        locale,
        timeZone,
        nameOf,
        // The unread summary is refreshed by the realtime stream: it is fresher than the list.
        unreadOf: (entry) =>
          unreadBySpace.get(entry.id)?.count ?? entry.unreadCount,
        presenceOf: ephemeral ? presenceOf : undefined,
        onSelect: (id) => {
          setListOpen(false);
          onOpenConversation?.(id);
        },
        // A Guest starts no conversation (UC-CHT-H02): the entry point is not offered.
        onNewMessage: isGuest
          ? undefined
          : () => {
              setNewMessageError(null);
              setNewMessageOpen(true);
            },
        onRetry: () => void conversationsQuery.refetch(),
      }
    : null;
  const spaceName = conversation
    ? conversationTitle(conversation, viewerId, nameOf)
    : (channel?.name ?? "");
  const spaceReady =
    conversationId !== null ? conversation !== null : channel !== null;

  const searchSpaces = useMemo<readonly ChatSearchSpace[]>(
    () => [
      ...(channels.data ?? []).map((entry) => ({
        id: entry.id,
        label: entry.name,
        kind: entry.kind,
      })),
      ...(conversationsQuery.data ?? []).map((entry) => ({
        id: entry.id,
        label:
          conversationTitle(entry, viewerId, nameOf) || copy.search.directLabel,
        kind: entry.kind,
      })),
    ],
    // nameOf and copy follow people and the locale.
    [channels.data, conversationsQuery.data, viewerId, people, copy],
  );

  const sidebar = (
    <ChatSidebar
      conversations={conversationList}
      channels={channels.data ?? []}
      status={channelsStatus}
      activeChannelId={conversationId ? null : channelProp}
      unreadBySpace={unreadBySpace}
      browse={browseView}
      canCreate={viewerRole !== null && !isGuest}
      guest={isGuest}
      copy={copy}
      joiningId={joiner.isPending ? ch : null}
      onSelect={(id) => {
        setListOpen(false);
        onOpenChannel(id);
      }}
      onOpenSettings={
        canManageWorkspace && !isGuest ? () => setAdminOpen(true) : undefined
      }
      onSearch={() => setSearchOpen(true)}
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
    <ChatMentionProvider value={mentionContext}>
      <ChatHighlightProvider value={highlightId}>
        <div className="flex h-full min-h-0 w-full">
          {listOnly ? (
            <div className="flex min-h-0 w-full flex-col bg-sidebar">
              {sidebar}
            </div>
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
                        <EmptyTitle>
                          {isGuest
                            ? copy.page.guestNoChannelsTitle
                            : copy.page.noChannelsTitle}
                        </EmptyTitle>
                        <EmptyDescription>
                          {isGuest
                            ? copy.page.guestNoChannelsBody
                            : copy.page.noChannelsBody}
                        </EmptyDescription>
                      </EmptyHeader>
                      {viewerRole !== null && !isGuest ? (
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
              ) : conversationId !== null && conversation === null ? (
                conversationsQuery.isPending ? (
                  <div
                    role="status"
                    className="flex flex-1 items-center justify-center text-sm text-muted-foreground"
                  >
                    {copy.page.loadingChannel}
                  </div>
                ) : (
                  <Empty className="flex-1">
                    <EmptyHeader>
                      <EmptyMedia variant="icon">
                        <MessageSquareText aria-hidden="true" />
                      </EmptyMedia>
                      <EmptyTitle>{copy.dm.view.notFoundTitle}</EmptyTitle>
                      <EmptyDescription>
                        {copy.dm.view.notFoundBody}
                      </EmptyDescription>
                    </EmptyHeader>
                    <Button
                      onClick={() => {
                        setNewMessageError(null);
                        setNewMessageOpen(true);
                      }}
                    >
                      {copy.dm.view.newMessage}
                    </Button>
                  </Empty>
                )
              ) : conversationId === null &&
                channel === null &&
                detailFailure !== null ? (
                <Empty className="flex-1">
                  <EmptyHeader>
                    <EmptyMedia variant="icon">
                      <MessageSquareText aria-hidden="true" />
                    </EmptyMedia>
                    <EmptyTitle>{copy.list.noAccess}</EmptyTitle>
                    <EmptyDescription>
                      {copy.list.noAccessBody}
                    </EmptyDescription>
                  </EmptyHeader>
                </Empty>
              ) : !spaceReady ? (
                <div
                  role="status"
                  className="flex flex-1 items-center justify-center text-sm text-muted-foreground"
                >
                  {copy.page.loadingChannel}
                </div>
              ) : (
                <>
                  {conversation ? (
                    <ChatConversationHeader
                      conversation={conversation}
                      viewerId={viewerId}
                      nameOf={nameOf}
                      copy={copy}
                      detailsOpen={panel?.kind === "details"}
                      blocked={counterpartBlocked}
                      presenceOf={ephemeral ? presenceOf : undefined}
                      onOpenList={openList}
                      onToggleDetails={() => togglePanel({ kind: "details" })}
                      onHide={() => handleVisibility(true)}
                      onSearch={() => setSearchOpen(true)}
                    />
                  ) : channel ? (
                    <ChatHeader
                      channel={channel}
                      members={members}
                      viewerId={viewerId}
                      canManage={canManage}
                      joining={joiner.isPending}
                      authorName={authorName}
                      presenceOf={ephemeral ? presenceOf : undefined}
                      copy={copy}
                      onOpenList={openList}
                      onRename={() => {
                        setRenameError(null);
                        setRenameOpen(true);
                      }}
                      onToggleArchive={() => setArchiveOpen(true)}
                      canManagePermissions={
                        channel.capabilities?.canManagePermissions ?? false
                      }
                      onPermissions={() => {
                        setPermissionsError(null);
                        setPermissionsOpen(true);
                      }}
                      onJoin={() => handleJoin(ch)}
                      onSearch={() => setSearchOpen(true)}
                      onSetLevel={handleLevel}
                    />
                  ) : null}
                  {connection === "reconnecting" ? (
                    <ChatStatusBanner tone="warning" icon={<WifiOff />}>
                      {copy.list.reconnecting}
                    </ChatStatusBanner>
                  ) : null}
                  {channel?.isArchived ? (
                    <ChatStatusBanner tone="info" icon={<Archive />}>
                      {copy.header.archivedNotice}
                    </ChatStatusBanner>
                  ) : null}
                  {channel &&
                  !channel.isArchived &&
                  channel.isMember &&
                  channel.capabilities?.canPost === false ? (
                    <ChatStatusBanner tone="info" icon={<Lock />}>
                      {copy.composer.readOnly}
                    </ChatStatusBanner>
                  ) : null}
                  {conversation?.isHidden ? (
                    <ChatStatusBanner
                      tone="info"
                      icon={<EyeOff />}
                      action={
                        <Button
                          type="button"
                          size="sm"
                          variant="outline"
                          disabled={visibility.isPending}
                          onClick={() => handleVisibility(false)}
                        >
                          {copy.dm.view.show}
                        </Button>
                      }
                    >
                      {copy.dm.view.hiddenNotice}
                    </ChatStatusBanner>
                  ) : null}
                  {counterpartBlocked && counterpartId ? (
                    <ChatStatusBanner
                      tone="warning"
                      icon={<Ban />}
                      action={
                        <Button
                          type="button"
                          size="sm"
                          variant="outline"
                          onClick={() =>
                            setBlockTarget({
                              userId: counterpartId,
                              blocked: false,
                            })
                          }
                        >
                          {copy.dm.view.unblock}
                        </Button>
                      }
                    >
                      {fillCopy(copy.dm.view.blockedNotice, {
                        name: nameOf(counterpartId),
                      })}
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
                      variant={
                        panel?.kind === "following" ? "secondary" : "ghost"
                      }
                      aria-pressed={panel?.kind === "following"}
                      onClick={() => togglePanel({ kind: "following" })}
                    >
                      <MessagesSquare aria-hidden="true" />
                      {copy.thread.followedTab}
                    </Button>
                  </div>
                  <ChatMessageList
                    channelName={spaceName}
                    emptyState={
                      conversation
                        ? {
                            title: fillCopy(
                              conversation.kind === "group"
                                ? copy.dm.view.introGroup
                                : copy.dm.view.introDirect,
                              { name: spaceName },
                            ),
                            body: copy.dm.view.introBody,
                          }
                        : undefined
                    }
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
                    onModerate={
                      channel?.capabilities?.canModerate
                        ? (message) => {
                            setModerateError(null);
                            setModerateTarget(message);
                          }
                        : undefined
                    }
                    onRetryPending={(entry: PendingMessage) =>
                      sender.retry(entry)
                    }
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
                    attachmentsReadOnly={channel?.isArchived === true}
                    onOpenQuote={(id) =>
                      document
                        .getElementById(`message-${id}`)
                        ?.scrollIntoView({ block: "center" })
                    }
                  />
                  {ephemeral ? (
                    <ChatTypingIndicator
                      names={typingIds
                        .filter((id) => id !== viewerId)
                        .map((id) => authorName(id))}
                      copy={copy}
                    />
                  ) : null}
                  {limitedEntries.length > 0 ? (
                    <ChatRateLimitNotice
                      key={
                        limitedEntries[limitedEntries.length - 1]!
                          .clientMessageId
                      }
                      seconds={
                        limitedEntries[limitedEntries.length - 1]!
                          .retryAfterSeconds ?? 1
                      }
                      limit={
                        limitedEntries[limitedEntries.length - 1]!.limit ??
                        "rate-limited"
                      }
                      copy={copy}
                      onRetry={() => {
                        for (const entry of limitedEntries) sender.retry(entry);
                      }}
                    />
                  ) : null}
                  <ChatComposer
                    channelName={spaceName}
                    labelOverride={
                      conversation
                        ? fillCopy(copy.dm.view.composer, { name: spaceName })
                        : undefined
                    }
                    value={draft}
                    disabledReason={composerDisabledReason}
                    copy={copy}
                    onChange={setDraft}
                    onTyping={ephemeral ? notifyTyping : undefined}
                    onSend={handleSend}
                    mentions={composerMentions}
                    attachments={{ draftKey: ch }}
                    slowModeSeconds={channel?.slowModeSeconds ?? 0}
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

          {channelId && (channel || conversation) && panel !== null ? (
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
                {conversation ? (
                  <button
                    type="button"
                    role="tab"
                    aria-selected={panel.kind === "details"}
                    onClick={() => setPanel({ kind: "details" })}
                    className={cn(
                      "rounded-md px-2.5 py-1.5 text-sm font-medium",
                      panel.kind === "details"
                        ? "bg-muted"
                        : "text-muted-foreground hover:bg-muted",
                    )}
                  >
                    {conversation.kind === "group"
                      ? copy.dm.details.groupTitle
                      : copy.dm.details.dmTitle}
                  </button>
                ) : null}
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
                <button
                  type="button"
                  role="tab"
                  aria-selected={panel.kind === "mentions"}
                  onClick={() => setPanel({ kind: "mentions" })}
                  className={cn(
                    "rounded-md px-2.5 py-1.5 text-sm font-medium",
                    panel.kind === "mentions"
                      ? "bg-muted"
                      : "text-muted-foreground hover:bg-muted",
                  )}
                >
                  {copy.mentions.tab}
                  {(unread.data?.totalMentions ?? 0) > 0 ? (
                    <span
                      aria-hidden="true"
                      className="ms-1.5 rounded-full bg-destructive px-1.5 text-xs font-bold text-destructive-foreground tabular-nums"
                    >
                      {unreadBadge(unread.data?.totalMentions ?? 0)}
                    </span>
                  ) : null}
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
              {panel.kind === "details" && conversation ? (
                <ChatConversationDetails
                  conversation={conversation}
                  viewerId={viewerId}
                  nameOf={nameOf}
                  copy={copy}
                  locale={locale}
                  blocked={counterpartBlocked}
                  presenceOf={ephemeral ? presenceOf : undefined}
                  busy={visibility.isPending || leaver.isPending}
                  onBlock={() =>
                    counterpartId &&
                    setBlockTarget({ userId: counterpartId, blocked: true })
                  }
                  onUnblock={() =>
                    counterpartId &&
                    setBlockTarget({ userId: counterpartId, blocked: false })
                  }
                  onAddPeople={() => {
                    setAddPeopleError(null);
                    setAddPeopleOpen(true);
                  }}
                  onLeave={() => setLeaveOpen(true)}
                  onHide={() => handleVisibility(true)}
                />
              ) : null}
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
                  mentionBase={{
                    selfId: viewerId,
                    people,
                    channels: mentionChannels,
                  }}
                  onClose={() => setPanel(null)}
                  onReact={handleReact}
                  onTogglePin={handleTogglePin}
                  attachmentsReadOnly={channel?.isArchived === true}
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
              {panel.kind === "mentions" ? (
                <MentionsPanel
                  rows={unread.data?.spaces ?? []}
                  copy={copy}
                  nameOf={(row) =>
                    conversationsQuery.data?.some(
                      (entry) => entry.id === row.spaceId,
                    )
                      ? conversationTitle(
                          conversationsQuery.data.find(
                            (entry) => entry.id === row.spaceId,
                          )!,
                          viewerId,
                          nameOf,
                        )
                      : channelNameOf(row.spaceId) || row.name
                  }
                  onOpen={(spaceId) => {
                    const isConversation = conversationsQuery.data?.some(
                      (entry) => entry.id === spaceId,
                    );
                    if (isConversation) onOpenConversation?.(spaceId);
                    else onOpenChannel(spaceId);
                  }}
                />
              ) : null}
              {panel.kind === "following" ? (
                <FollowedThreadsPanel
                  hooks={hooks}
                  workspaceId={ws}
                  copy={copy}
                  authorName={authorName}
                  channelName={channelNameOf}
                  onOpenThread={(rootId) =>
                    setPanel({ kind: "thread", rootId })
                  }
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
              initialSlowMode={channel.slowModeSeconds ?? 0}
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
          {offersConversations ? (
            <ChatNewMessageDialog
              open={newMessageOpen}
              busy={starter.isPending}
              people={pickable}
              error={newMessageError}
              copy={copy}
              onOpenChange={setNewMessageOpen}
              onSubmit={handleStart}
            />
          ) : null}
          {conversation?.kind === "group" ? (
            <>
              <ChatAddPeopleDialog
                open={addPeopleOpen}
                busy={participantAdder.isPending}
                candidates={addCandidates}
                roomLeft={groupRoomLeft(conversation.participants.length)}
                error={addPeopleError}
                copy={copy}
                onOpenChange={setAddPeopleOpen}
                onSubmit={handleAddPeople}
              />
              <ConfirmDialog
                open={leaveOpen}
                title={copy.dm.details.leaveTitle}
                body={copy.dm.details.leaveBody}
                confirmLabel={copy.dm.details.leaveConfirm}
                cancelLabel={copy.dm.details.cancel}
                destructive
                busy={leaver.isPending}
                onOpenChange={setLeaveOpen}
                onConfirm={handleLeave}
              />
            </>
          ) : null}
          {blockTarget ? (
            <ConfirmDialog
              open
              title={fillCopy(
                blockTarget.blocked
                  ? copy.dm.details.blockTitle
                  : copy.dm.details.unblockTitle,
                { name: nameOf(blockTarget.userId) },
              )}
              body={
                blockTarget.blocked
                  ? copy.dm.details.blockBody
                  : copy.dm.details.unblockBody
              }
              confirmLabel={
                blockTarget.blocked
                  ? copy.dm.details.blockConfirm
                  : copy.dm.details.unblockConfirm
              }
              cancelLabel={copy.dm.details.cancel}
              destructive={blockTarget.blocked}
              busy={blocker.isPending}
              onOpenChange={(open) => {
                if (!open) setBlockTarget(null);
              }}
              onConfirm={handleBlockConfirm}
            />
          ) : null}
          {channel ? (
            <ChatPermissionsDialog
              open={permissionsOpen}
              copy={copy}
              permissions={permissionsQuery.data}
              loading={permissionsQuery.isPending}
              loadFailed={permissionsQuery.isError}
              busy={permissionsWriter.isPending}
              error={permissionsError}
              onOpenChange={setPermissionsOpen}
              onRetry={() => void permissionsQuery.refetch()}
              onSave={handleSavePermissions}
            />
          ) : null}
          <ChatSearchPanel
            open={searchOpen}
            onOpenChange={setSearchOpen}
            hooks={hooks}
            workspaceId={ws}
            copy={copy}
            locale={locale}
            timeZone={timeZone}
            spaces={searchSpaces}
            people={people}
            authorName={authorName}
            onOpenHit={openSearchHit}
          />
          <ChatAdminSettings
            open={adminOpen}
            onOpenChange={setAdminOpen}
            hooks={hooks}
            workspaceId={ws}
            viewerRole={viewerRole}
            people={people}
            locale={locale}
            copy={copy}
          />
          <ModerateMessageDialog
            open={moderateTarget !== null}
            busy={moderator.isPending}
            error={moderateError}
            copy={copy}
            onOpenChange={(open) => {
              if (!open) setModerateTarget(null);
            }}
            onSubmit={handleModerate}
          />
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
      </ChatHighlightProvider>
    </ChatMentionProvider>
  );
}
