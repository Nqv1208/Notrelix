import { toast } from "sonner";
import {
  useCallback,
  useEffect,
  useMemo,
  useRef,
  useState,
  type RefObject,
} from "react";
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
  Bell,
  BellOff,
  EyeOff,
  Hash,
  Lock,
  MessageSquareText,
  Phone,
  PhoneOff,
  Plus,
  Search,
  Video,
  WifiOff,
} from "lucide-react";
import {
  chatFailureOf,
  type ChatFailureKind,
} from "../../core/api/chat-failure";
import { chatQueryKeys } from "../../query/keys";
import {
  checkMessageContent,
  type PendingMessage,
} from "../../core/model/chat-model";
import { messageActionGrants } from "../../core/model/message-display-model";
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
  type ChatLinkKind,
  type ChatMentionPayload,
  type ChatConversation,
  type ChatStartConversationInput,
  type ChatSearchHit,
  type ChatMessage,
  type ChatPermissionEntry,
  type ChatNotificationLevel,
} from "../../core/types/chat";
import { cn } from "@notrelix/ui-web";
import { ReplyChip } from "./chat-quote";
import { ChatThreadPanel } from "./chat-thread-panel";
import { PinnedBar } from "./chat-pins";
import { PinLimitBanner, UnpinDialog } from "./chat-pin-ui";
import { ChatInfoPanelHost } from "./chat-info-panel-host";
import type { ChatInfoAction, ChatInfoSectionKey } from "./chat-info-panel";
import {
  ChatChannelMemberRows,
  ChatInviteDialog,
} from "./chat-channel-members";
import { ChatMentionProvider } from "./chat-mention-context";
import type { QuoteView } from "./chat-message-row";
import { createChatHooks, type ChatHookDeps } from "../query/create-chat-hooks";
import type { ChatCopy } from "./chat.copy";
import {
  ChatComposer,
  type ChatComposerMentions,
  type ChatComposerProps,
} from "./chat-composer";
import { ChatMessageList, type ChatListStatus } from "./chat-message-list";
import {
  ChatCallIncoming,
  ChatCallStage,
  ChatCallStrip,
} from "../calls/chat-call-surfaces";
import { useChatCalls, type ChatCallError } from "../calls/use-chat-calls";

const CALL_ERROR_COPY: Record<ChatCallError, keyof ChatCopy["calls"]> = {
  unavailable: "unavailable",
  "start-failed": "startFailed",
  "you-busy": "youBusy",
  "already-active": "alreadyActive",
  full: "full",
};
import { ChatHeader } from "./chat-header";
import { ChatHighlightProvider } from "./chat-highlight-context";
import { useChannelTimeline } from "./use-channel-timeline";
import { ChatMessageLinkProvider } from "./chat-link-context";
import { ChatSearchPanel, type ChatSearchSpace } from "./chat-search-panel";
import { ChatStatusBanner } from "./chat-status-banner";
import { ChatRateLimitNotice } from "./chat-rate-limit";
import {
  ChatSidebar,
  type ChannelListStatus,
  type ChannelUnread,
} from "./chat-sidebar";
import {
  ConfirmDialog,
  CreateChannelDialog,
  DeleteChannelDialog,
  ModerateMessageDialog,
  RenameChannelDialog,
  type CreateChannelValues,
} from "./chat-dialogs";
import { ChatPermissionsDialog } from "./chat-permissions-dialog";
import { ChatLinkDialog } from "./chat-resource-link";
import { ChatAdminSettings } from "./chat-admin-settings-host";
import { ChatBlockedPeopleDialog } from "./chat-blocked-people";
import { ChatNewMessageDialog } from "./chat-new-message-dialog";
import {
  ChatAddPeopleDialog,
  ChatConversationHeader,
  ConversationInfoFooter,
  ConversationMemberRows,
  conversationSubtitle,
} from "./chat-conversation-view";
import { ConversationAvatar } from "./chat-conversation-list";
import type { ChatCallKind } from "./chat-header-actions";
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
  /**
   * Message links (UC-CHT-B10). `buildMessageLink` returns the address of a message of the open space (the host owns the routes);
   * without it no Copy link entry is shown. `linkedSequence` is the `?m=` of the address the page was opened with: the history
   * opens around it and the message is highlighted.
   */
  readonly buildMessageLink?: (target: {
    readonly spaceId: string;
    readonly conversation: boolean;
    readonly sequence: number;
  }) => string;
  readonly linkedSequence?: number | null;
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

/** The right-hand panel: one thread, or the info panel of the channel or conversation. */
type SidePanel =
  | { readonly kind: "thread"; readonly rootId: string }
  | {
      readonly kind: "details";
      /** Opens the info panel scrolled to this section, expanded; `seq` repeats the request. */
      readonly section?: ChatInfoSectionKey;
      readonly seq?: number;
    };

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
    case "last-manager":
      return copy.membership.lastManager;
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
/** The height of an element, kept current with a ResizeObserver (a multi-line composer grows it). */
function useElementHeight(): [(element: HTMLElement | null) => void, number] {
  const [height, setHeight] = useState(0);
  const observer = useRef<ResizeObserver | null>(null);
  const ref = useCallback((element: HTMLElement | null) => {
    observer.current?.disconnect();
    observer.current = null;
    if (!element) {
      setHeight(0);
      return;
    }
    const read = () =>
      setHeight(Math.ceil(element.getBoundingClientRect().height));
    read();
    if (typeof ResizeObserver === "undefined") return;
    observer.current = new ResizeObserver(read);
    observer.current.observe(element);
  }, []);
  return [ref, height];
}

/**
 * The channel composer with its draft. The text lives here, not in the workspace, so a keystroke re-renders the
 * composer only and not the message list. Drafts are kept per channel while the page is open (UC-CHT-B07).
 */
function ChatDraftComposer({
  draftKey,
  drafts,
  onSend,
  onTypingStop,
  ...rest
}: Omit<ChatComposerProps, "value" | "onChange"> & {
  readonly draftKey: string;
  readonly drafts: RefObject<Record<string, string>>;
  /** The caller stopped typing: the draft emptied, was sent, or lost the focus. */
  readonly onTypingStop?: () => void;
}) {
  const [value, setValue] = useState(drafts.current[draftKey] ?? "");
  function change(next: string) {
    setValue(next);
    if (next.length === 0) delete drafts.current[draftKey];
    else drafts.current[draftKey] = next;
    if (next.trim().length === 0) onTypingStop?.();
  }
  return (
    <div className="contents" onBlur={() => onTypingStop?.()}>
      <ChatComposer
        {...rest}
        value={value}
        onChange={change}
        onSend={(...args) => {
          onSend(...args);
          change("");
        }}
      />
    </div>
  );
}

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
  buildMessageLink,
  linkedSequence = null,
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
  // Calls: the dock below renders them; the header and info panel buttons start one in the open conversation.
  const calls = useChatCalls({ deps, workspaceId });
  const offersConversations = onOpenConversation !== undefined;
  const isGuest = viewerRole === "guest";
  const canBrowse = viewerRole !== null && !isGuest;
  const canManageWorkspace = viewerRole === "owner" || viewerRole === "admin";

  const [permissionsOpen, setPermissionsOpen] = useState(false);
  const [linkOpen, setLinkOpen] = useState(false);
  const [linkError, setLinkError] = useState<string | null>(null);
  const [adminOpen, setAdminOpen] = useState(false);
  const [searchOpen, setSearchOpen] = useState(false);
  const [searchSeed, setSearchSeed] = useState("");

  // ─── Server state ─────────────────────────────────────────────────────────
  const channels = hooks.useChannels(ws);
  const browse = hooks.useBrowseChannels(ws, canBrowse);
  const unread = hooks.useUnreadSummary(ws);
  const detail = hooks.useChannel(
    ws,
    conversationId ? undefined : (channelId ?? undefined),
  );
  const conversationsQuery = hooks.useConversations(ws);
  const channelGroups = hooks.useChannelGroups(ws);
  const groupNames = useMemo(
    () => new Map((channelGroups.data ?? []).map((g) => [g.id, g.name])),
    [channelGroups.data],
  );
  const blockedQuery = hooks.useBlockedPeople(ws);
  const blockedPeople = blockedQuery.data;
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
  // One-to-one calls only: a direct conversation, for a person who is not a Guest, until the server says calls are off.
  const callsEnabled =
    calls.enabled &&
    !isGuest &&
    conversation?.kind === "direct" &&
    channelId !== null;
  const onStartCall = (kind: ChatCallKind) => {
    if (channelId) calls.start(channelId, kind);
  };

  const timeline = useChannelTimeline({
    hooks,
    workspaceId: ws,
    channelId,
    linkedSequence,
    unreadSettled: unread.isFetched || unread.isError,
    firstUnreadSequence: unreadRow?.firstUnreadSequence ?? null,
    timeZone,
    // The panel state is declared below; the hook only calls this from an effect, after the first render.
    onShowThread: (rootId) => setPanel({ kind: "thread", rootId }),
  });
  const { messages, flat, pendingAll, pendingHere, rows, hasNewer } = timeline;
  // Sends the server answered with 429: held as they are, retried when the wait is over (UC-CHT-H08).
  const limitedEntries = useMemo(
    () =>
      pendingHere.filter(
        (entry) => entry.status === "failed" && entry.limit != null,
      ),
    [pendingHere],
  );
  // ─── Writes ───────────────────────────────────────────────────────────────
  const sender = hooks.useSendMessage(ws, ch, viewerId);
  const channelAdder = hooks.useAddMembers(ws, ch);
  const editor = hooks.useEditMessage(ws, ch);
  const deleter = hooks.useDeleteMessage(ws, ch);
  const moderator = hooks.useModerateMessage(ws, ch);
  const permissionsWriter = hooks.useSetChannelPermissions(ws, ch);
  const linker = hooks.useLinkChannel(ws, ch);
  const unlinker = hooks.useUnlinkChannel(ws, ch);
  const allReader = hooks.useMarkAllRead(ws);
  const levelWriter = hooks.useSetNotificationLevel(ws);
  const starter = hooks.useStartConversation(ws);
  const participantAdder = hooks.useAddParticipants(ws, ch);
  const leaver = hooks.useLeaveConversation(ws);
  const visibility = hooks.useSetConversationVisibility(ws);
  const blocker = hooks.useBlockPerson(ws);
  const creator = hooks.useCreateChannel(ws);
  const updater = hooks.useUpdateChannel(ws, ch);
  const archiver = hooks.useArchiveChannel(ws, ch);
  const memberRemover = hooks.useRemoveMember(ws, ch, viewerId);
  const roleSetter = hooks.useSetMemberRole(ws, ch);
  const channelDeleter = hooks.useDeleteChannel(ws, ch);
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
  const drafts = useRef<Record<string, string>>({});
  const [dockRef, dockHeight] = useElementHeight();
  const [editing, setEditing] = useState<EditState | null>(null);
  const [confirmDelete, setConfirmDelete] = useState<ChatMessage | null>(null);
  const [confirmUnpin, setConfirmUnpin] = useState<ChatMessage | null>(null);
  const [unpinFailed, setUnpinFailed] = useState(false);
  const [pinLimit, setPinLimit] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);
  const [createOpen, setCreateOpen] = useState(false);
  const [createError, setCreateError] = useState<string | null>(null);
  const [renameOpen, setRenameOpen] = useState(false);
  const [renameError, setRenameError] = useState<string | null>(null);
  const [archiveOpen, setArchiveOpen] = useState(false);
  const [leaveChannelOpen, setLeaveChannelOpen] = useState(false);
  const [removeTarget, setRemoveTarget] = useState<string | null>(null);
  const [inviteOpen, setInviteOpen] = useState(false);
  const [inviteError, setInviteError] = useState<string | null>(null);
  const [deleteOpen, setDeleteOpen] = useState(false);
  const [deleteError, setDeleteError] = useState<string | null>(null);
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
  const [blockedOpen, setBlockedOpen] = useState(false);
  const [blockTarget, setBlockTarget] = useState<{
    readonly userId: string;
    readonly blocked: boolean;
  } | null>(null);
  const [replyTo, setReplyTo] = useState<ChatMessage | null>(null);
  const [panel, setPanel] = useState<SidePanel | null>(null);
  const messageLink = useMemo(
    () =>
      buildMessageLink
        ? (message: ChatMessage): string | null =>
            message.threadRootId && !message.alsoSentToChannel
              ? null
              : buildMessageLink({
                  spaceId: message.spaceId,
                  conversation: conversationId !== null,
                  sequence: message.sequence,
                })
        : null,
    [buildMessageLink, conversationId],
  );

  function openSearchHit(hit: ChatSearchHit) {
    timeline.openMessage({
      spaceId: hit.spaceId,
      messageId: hit.messageId,
      sequence: hit.sequence,
      threadRootId: hit.alsoSentToChannel ? null : hit.threadRootId,
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
  // What the message actions offer here: archived is read-only, and the server's capabilities decide the rest.
  const grants = messageActionGrants({
    archived: channel?.isArchived === true,
    postRefused: composerDisabledReason !== null,
    canReact: channel?.capabilities?.canReact,
    canPin: channel?.capabilities?.canPin,
    canModerate: channel?.capabilities?.canModerate,
  });
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
    .filter((entry) => !entry.isHidden)
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
  const typingSignal = useTypingSignal(ephemeral, channelId);

  // A reply and an open panel belong to one channel: both close when the channel changes.
  useEffect(() => {
    setReplyTo(null);
    setPanel(null);
  }, [channelId]);

  // A stored send keeps its bubble until a window shows the message (then the history's copy takes over) or the
  // channel is left; no timer. The cleanup reads the sender of the channel it ran for.
  useEffect(() => {
    if (!pendingAll.some((entry) => entry.storedMessageId !== undefined))
      return;
    sender.discardStored(new Set(flat.map((message) => message.id)));
  }, [flat, pendingAll]);
  useEffect(() => {
    const leaving = sender.discardStored;
    return () => leaving();
  }, [channelId]);

  function handleSend(
    content: string,
    payload?: ChatMentionPayload,
    files?: readonly File[],
  ) {
    if (!channelId) return;
    sender.send(content, replyTo?.id ?? null, payload, files);
    setReplyTo(null);
    timeline.afterSend();
  }

  function handleReact(message: ChatMessage, emoji: string, add: boolean) {
    reactor.mutate(
      { messageId: message.id, kind: emoji, add },
      { onError: () => setNotice(copy.reactions.failed) },
    );
  }

  function handleTogglePin(message: ChatMessage, pinned: boolean) {
    // Unpinning is announced to everyone, so it asks first (Chat-Pins boards).
    if (!pinned) {
      setUnpinFailed(false);
      setConfirmUnpin(message);
      return;
    }
    pinner.mutate(
      { messageId: message.id, pinned },
      {
        onError: (error) =>
          chatFailureOf(error).code === "limit-reached"
            ? setPinLimit(ch)
            : setNotice(copy.pins.failed),
      },
    );
  }

  function handleUnpinConfirm() {
    if (!confirmUnpin) return;
    pinner.mutate(
      { messageId: confirmUnpin.id, pinned: false },
      {
        onSuccess: () => {
          setConfirmUnpin(null);
          setPinLimit(null);
        },
        onError: () => setUnpinFailed(true),
      },
    );
  }

  /** "Open message" of a pin: lands on the message like a search hit, and a phone leaves the Pins page. */
  function handleOpenPinned(message: ChatMessage) {
    timeline.openMessage({
      spaceId: message.spaceId,
      messageId: message.id,
      sequence: message.sequence,
      threadRootId: message.alsoSentToChannel
        ? null
        : (message.threadRootId ?? null),
    });
    if (isMobile) setPanel(null);
  }

  function handleOpenThread(message: ChatMessage) {
    setPanel({ kind: "thread", rootId: message.threadRootId ?? message.id });
  }

  function openSection(section: ChatInfoSectionKey) {
    setPanel((current) => ({
      kind: "details",
      section,
      seq: (current?.kind === "details" ? (current.seq ?? 0) : 0) + 1,
    }));
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
        authorId: target.authorId,
      };
    }
    if (message.replyPreview) {
      return {
        name: authorName(message.replyPreview.authorId),
        text: message.replyPreview.content,
        deleted: message.replyPreview.deleted,
        unavailable: false,
        authorId: message.replyPreview.authorId,
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

  function handleLink(target: { kind: ChatLinkKind; resourceId: string }) {
    if (!channel) return;
    setLinkError(null);
    linker.mutate(
      { ...target, expectedVersion: channel.version },
      {
        onSuccess: () => {
          setLinkOpen(false);
          setNotice(copy.resourceLink.saved);
        },
        onError: (error) => {
          const kind = chatFailureOf(error).kind;
          setLinkError(
            kind === "stale"
              ? copy.resourceLink.dialog.stale
              : kind === "forbidden"
                ? copy.resourceLink.dialog.forbidden
                : copy.resourceLink.dialog.failed,
          );
        },
      },
    );
  }

  function handleUnlink() {
    if (!channel) return;
    unlinker.mutate(channel.version, {
      onSuccess: () => setNotice(copy.resourceLink.unlinked),
      onError: (error) =>
        setNotice(
          chatFailureOf(error).kind === "stale"
            ? copy.resourceLink.unlinkStale
            : copy.resourceLink.unlinkFailed,
        ),
    });
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

  /** Leaves the open channel for another one, or for the bare chat page when there is none. */
  function closeChannel() {
    const other = channels.data?.find(
      (entry) => entry.id !== ch && entry.isMember,
    );
    if (other) onOpenChannel(other.id);
    else (onCloseConversation ?? onBackToList)?.();
  }

  function handleLeaveChannel() {
    if (!channel) return;
    const wasPrivate = channel.kind === "channel_private";
    memberRemover.mutate(viewerId, {
      onSuccess: () => {
        setLeaveChannelOpen(false);
        // A public channel stays readable (the header offers Join again); a private one is gone for the caller.
        if (wasPrivate) closeChannel();
      },
      onError: (error) => {
        setLeaveChannelOpen(false);
        setNotice(failureText(chatFailureOf(error).kind, copy));
      },
    });
  }

  function handleInvite(userIds: readonly string[]) {
    setInviteError(null);
    channelAdder.mutate(userIds, {
      onSuccess: (result) => {
        setInviteOpen(false);
        if (result.skipped.length > 0) {
          setNotice(
            pluralCopy(
              {
                one: copy.membership.inviteSkipped_one,
                other: copy.membership.inviteSkipped_other,
              },
              result.skipped.length,
            ),
          );
        }
      },
      onError: () => setInviteError(copy.membership.inviteFailed),
    });
  }

  function handleRemoveMember() {
    if (removeTarget === null) return;
    memberRemover.mutate(removeTarget, {
      onSuccess: () => setRemoveTarget(null),
      onError: (error) => {
        setRemoveTarget(null);
        setNotice(failureText(chatFailureOf(error).kind, copy));
      },
    });
  }

  function handleDeleteChannel() {
    if (!channel) return;
    setDeleteError(null);
    channelDeleter.mutate(channel.version, {
      onSuccess: () => {
        setDeleteOpen(false);
        closeChannel();
      },
      onError: (error) =>
        setDeleteError(failureText(chatFailureOf(error).kind, copy)),
    });
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
  // The call takes the pane of its own conversation; "back to the conversation" hides it and a bar in the pane's flow remains.
  const [minimizedCallId, setMinimizedCallId] = useState<string | null>(null);
  const heldCall = calls.current;
  const callSpaceOpen =
    heldCall !== null && channelId === heldCall.call.spaceId;
  const stageVisible =
    callSpaceOpen &&
    (heldCall.call.state === "ended" || minimizedCallId !== heldCall.call.id);
  const callPeople = {
    viewerId,
    nameOf,
    peerOf: (spaceId: string) =>
      conversationsQuery.data
        ?.find((entry) => entry.id === spaceId)
        ?.participants.find((p) => p.userId !== viewerId)?.userId ?? null,
  };
  const callStrip =
    heldCall && !stageVisible ? (
      <ChatCallStrip
        controller={calls}
        held={heldCall}
        copy={copy}
        onReturn={() => {
          setMinimizedCallId(null);
          // A one-to-one call lives in a conversation, which has its own route.
          if (
            conversationsQuery.data?.some(
              (entry) => entry.id === heldCall.call.spaceId,
            )
          )
            onOpenConversation?.(heldCall.call.spaceId);
          else onOpenChannel(heldCall.call.spaceId);
        }}
        {...callPeople}
      />
    ) : null;
  const callIncoming = (
    <ChatCallIncoming
      controller={calls}
      nameOf={nameOf}
      isPhone={isMobile}
      copy={copy}
    />
  );
  const callError = calls.error ? (
    <ChatStatusBanner
      tone="warning"
      icon={<PhoneOff />}
      action={
        <Button
          type="button"
          size="sm"
          variant="outline"
          onClick={calls.dismiss}
        >
          {copy.calls.close}
        </Button>
      }
    >
      {copy.calls[CALL_ERROR_COPY[calls.error]]}
    </ChatStatusBanner>
  ) : null;
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

  const insideChannel = new Set(members.map((member) => member.userId));
  const inviteCandidates = pickable.filter(
    (person) => !insideChannel.has(person.id),
  );

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
  // An own send shows its bubble at once, even while the first window is still loading.
  const listStatus: ChatListStatus =
    detailFailure === "not-found" || detailFailure === "forbidden"
      ? "no-access"
      : messagesFailure === "not-found" || messagesFailure === "forbidden"
        ? "no-access"
        : !channelId || (messages.isPending && pendingHere.length === 0)
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

  // On a narrow screen with no channel open, the list is the page. A person with direct messages and no channel needs it
  // too: it is where they open a conversation and start a new one.
  const listOnly =
    isMobile &&
    channelId === null &&
    ((channels.data?.length ?? 0) > 0 || offersConversations);
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
        // The open conversation shows its own typing bubble; the others show it on their sidebar row.
        typingOf: ephemeral
          ? (id) =>
              id === (conversationId ?? channelId)
                ? []
                : ephemeralStores.typing
                    .typingIn(id)
                    .filter((who) => who !== viewerId)
          : undefined,
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
        blockedCount: blockedPeople?.length ?? 0,
        onOpenBlocked: () => setBlockedOpen(true),
      }
    : null;
  const spaceName = conversation
    ? conversationTitle(conversation, viewerId, nameOf)
    : (channel?.name ?? "");
  const panelSpaceLabel = conversation ? spaceName : `# ${spaceName}`;
  // The round quick actions of the info panel; the call ones exist only behind the calls flag, and Mute only for a channel member.
  const muted = channel?.notificationLevel === "none";
  const infoActions: ChatInfoAction[] = [
    ...(callsEnabled && onStartCall
      ? [
          {
            key: "voice",
            label: copy.panel.voiceCall,
            icon: Phone,
            onClick: () => onStartCall("voice"),
          },
          {
            key: "video",
            label: copy.panel.videoCall,
            icon: Video,
            onClick: () => onStartCall("video"),
          },
        ]
      : []),
    {
      key: "search",
      label: copy.panel.searchShort,
      icon: Search,
      onClick: () => {
        setSearchSeed("");
        setSearchOpen(true);
      },
    },
    ...(channel?.isMember
      ? [
          {
            key: "mute",
            label: muted ? copy.panel.unmute : copy.panel.mute,
            icon: muted ? Bell : BellOff,
            onClick: () => handleLevel(muted ? "all" : "none"),
          },
        ]
      : []),
  ];
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
      groupNames={groupNames}
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
      onSearch={(query) => {
        setSearchSeed(query);
        setSearchOpen(true);
      }}
      onMarkAllRead={() =>
        allReader.mutate(undefined, {
          // The unread counts stay as they were and the button stays usable for a retry.
          onError: () =>
            toast.error(copy.sidebar.markAllReadFailed, {
              id: "chat-mark-all-read",
            }),
        })
      }
      markingAllRead={allReader.isPending}
      onCreate={() => {
        setCreateError(null);
        setCreateOpen(true);
      }}
      onJoin={(id) => handleJoin(id, () => onOpenChannel(id))}
      onRetry={() => void channels.refetch()}
    />
  );

  return (
    <ChatMentionProvider value={mentionContext}>
      <ChatMessageLinkProvider value={messageLink}>
        <ChatHighlightProvider value={timeline.highlightId}>
          <div className="relative flex h-full min-h-0 w-full">
            {listOnly ? (
              <div className="flex min-h-0 w-full flex-col bg-sidebar">
                {sidebar}
                {callError}
                {callStrip}
                {callIncoming}
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
              <aside
                className={cn(
                  "flex w-64 shrink-0 flex-col border-e border-border bg-sidebar xl:w-72",
                  // Below 1280 the open side panel needs the room: the channel column would shrink to a sliver.
                  channelId && (channel || conversation) && panel !== null
                    ? "max-xl:hidden"
                    : null,
                )}
              >
                {sidebar}
              </aside>
            )}

            {listOnly ? null : (
              <section
                className={cn(
                  "relative flex min-h-0 min-w-0 flex-1 flex-col",
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
                        onSearch={() => {
                          setSearchSeed("");
                          setSearchOpen(true);
                        }}
                        callsEnabled={callsEnabled}
                        onStartCall={onStartCall}
                        onOpenSection={openSection}
                      />
                    ) : channel ? (
                      <ChatHeader
                        channel={channel}
                        canManage={canManage}
                        joining={joiner.isPending}
                        copy={copy}
                        onOpenList={openList}
                        onRename={() => {
                          setRenameError(null);
                          setRenameOpen(true);
                        }}
                        onToggleArchive={() => setArchiveOpen(true)}
                        onLeave={
                          channel.isMember
                            ? () => setLeaveChannelOpen(true)
                            : undefined
                        }
                        onDelete={
                          canManage
                            ? () => {
                                setDeleteError(null);
                                setDeleteOpen(true);
                              }
                            : undefined
                        }
                        canManagePermissions={
                          channel.capabilities?.canManagePermissions ?? false
                        }
                        onPermissions={() => {
                          setPermissionsError(null);
                          setPermissionsOpen(true);
                        }}
                        workspaceId={ws}
                        onLinkResource={
                          canManage && !channel.isArchived
                            ? () => {
                                setLinkError(null);
                                setLinkOpen(true);
                              }
                            : undefined
                        }
                        onUnlinkResource={
                          canManage && !channel.isArchived
                            ? handleUnlink
                            : undefined
                        }
                        onJoin={() => handleJoin(ch)}
                        onSearch={() => {
                          setSearchSeed("");
                          setSearchOpen(true);
                        }}
                        callsEnabled={callsEnabled}
                        onStartCall={onStartCall}
                        infoOpen={panel?.kind === "details"}
                        onToggleInfo={() => togglePanel({ kind: "details" })}
                        onOpenSection={openSection}
                        onSetLevel={handleLevel}
                      />
                    ) : null}
                    {callError}
                    {callStrip}
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
                    {pinLimit === ch ? (
                      <PinLimitBanner
                        copy={copy}
                        onView={() => openSection("pins")}
                      />
                    ) : null}
                    <PinnedBar
                      copy={copy}
                      count={pinCount}
                      newest={pinnedNewest}
                      newestAuthor={
                        pinnedNewest ? authorName(pinnedNewest.authorId) : ""
                      }
                      onOpen={() => openSection("pins")}
                    />
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
                      scrollRequest={timeline.scrollRequest}
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
                      onAtBottomChange={timeline.setAtBottom}
                      onJumpLatest={timeline.jumpToLatest}
                      onEdit={grants.editOwn ? handleEditStart : undefined}
                      onEditDraftChange={(value) =>
                        setEditing((current) =>
                          current ? { ...current, draft: value } : current,
                        )
                      }
                      onEditSave={handleEditSave}
                      onEditCancel={() => setEditing(null)}
                      onDelete={
                        grants.deleteOwn
                          ? (message) => setConfirmDelete(message)
                          : undefined
                      }
                      onModerate={
                        grants.moderate
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
                      onReact={grants.react ? handleReact : undefined}
                      onReply={
                        grants.reply
                          ? (message) => setReplyTo(message)
                          : undefined
                      }
                      onOpenThread={handleOpenThread}
                      onTogglePin={grants.pin ? handleTogglePin : undefined}
                      attachmentsReadOnly={channel?.isArchived === true}
                      bottomInset={dockHeight}
                      showNames={conversation?.kind !== "direct"}
                      typingIds={
                        ephemeral
                          ? typingIds.filter((id) => id !== viewerId)
                          : undefined
                      }
                      onOpenQuote={(id) =>
                        document
                          .getElementById(`message-${id}`)
                          ?.scrollIntoView({ block: "center" })
                      }
                    />
                    <div
                      ref={dockRef}
                      className="pointer-events-none absolute inset-x-0 bottom-0 z-10 flex flex-col"
                      data-testid="chat-composer-dock"
                    >
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
                            for (const entry of limitedEntries)
                              sender.retry(entry);
                          }}
                        />
                      ) : null}
                      <ChatDraftComposer
                        floating
                        key={ch}
                        draftKey={ch}
                        drafts={drafts}
                        channelName={spaceName}
                        labelOverride={
                          conversation
                            ? fillCopy(copy.dm.view.composer, {
                                name: spaceName,
                              })
                            : undefined
                        }
                        disabledReason={composerDisabledReason}
                        copy={copy}
                        onTyping={ephemeral ? typingSignal.start : undefined}
                        onTypingStop={ephemeral ? typingSignal.stop : undefined}
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
                    </div>
                  </>
                )}
                {heldCall ? (
                  <ChatCallStage
                    controller={calls}
                    held={heldCall}
                    visible={stageVisible}
                    onMinimize={() => setMinimizedCallId(heldCall.call.id)}
                    copy={copy}
                    {...callPeople}
                  />
                ) : null}
                {callIncoming}
              </section>
            )}

            {channelId && (channel || conversation) && panel !== null ? (
              <aside
                className={cn(
                  "flex min-h-0 w-full flex-col border-border bg-background md:w-[340px] md:border-s md:shrink-0 xl:w-[360px]",
                  // On a tablet (below 1024) the room would be left about 180 px wide beside the shell and the details, so a
                  // conversation's details slide over it as a sheet. From 1024 up they sit beside the room, which shrinks to
                  // what is left: messages are never drawn under the panel.
                  panel.kind === "details" &&
                    "md:max-lg:absolute md:max-lg:inset-y-0 md:max-lg:end-0 md:max-lg:z-20 md:max-lg:shadow-lg",
                )}
                aria-label={copy.panel.label}
                data-testid="chat-side-panel"
              >
                {panel.kind === "details" ? (
                  <ChatInfoPanelHost
                    hooks={hooks}
                    workspaceId={ws}
                    channelId={ch}
                    avatar={
                      conversation ? (
                        <ConversationAvatar
                          title={spaceName}
                          id={counterpartId ?? conversation.id}
                          group={conversation.kind === "group"}
                          className="size-[72px] text-2xl"
                        />
                      ) : (
                        <span className="flex size-[72px] items-center justify-center rounded-[20px] bg-primary/10 text-primary">
                          {channel?.kind === "channel_private" ? (
                            <Lock className="size-8" aria-hidden="true" />
                          ) : (
                            <Hash className="size-8" aria-hidden="true" />
                          )}
                        </span>
                      )
                    }
                    title={spaceName}
                    subtitle={
                      conversation
                        ? counterpartBlocked
                          ? copy.dm.details.blockedBadge
                          : conversationSubtitle(
                              conversation,
                              copy,
                              counterpartId && ephemeral
                                ? presenceOf(counterpartId)
                                : undefined,
                            )
                        : fillCopy(
                            channel?.kind === "channel_private"
                              ? copy.panel.privateSubtitle
                              : copy.panel.publicSubtitle,
                            { count: members.length },
                          )
                    }
                    regionLabel={
                      conversation
                        ? conversation.kind === "group"
                          ? copy.dm.details.groupTitle
                          : copy.dm.details.dmTitle
                        : copy.panel.channelInfo
                    }
                    closeLabel={copy.pins.close}
                    actions={infoActions}
                    focus={
                      panel.section
                        ? { key: panel.section, seq: panel.seq ?? 0 }
                        : undefined
                    }
                    footer={
                      conversation ? (
                        <ConversationInfoFooter
                          conversation={conversation}
                          viewerId={viewerId}
                          nameOf={nameOf}
                          copy={copy}
                          locale={locale}
                          blocked={counterpartBlocked}
                          busy={visibility.isPending || leaver.isPending}
                          onBlock={() =>
                            counterpartId &&
                            setBlockTarget({
                              userId: counterpartId,
                              blocked: true,
                            })
                          }
                          onUnblock={() =>
                            counterpartId &&
                            setBlockTarget({
                              userId: counterpartId,
                              blocked: false,
                            })
                          }
                          onAddPeople={() => {
                            setAddPeopleError(null);
                            setAddPeopleOpen(true);
                          }}
                          onLeave={() => setLeaveOpen(true)}
                          onHide={() => handleVisibility(true)}
                        />
                      ) : undefined
                    }
                    members={
                      conversation
                        ? conversation.kind === "group"
                          ? {
                              count: conversation.participants.length,
                              content: (
                                <ConversationMemberRows
                                  conversation={conversation}
                                  viewerId={viewerId}
                                  nameOf={nameOf}
                                  copy={copy}
                                  presenceOf={
                                    ephemeral ? presenceOf : undefined
                                  }
                                />
                              ),
                            }
                          : undefined
                        : {
                            count: members.length,
                            content: (
                              <ChatChannelMemberRows
                                members={members}
                                viewerId={viewerId}
                                authorName={authorName}
                                presenceOf={ephemeral ? presenceOf : undefined}
                                copy={copy}
                                onRemoveMember={
                                  canManage && !channel?.isArchived
                                    ? setRemoveTarget
                                    : undefined
                                }
                                onSetRole={
                                  canManage && !channel?.isArchived
                                    ? (userId, role) =>
                                        roleSetter.mutate(
                                          { userId, role },
                                          {
                                            onError: (error) =>
                                              setNotice(
                                                chatFailureOf(error).code ===
                                                  "last-manager"
                                                  ? copy.membership.lastManager
                                                  : copy.membership.roleFailed,
                                              ),
                                          },
                                        )
                                    : undefined
                                }
                                onInvite={
                                  canManage && !channel?.isArchived
                                    ? () => {
                                        setInviteError(null);
                                        setInviteOpen(true);
                                      }
                                    : undefined
                                }
                              />
                            ),
                          }
                    }
                    pins={{
                      copy,
                      locale,
                      timeZone,
                      authorName,
                      spaceLabel: panelSpaceLabel,
                      canUnpin: () => true,
                      unpinBusy: pinner.isPending,
                      onUnpin: (message) => handleTogglePin(message, false),
                      onOpenMessage: handleOpenPinned,
                    }}
                    following={{
                      copy,
                      authorName,
                      channelName: channelNameOf,
                      locale,
                      timeZone,
                      onOpenThread: (rootId) =>
                        setPanel({ kind: "thread", rootId }),
                    }}
                    mentions={{
                      rows: unread.data?.spaces ?? [],
                      copy,
                      nameOf: (row) =>
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
                          : channelNameOf(row.spaceId) || row.name,
                      onOpen: (spaceId) => {
                        const isConversation = conversationsQuery.data?.some(
                          (entry) => entry.id === spaceId,
                        );
                        if (isConversation) onOpenConversation?.(spaceId);
                        else onOpenChannel(spaceId);
                      },
                    }}
                    onClose={() => setPanel(null)}
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
                    spaceLabel={panelSpaceLabel}
                    mentionBase={{
                      selfId: viewerId,
                      people,
                      channels: mentionChannels,
                    }}
                    onClose={() => setPanel(null)}
                    onReact={grants.react ? handleReact : undefined}
                    onTogglePin={grants.pin ? handleTogglePin : undefined}
                    attachmentsReadOnly={channel?.isArchived === true}
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
            {channel ? (
              <>
                <ConfirmDialog
                  open={leaveChannelOpen}
                  title={copy.membership.leaveTitle}
                  body={
                    channel.kind === "channel_private"
                      ? copy.membership.leaveBodyPrivate
                      : copy.membership.leaveBodyPublic
                  }
                  confirmLabel={copy.membership.leaveConfirm}
                  cancelLabel={copy.membership.cancel}
                  destructive
                  busy={memberRemover.isPending}
                  onOpenChange={setLeaveChannelOpen}
                  onConfirm={handleLeaveChannel}
                />
                <ConfirmDialog
                  open={removeTarget !== null}
                  title={fillCopy(copy.membership.removeTitle, {
                    name: removeTarget ? authorName(removeTarget) : "",
                  })}
                  body={copy.membership.removeBody}
                  confirmLabel={copy.membership.removeConfirm}
                  cancelLabel={copy.membership.cancel}
                  destructive
                  busy={memberRemover.isPending}
                  onOpenChange={(next) => {
                    if (!next) setRemoveTarget(null);
                  }}
                  onConfirm={handleRemoveMember}
                />
                <ChatInviteDialog
                  open={inviteOpen}
                  busy={channelAdder.isPending}
                  channelName={channel?.name ?? ""}
                  candidates={inviteCandidates}
                  error={inviteError}
                  copy={copy}
                  onOpenChange={setInviteOpen}
                  onSubmit={handleInvite}
                />
                <DeleteChannelDialog
                  open={deleteOpen}
                  busy={channelDeleter.isPending}
                  channelName={channel.name}
                  error={deleteError}
                  copy={copy}
                  onOpenChange={setDeleteOpen}
                  onConfirm={handleDeleteChannel}
                />
              </>
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
            {channel ? (
              <ChatLinkDialog
                key={`${channel.id}:${linkOpen ? "open" : "closed"}`}
                open={linkOpen}
                copy={copy}
                workspaceId={ws}
                hooks={hooks}
                busy={linker.isPending}
                error={linkError}
                onOpenChange={setLinkOpen}
                onConfirm={handleLink}
              />
            ) : null}
            <ChatSearchPanel
              open={searchOpen}
              onOpenChange={(next) => {
                setSearchOpen(next);
                if (!next) setSearchSeed("");
              }}
              initialQuery={searchSeed}
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
            <ChatBlockedPeopleDialog
              open={blockedOpen}
              userIds={blockedPeople ?? []}
              status={
                blockedQuery.isError
                  ? "error"
                  : blockedQuery.isPending
                    ? "loading"
                    : "ready"
              }
              nameOf={nameOf}
              busyId={
                blocker.isPending ? (blocker.variables?.userId ?? null) : null
              }
              copy={copy}
              onOpenChange={setBlockedOpen}
              onUnblock={(userId) =>
                blocker.mutate(
                  { userId, blocked: false },
                  { onError: () => setNotice(copy.dm.blocked.unblockFailed) },
                )
              }
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
            <UnpinDialog
              open={confirmUnpin !== null}
              copy={copy}
              actorName={authorName(viewerId)}
              busy={pinner.isPending}
              failed={unpinFailed}
              onOpenChange={(open) => {
                if (!open) setConfirmUnpin(null);
              }}
              onConfirm={handleUnpinConfirm}
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
      </ChatMessageLinkProvider>
    </ChatMentionProvider>
  );
}
