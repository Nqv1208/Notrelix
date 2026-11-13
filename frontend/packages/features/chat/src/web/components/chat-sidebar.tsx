import {
  BellOff,
  Hash,
  Lock,
  Plus,
  RotateCw,
  Search,
  Settings,
} from "lucide-react";
import { Avatar, AvatarFallback, Button, Skeleton, cn } from "@notrelix/ui-web";
import type { ChatChannel } from "../../core/types/chat";
import type { ChatCopy } from "./chat.copy";
import { avatarToneOf, initialsOf } from "./chat-initials";
import { fillCopy, pluralCopy } from "./use-chat-copy";
import {
  ChatConversationSection,
  type ChatConversationListView,
} from "./chat-conversation-list";

export interface ChannelUnread {
  readonly count: number;
  readonly muted: boolean;
  /** Unread messages that mention the caller (UC-CHT-D04): a badge apart from the unread count. */
  readonly mentions?: number;
}

export type ChannelListStatus = "loading" | "ready" | "error";

/** The caller, shown in the footer when the host knows their name. */
export interface ChatSidebarViewer {
  readonly id: string;
  readonly name: string;
}

export interface ChatSidebarProps {
  readonly channels: readonly ChatChannel[];
  readonly status: ChannelListStatus;
  readonly activeChannelId: string | null;
  readonly unreadBySpace: ReadonlyMap<string, ChannelUnread>;
  /** Public channels the caller is not in; null hides the browse section. */
  readonly browse: {
    readonly channels: readonly ChatChannel[];
    readonly status: ChannelListStatus;
  } | null;
  readonly canCreate: boolean;
  /** A Guest sees only the channels they were added to; an empty list says so (UC-CHT-H02). */
  readonly guest?: boolean;
  readonly copy: ChatCopy;
  readonly onSelect: (channelId: string) => void;
  readonly onCreate: () => void;
  /** Opens the Chat admin settings (retention and export); only an Owner or an Admin is offered it. */
  readonly onOpenSettings?: () => void;
  /** Opens the message search (UC-CHT-H07). */
  readonly onSearch?: () => void;
  readonly onJoin: (channelId: string) => void;
  readonly onRetry: () => void;
  readonly joiningId: string | null;
  /** The caller; the footer is left out when it is null. */
  readonly viewer?: ChatSidebarViewer | null;
  /** The direct and group conversations section; left out when the host does not offer conversations. */
  readonly conversations?: ChatConversationListView | null;
}

/** A badge count: nothing for zero, 99+ above 99 (UC-CHT-D01). */
export function unreadBadge(count: number): string | null {
  if (!Number.isFinite(count) || count <= 0) return null;
  return count > 99 ? "99+" : String(Math.floor(count));
}

/** A run of channels under one heading. `groupId` is null for the channels outside any group. */
export interface ChannelGroup {
  readonly groupId: string | null;
  readonly channels: readonly ChatChannel[];
}

/**
 * Splits the caller's channels into the ungrouped ones (first) and one run per
 * channel group, in the order the list gives them.
 */
export function groupChannels(
  channels: readonly ChatChannel[],
): readonly ChannelGroup[] {
  const ungrouped: ChatChannel[] = [];
  const byGroup = new Map<string, ChatChannel[]>();
  for (const channel of channels) {
    if (!channel.groupId) {
      ungrouped.push(channel);
      continue;
    }
    const run = byGroup.get(channel.groupId);
    if (run) run.push(channel);
    else byGroup.set(channel.groupId, [channel]);
  }
  const groups: ChannelGroup[] = [];
  if (ungrouped.length > 0) groups.push({ groupId: null, channels: ungrouped });
  for (const [groupId, run] of byGroup) {
    groups.push({ groupId, channels: run });
  }
  return groups;
}

const HEADING =
  "px-2 pt-4 pb-1 text-xs font-semibold uppercase tracking-wide text-muted-foreground";

/**
 * The channel column: the caller's channels grouped under headings, each with
 * its unread badge (muted channels are dimmed and show no badge), a lock for a
 * private channel, a browse section for public channels to join, and the
 * caller in the footer.
 */
export function ChatSidebar({
  channels,
  status,
  activeChannelId,
  unreadBySpace,
  browse,
  canCreate,
  guest = false,
  copy,
  onSelect,
  onCreate,
  onOpenSettings,
  onSearch,
  onJoin,
  onRetry,
  joiningId,
  viewer = null,
  conversations = null,
}: ChatSidebarProps) {
  const groups = groupChannels(channels);

  return (
    <nav
      aria-label={copy.sidebar.navLabel}
      className="flex min-h-0 flex-1 flex-col bg-sidebar text-sidebar-foreground"
    >
      <div className="flex items-center justify-between gap-2 px-3 pt-3 pb-2">
        <h2 className="text-sm font-semibold">{copy.sidebar.listTitle}</h2>
        <div className="flex items-center gap-1">
          {onSearch ? (
            <Button
              variant="ghost"
              size="icon-sm"
              aria-label={copy.search.sidebarEntry}
              title={copy.search.sidebarEntry}
              onClick={onSearch}
            >
              <Search aria-hidden="true" />
            </Button>
          ) : null}
          {onOpenSettings ? (
            <Button
              variant="ghost"
              size="icon-sm"
              aria-label={copy.admin.open}
              title={copy.admin.open}
              onClick={onOpenSettings}
            >
              <Settings aria-hidden="true" />
            </Button>
          ) : null}
          {canCreate ? (
            <Button
              variant="ghost"
              size="icon-sm"
              aria-label={copy.sidebar.newChannel}
              title={copy.sidebar.newChannel}
              onClick={onCreate}
            >
              <Plus aria-hidden="true" />
            </Button>
          ) : null}
        </div>
      </div>

      {/* A jump-to field with no handler yet: visual only, so it is not a control. */}
      <div
        aria-hidden="true"
        className="mx-3 mb-2 flex h-9 items-center gap-2 rounded-md border border-border bg-background px-3 text-sm text-muted-foreground"
      >
        <Search className="size-3.5 shrink-0" />
        <span className="truncate">{copy.sidebar.jumpTo}</span>
      </div>

      <div className="flex min-h-0 flex-1 flex-col gap-1 overflow-y-auto px-2 pb-3">
        {status === "loading" ? (
          <div role="status" className="space-y-2 px-1 pt-2">
            <span className="sr-only">{copy.sidebar.loading}</span>
            {[0, 1, 2].map((index) => (
              <Skeleton key={index} className="h-7 w-full" />
            ))}
          </div>
        ) : status === "error" ? (
          <div className="space-y-2 px-1 pt-2 text-sm">
            <p className="text-muted-foreground">{copy.sidebar.error}</p>
            <Button variant="outline" size="sm" onClick={onRetry}>
              <RotateCw aria-hidden="true" />
              {copy.sidebar.retry}
            </Button>
          </div>
        ) : channels.length === 0 ? (
          <p className="px-2 pt-2 text-sm text-muted-foreground">
            {guest ? copy.sidebar.guestEmpty : copy.page.noChannelsTitle}
          </p>
        ) : (
          groups.map((group, index) => {
            const heading =
              group.groupId === null
                ? copy.sidebar.channelsHeading
                : fillCopy(copy.sidebar.groupNumbered, {
                    number: groupNumber(groups, index),
                  });
            return (
              <section
                key={group.groupId ?? "ungrouped"}
                className="flex flex-col"
              >
                <h3 className={HEADING}>{heading}</h3>
                <ul className="flex flex-col gap-0.5">
                  {group.channels.map((channel) => (
                    <li key={channel.id}>
                      <ChannelButton
                        channel={channel}
                        active={channel.id === activeChannelId}
                        unread={unreadBySpace.get(channel.id)}
                        copy={copy}
                        onSelect={onSelect}
                      />
                    </li>
                  ))}
                </ul>
              </section>
            );
          })
        )}

        {conversations ? (
          <ChatConversationSection view={conversations} copy={copy} />
        ) : null}

        {browse ? (
          <section
            aria-labelledby="chat-browse-title"
            className="flex flex-col gap-0.5"
          >
            <h3 id="chat-browse-title" className={HEADING}>
              {copy.sidebar.browseTitle}
            </h3>
            {browse.status === "loading" ? (
              <Skeleton className="h-7 w-full" />
            ) : browse.channels.length === 0 ? (
              <p className="px-2 text-sm text-muted-foreground">
                {copy.sidebar.browseEmpty}
              </p>
            ) : (
              <ul className="flex flex-col gap-0.5">
                {browse.channels.map((channel) => (
                  <li
                    key={channel.id}
                    className="flex h-8 items-center gap-2 px-2 text-sm text-muted-foreground"
                  >
                    <Hash className="size-3.5 shrink-0" aria-hidden="true" />
                    <span className="min-w-0 flex-1 truncate">
                      {channel.name}
                    </span>
                    <Button
                      variant="ghost"
                      size="sm"
                      disabled={joiningId === channel.id}
                      aria-label={`${copy.sidebar.join} #${channel.name}`}
                      onClick={() => onJoin(channel.id)}
                    >
                      {copy.sidebar.join}
                    </Button>
                  </li>
                ))}
              </ul>
            )}
          </section>
        ) : null}
      </div>

      {viewer ? (
        <div className="flex items-center gap-2.5 border-t border-border px-3 py-2.5">
          <Avatar className="size-8" aria-hidden="true">
            <AvatarFallback
              className={cn("text-xs font-semibold", avatarToneOf(viewer.id))}
            >
              {initialsOf(viewer.name)}
            </AvatarFallback>
          </Avatar>
          <span className="min-w-0 flex-1 truncate text-sm font-semibold">
            {viewer.name}
          </span>
        </div>
      ) : null}
    </nav>
  );
}

/** Groups are numbered in the order they appear, counting only the named ones. */
function groupNumber(groups: readonly ChannelGroup[], index: number): number {
  return groups.slice(0, index + 1).filter((group) => group.groupId !== null)
    .length;
}

function ChannelButton({
  channel,
  active,
  unread,
  copy,
  onSelect,
}: {
  readonly channel: ChatChannel;
  readonly active: boolean;
  readonly unread: ChannelUnread | undefined;
  readonly copy: ChatCopy;
  readonly onSelect: (channelId: string) => void;
}) {
  const badge = unread && !unread.muted ? unreadBadge(unread.count) : null;
  const muted = unread?.muted === true || channel.notificationLevel === "none";
  // A mention in a muted channel counts but does not badge (UC-CHT-D04).
  const mentionBadge =
    unread && !muted ? unreadBadge(unread.mentions ?? 0) : null;
  const isPrivate = channel.kind === "channel_private";
  const unreadHere = !muted && unread !== undefined && unread.count > 0;
  return (
    <button
      type="button"
      onClick={() => onSelect(channel.id)}
      aria-current={active ? "page" : undefined}
      aria-label={channelLabel(
        isPrivate,
        channel.name,
        badge,
        unread?.count ?? 0,
        muted,
        copy,
        unread?.mentions ?? 0,
      )}
      className={cn(
        "flex h-8 w-full items-center gap-2 rounded-lg px-2 text-start text-sm outline-none transition-colors focus-visible:ring-2 focus-visible:ring-focus-ring motion-reduce:transition-none",
        active
          ? "bg-state-selected font-semibold text-state-selected-foreground"
          : "text-muted-foreground hover:bg-sidebar-accent hover:text-sidebar-foreground",
        unreadHere && !active && "font-semibold text-foreground",
        muted && !active && "opacity-60",
      )}
    >
      {isPrivate ? (
        <Lock className="size-3.5 shrink-0" aria-hidden="true" />
      ) : (
        <Hash className="size-3.5 shrink-0" aria-hidden="true" />
      )}
      <span className="min-w-0 flex-1 truncate" title={channel.name}>
        {channel.name}
      </span>
      {muted ? (
        <BellOff className="size-3.5 shrink-0" aria-hidden="true" />
      ) : null}
      {mentionBadge ? (
        <span
          aria-hidden="true"
          data-testid="chat-mention-badge"
          className="flex h-5 min-w-5 shrink-0 items-center justify-center rounded-full bg-destructive px-1.5 text-xs font-bold text-destructive-foreground tabular-nums"
        >
          @{mentionBadge}
        </span>
      ) : null}
      {badge ? (
        <span
          aria-hidden="true"
          className="flex h-5 min-w-5 shrink-0 items-center justify-center rounded-full bg-brand-solid px-1.5 text-xs font-bold text-brand-solid-foreground tabular-nums"
        >
          {badge}
        </span>
      ) : null}
    </button>
  );
}

function channelLabel(
  isPrivate: boolean,
  name: string,
  badge: string | null,
  count: number,
  muted: boolean,
  copy: ChatCopy,
  mentions = 0,
): string {
  const parts = [
    `${isPrivate ? copy.sidebar.privateLabel : copy.sidebar.publicLabel}: ${name}`,
  ];
  if (badge && count > 0) {
    parts.push(
      pluralCopy(
        {
          one: copy.sidebar.unreadCount_one,
          other: copy.sidebar.unreadCount_other,
        },
        count,
      ),
    );
  }
  if (!muted && mentions > 0) {
    parts.push(
      pluralCopy(
        {
          one: copy.mentions.sidebarBadge_one,
          other: copy.mentions.sidebarBadge_other,
        },
        mentions,
      ),
    );
  }
  if (muted) parts.push(copy.sidebar.mutedLabel);
  return parts.join(", ");
}
