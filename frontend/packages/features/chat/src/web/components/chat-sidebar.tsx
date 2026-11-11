import { BellOff, Hash, Lock, Plus, RotateCw } from "lucide-react";
import { Button, Skeleton, cn } from "@notrelix/ui-web";
import type { ChatChannel } from "../../core/types/chat";
import type { ChatCopy } from "./chat.copy";
import { pluralCopy } from "./use-chat-copy";

export interface ChannelUnread {
  readonly count: number;
  readonly muted: boolean;
}

export type ChannelListStatus = "loading" | "ready" | "error";

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
  readonly copy: ChatCopy;
  readonly onSelect: (channelId: string) => void;
  readonly onCreate: () => void;
  readonly onJoin: (channelId: string) => void;
  readonly onRetry: () => void;
  readonly joiningId: string | null;
}

/** A badge count: nothing for zero, 99+ above 99 (UC-CHT-D01). */
export function unreadBadge(count: number): string | null {
  if (!Number.isFinite(count) || count <= 0) return null;
  return count > 99 ? "99+" : String(Math.floor(count));
}

/**
 * The channel column: the caller's channels in sidebar order, each with its
 * unread badge (muted channels are dimmed and show no badge), a lock for a
 * private channel, and a browse section for public channels to join.
 */
export function ChatSidebar({
  channels,
  status,
  activeChannelId,
  unreadBySpace,
  browse,
  canCreate,
  copy,
  onSelect,
  onCreate,
  onJoin,
  onRetry,
  joiningId,
}: ChatSidebarProps) {
  return (
    <nav
      aria-label={copy.sidebar.navLabel}
      className="flex min-h-0 flex-1 flex-col gap-4 overflow-y-auto p-3"
    >
      <div className="flex items-center justify-between gap-2 px-1">
        <h2 className="text-xs font-semibold uppercase tracking-wide text-muted-foreground">
          {copy.sidebar.listTitle}
        </h2>
        {canCreate ? (
          <Button
            variant="ghost"
            size="icon-xs"
            aria-label={copy.sidebar.newChannel}
            title={copy.sidebar.newChannel}
            onClick={onCreate}
          >
            <Plus aria-hidden="true" />
          </Button>
        ) : null}
      </div>

      {status === "loading" ? (
        <div role="status" className="space-y-2 px-1">
          <span className="sr-only">{copy.sidebar.loading}</span>
          {[0, 1, 2].map((index) => (
            <Skeleton key={index} className="h-7 w-full" />
          ))}
        </div>
      ) : status === "error" ? (
        <div className="space-y-2 px-1 text-sm">
          <p className="text-muted-foreground">{copy.sidebar.error}</p>
          <Button variant="outline" size="sm" onClick={onRetry}>
            <RotateCw aria-hidden="true" />
            {copy.sidebar.retry}
          </Button>
        </div>
      ) : (
        <ul className="flex flex-col gap-0.5">
          {channels.map((channel) => {
            const unread = unreadBySpace.get(channel.id);
            const badge =
              unread && !unread.muted ? unreadBadge(unread.count) : null;
            const active = channel.id === activeChannelId;
            const muted =
              unread?.muted === true || channel.notificationLevel === "none";
            const name = channel.name;
            return (
              <li key={channel.id}>
                <button
                  type="button"
                  onClick={() => onSelect(channel.id)}
                  aria-current={active ? "page" : undefined}
                  aria-label={channelLabel(
                    channel.kind === "channel_private",
                    name,
                    badge,
                    unread?.count ?? 0,
                    muted,
                    copy,
                  )}
                  className={cn(
                    "flex w-full items-center gap-2 rounded-md px-2 py-1.5 text-start text-sm outline-none focus-visible:ring-2 focus-visible:ring-focus-ring",
                    active
                      ? "bg-accent text-accent-foreground"
                      : "text-muted-foreground hover:bg-muted hover:text-foreground",
                    muted && !active && "opacity-60",
                    !muted &&
                      unread &&
                      unread.count > 0 &&
                      !active &&
                      "font-semibold text-foreground",
                  )}
                >
                  {channel.kind === "channel_private" ? (
                    <Lock className="size-3.5 shrink-0" aria-hidden="true" />
                  ) : (
                    <Hash className="size-3.5 shrink-0" aria-hidden="true" />
                  )}
                  <span className="min-w-0 flex-1 truncate">{name}</span>
                  {muted ? (
                    <BellOff className="size-3.5 shrink-0" aria-hidden="true" />
                  ) : null}
                  {badge ? (
                    <span
                      aria-hidden="true"
                      className="ms-auto min-w-5 rounded-full bg-brand-solid px-1.5 text-center text-xs font-semibold text-brand-solid-foreground tabular-nums"
                    >
                      {badge}
                    </span>
                  ) : null}
                </button>
              </li>
            );
          })}
          {channels.length === 0 ? (
            <li className="px-2 py-1.5 text-sm text-muted-foreground">
              {copy.page.noChannelsTitle}
            </li>
          ) : null}
        </ul>
      )}

      {browse ? (
        <section
          aria-labelledby="chat-browse-title"
          className="flex flex-col gap-1"
        >
          <h2
            id="chat-browse-title"
            className="px-1 text-xs font-semibold uppercase tracking-wide text-muted-foreground"
          >
            {copy.sidebar.browseTitle}
          </h2>
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
                  className="flex items-center gap-2 px-2 py-1 text-sm"
                >
                  <Hash
                    className="size-3.5 shrink-0 text-muted-foreground"
                    aria-hidden="true"
                  />
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
    </nav>
  );
}

function channelLabel(
  isPrivate: boolean,
  name: string,
  badge: string | null,
  count: number,
  muted: boolean,
  copy: ChatCopy,
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
  if (muted) parts.push(copy.sidebar.mutedLabel);
  return parts.join(", ");
}
