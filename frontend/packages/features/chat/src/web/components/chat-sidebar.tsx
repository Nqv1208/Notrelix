import { useEffect, useId, useRef, useState, type KeyboardEvent } from "react";
import {
  BellOff,
  CheckCheck,
  Hash,
  Lock,
  Plus,
  RotateCw,
  Search,
  Settings,
  Trash2,
  X,
} from "lucide-react";
import { Button, Skeleton, cn } from "@notrelix/ui-web";
import type { ChatChannel, ChatConversation } from "../../core/types/chat";
import { conversationTitle } from "../../core/model/conversation-model";
import type { ChatCopy } from "./chat.copy";
import { fillCopy, pluralCopy } from "./use-chat-copy";
import {
  ChatConversationSection,
  ConversationRow,
  RowShell,
  type ChatConversationListView,
  type RowOption,
} from "./chat-conversation-list";

export interface ChannelUnread {
  readonly count: number;
  readonly muted: boolean;
  /** Unread messages that mention the caller (UC-CHT-D04): a badge apart from the unread count. */
  readonly mentions?: number;
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
  /** A Guest sees only the channels they were added to; an empty list says so (UC-CHT-H02). */
  readonly guest?: boolean;
  readonly copy: ChatCopy;
  readonly onSelect: (channelId: string) => void;
  readonly onCreate: () => void;
  /** Opens the Chat admin settings (retention and export); only an Owner or an Admin is offered it. */
  readonly onOpenSettings?: () => void;
  /** Opens the message search (UC-CHT-H07) with the text typed in the sidebar field. */
  readonly onSearch?: (query: string) => void;
  /** Marks every space as read (UC-CHT-D07); the button shows only while something is unread. */
  readonly onMarkAllRead?: () => void;
  readonly markingAllRead?: boolean;
  readonly onJoin: (channelId: string) => void;
  /** Opens the deleted channels the caller may restore (UC-CHT-A08); the entry shows only when it is given. */
  readonly onOpenDeleted?: () => void;
  readonly deletedActive?: boolean;
  readonly onRetry: () => void;
  readonly joiningId: string | null;
  /** The direct and group conversations section; left out when the host does not offer conversations. */
  readonly conversations?: ChatConversationListView | null;
  /** The category names by group id (`Chat.ChannelGroups.List`); a group without one keeps the numbered fallback. */
  readonly groupNames?: ReadonlyMap<string, string>;
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

/** Lower case without accents (đ included), so "thao" finds "Thảo". */
export function foldText(text: string): string {
  return text
    .normalize("NFD")
    .replace(/[̀-ͯ]/g, "")
    .replace(/đ/gi, "d")
    .toLowerCase();
}

type SearchHit =
  | { readonly kind: "channel"; readonly channel: ChatChannel }
  | { readonly kind: "conversation"; readonly conversation: ChatConversation };

function isEditable(target: EventTarget | null): boolean {
  if (!(target instanceof HTMLElement)) return false;
  return (
    target.isContentEditable ||
    ["INPUT", "TEXTAREA", "SELECT"].includes(target.tagName)
  );
}

const HEADING =
  "-mx-2 pt-3.5 pr-3 pb-1 pl-2.5 text-micro leading-[1.45] font-[650] tracking-[.04em] text-muted-foreground uppercase";

/**
 * The channel column: the caller's channels grouped under headings, each with
 * its unread badge (muted channels are dimmed and show no badge), a lock for a
 * private channel, a browse section for public channels to join.
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
  onOpenDeleted,
  deletedActive = false,
  onSearch,
  onMarkAllRead,
  markingAllRead = false,
  onJoin,
  onRetry,
  joiningId,
  conversations = null,
  groupNames,
}: ChatSidebarProps) {
  const groups = groupChannels(channels);
  const hasConversations = (conversations?.conversations.length ?? 0) > 0;
  const [query, setQuery] = useState("");
  const [activeRaw, setActive] = useState(0);
  const inputRef = useRef<HTMLInputElement>(null);
  const ids = useId();
  const listId = `${ids}-results`;
  const optionId = (index: number) => `${ids}-option-${index}`;
  const needle = foldText(query.trim());
  const searching = needle !== "";
  const hits: readonly SearchHit[] = searching
    ? [
        ...channels
          .filter((channel) => foldText(channel.name).includes(needle))
          .map((channel): SearchHit => ({ kind: "channel", channel })),
        ...(conversations?.conversations ?? [])
          .filter((conversation) =>
            foldText(
              conversationTitle(
                conversation,
                conversations!.viewerId,
                conversations!.nameOf,
              ),
            ).includes(needle),
          )
          .map((conversation): SearchHit => ({
            kind: "conversation",
            conversation,
          })),
      ]
    : [];
  const active = Math.min(activeRaw, Math.max(hits.length - 1, 0));

  function clearQuery() {
    setQuery("");
    setActive(0);
    inputRef.current?.focus();
  }
  // A picked row opens and the field empties, so the sidebar is back to its lists.
  const pick = (hit: SearchHit) => {
    setQuery("");
    setActive(0);
    if (hit.kind === "channel") onSelect(hit.channel.id);
    else conversations?.onSelect(hit.conversation.id);
  };
  function onFieldKey(event: KeyboardEvent<HTMLInputElement>) {
    if (event.key === "Escape" && query) {
      event.preventDefault();
      event.stopPropagation();
      clearQuery();
    } else if (event.key === "ArrowDown" || event.key === "ArrowUp") {
      if (hits.length === 0) return;
      event.preventDefault();
      const step = event.key === "ArrowDown" ? 1 : -1;
      setActive((active + step + hits.length) % hits.length);
    } else if (event.key === "Enter") {
      const hit = hits[active];
      if (hit) {
        event.preventDefault();
        pick(hit);
      }
    }
  }
  // "/" jumps to the field from anywhere outside a text box (Ctrl+K belongs to the app-wide palette).
  useEffect(() => {
    function onKey(event: globalThis.KeyboardEvent) {
      if (
        event.key !== "/" ||
        event.metaKey ||
        event.ctrlKey ||
        event.altKey ||
        isEditable(event.target)
      ) {
        return;
      }
      event.preventDefault();
      inputRef.current?.focus();
    }
    document.addEventListener("keydown", onKey);
    return () => document.removeEventListener("keydown", onKey);
  }, []);
  const anyUnread =
    [...unreadBySpace.values()].some((u) => !u.muted && u.count > 0) ||
    (conversations?.conversations ?? []).some(
      (c) => c.notificationLevel !== "none" && conversations!.unreadOf(c) > 0,
    );

  return (
    <nav
      aria-label={copy.sidebar.navLabel}
      className="flex min-h-0 flex-1 flex-col bg-sidebar text-sidebar-foreground"
    >
      <div className="flex items-center justify-between gap-2 px-3.5 pt-3.5 pb-2.5 max-md:h-14 max-md:px-3 max-md:py-0">
        <h2 className="text-item-heading font-[650] max-md:text-calendar-title">
          {copy.sidebar.listTitle}
        </h2>
        <div className="flex items-center gap-1">
          {onMarkAllRead && anyUnread ? (
            <Button
              variant="ghost"
              size="icon-sm"
              aria-label={copy.sidebar.markAllRead}
              title={copy.sidebar.markAllRead}
              disabled={markingAllRead}
              onClick={onMarkAllRead}
            >
              <CheckCheck aria-hidden="true" />
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

      <div className="relative mx-3 mb-2">
        <Search
          aria-hidden="true"
          className="pointer-events-none absolute start-2.5 top-1/2 size-3.5 -translate-y-1/2 text-muted-foreground"
        />
        <input
          ref={inputRef}
          type="text"
          role="combobox"
          aria-label={copy.sidebar.jumpTo}
          aria-expanded={hits.length > 0}
          aria-controls={listId}
          aria-autocomplete="list"
          aria-activedescendant={hits.length > 0 ? optionId(active) : undefined}
          aria-keyshortcuts="/"
          autoComplete="off"
          spellCheck={false}
          placeholder={copy.sidebar.jumpTo}
          value={query}
          onChange={(event) => {
            setQuery(event.target.value);
            setActive(0);
          }}
          onKeyDown={onFieldKey}
          className="h-9 w-full rounded-[10px] border border-border bg-card ps-8 pe-8 text-compact text-foreground outline-none placeholder:text-muted-foreground focus-visible:ring-2 focus-visible:ring-focus-ring max-md:h-11"
        />
        {query ? (
          <button
            type="button"
            aria-label={copy.sidebar.searchClear}
            title={copy.sidebar.searchClear}
            onMouseDown={(event) => event.preventDefault()}
            onClick={clearQuery}
            className="absolute end-1.5 top-1/2 flex size-6 -translate-y-1/2 items-center justify-center rounded-md text-muted-foreground hover:text-foreground"
          >
            <X className="size-3.5" aria-hidden="true" />
          </button>
        ) : null}
      </div>

      <div className="flex min-h-0 flex-1 flex-col overflow-y-auto px-2 pb-2">
        {searching ? (
          <>
            <div
              id={listId}
              role="listbox"
              aria-label={copy.sidebar.searchResults}
              hidden={hits.length === 0}
              className="flex flex-col gap-px pt-1"
            >
              {hits.map((hit, index) => {
                const option: RowOption = {
                  id: optionId(index),
                  selected: index === active,
                };
                return hit.kind === "channel" ? (
                  <ChannelButton
                    key={hit.channel.id}
                    channel={hit.channel}
                    active={hit.channel.id === activeChannelId}
                    unread={unreadBySpace.get(hit.channel.id)}
                    copy={copy}
                    onSelect={() => pick(hit)}
                    option={option}
                  />
                ) : (
                  <ConversationRow
                    key={hit.conversation.id}
                    conversation={hit.conversation}
                    view={{
                      ...conversations!,
                      onSelect: () => pick(hit),
                    }}
                    copy={copy}
                    now={new Date()}
                    option={option}
                  />
                );
              })}
            </div>
            {hits.length === 0 ? (
              <div role="status" className="space-y-2 px-2 pt-2 text-sm">
                <p className="text-muted-foreground">
                  {copy.sidebar.searchNoMatch}
                </p>
                {onSearch ? (
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={() => onSearch(query.trim())}
                  >
                    <Search aria-hidden="true" />
                    {fillCopy(copy.sidebar.searchInMessages, {
                      query: query.trim(),
                    })}
                  </Button>
                ) : null}
              </div>
            ) : null}
          </>
        ) : status === "loading" ? (
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
          // Another section has rows: the empty line would only take room.
          hasConversations ? null : (
            <p className="px-2 pt-2 text-sm text-muted-foreground">
              {guest ? copy.sidebar.guestEmpty : copy.page.noChannelsTitle}
            </p>
          )
        ) : (
          groups.map((group, index) => {
            const heading =
              group.groupId === null
                ? copy.sidebar.channelsHeading
                : groupHeading(
                    groupNames?.get(group.groupId),
                    fillCopy(copy.sidebar.groupNumbered, {
                      number: groupNumber(groups, index),
                    }),
                  );
            return (
              <section
                key={group.groupId ?? "ungrouped"}
                className="flex flex-col"
              >
                <h3 className={HEADING}>{heading}</h3>
                <ul className="flex flex-col gap-px">
                  {group.channels.map((channel) => (
                    <li key={channel.id} className="max-md:-mx-2">
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

        {conversations && !searching ? (
          <ChatConversationSection
            view={conversations}
            copy={copy}
            hideEmptyText={channels.length > 0}
          />
        ) : null}

        {browse && !searching ? (
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
                    className="flex h-8 items-center gap-[9px] px-2.5 text-sm text-muted-foreground max-md:-mx-2 max-md:min-h-12 max-md:gap-3 max-md:px-4"
                  >
                    <Hash className="size-3.5 shrink-0" aria-hidden="true" />
                    <span className="min-w-0 flex-1 truncate">
                      {channel.name}
                    </span>
                    <Button
                      variant="ghost"
                      size="sm"
                      className="max-md:min-h-11"
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

        {onOpenDeleted && !searching ? (
          <div className="flex flex-col gap-0.5 pt-2">
            <button
              type="button"
              aria-current={deletedActive ? "page" : undefined}
              className={cn(
                "flex h-8 items-center gap-[9px] rounded-md px-2.5 text-start text-sm text-muted-foreground hover:bg-accent max-md:-mx-2 max-md:min-h-12 max-md:gap-3 max-md:px-4",
                deletedActive && "bg-accent font-medium text-foreground",
              )}
              onClick={onOpenDeleted}
            >
              <Trash2 className="size-3.5 shrink-0" aria-hidden="true" />
              {copy.deleted.entry}
            </button>
          </div>
        ) : null}
      </div>
    </nav>
  );
}

/** Groups are numbered in the order they appear, counting only the named ones. */
/** The category's real name, or the numbered fallback while the names are unread or the group is not in them. */
export function groupHeading(name: string | undefined, fallback: string) {
  const trimmed = name?.trim();
  return trimmed ? trimmed : fallback;
}

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
  option,
}: {
  readonly option?: RowOption;
  readonly channel: ChatChannel;
  readonly active: boolean;
  readonly unread: ChannelUnread | undefined;
  readonly copy: ChatCopy;
  readonly onSelect: (channelId: string) => void;
}) {
  const muted = unread?.muted === true || channel.notificationLevel === "none";
  // A mention in a muted channel counts but does not badge (UC-CHT-D04).
  const mentionBadge =
    unread && !muted ? unreadBadge(unread.mentions ?? 0) : null;
  const isPrivate = channel.kind === "channel_private";
  // A channel shows that it is unread (bold and a dot), never how many: the count is a sequence difference, not a count of rows.
  const unreadHere = !muted && unread !== undefined && unread.count > 0;
  return (
    <RowShell
      option={option}
      active={active}
      onPick={() => onSelect(channel.id)}
      label={channelLabel(
        isPrivate,
        channel.name,
        unreadHere,
        muted,
        copy,
        unread?.mentions ?? 0,
      )}
      className={cn(
        "flex h-8 w-full items-center gap-[9px] rounded-[8px] px-2.5 text-start text-sm outline-none max-md:h-auto max-md:min-h-12 max-md:gap-3 max-md:rounded-none max-md:px-4 max-md:py-3 transition-colors focus-visible:ring-2 focus-visible:ring-focus-ring motion-reduce:transition-none",
        active
          ? "bg-state-selected font-[650] text-state-selected-foreground"
          : "text-muted-foreground hover:bg-sidebar-accent hover:text-sidebar-foreground active:bg-sidebar-accent/70",
        unreadHere && !active && "font-[650] text-foreground",
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
          className="flex h-5 min-w-5 shrink-0 items-center justify-center rounded-full bg-destructive px-1.5 text-micro font-bold text-destructive-foreground tabular-nums"
        >
          @{mentionBadge}
        </span>
      ) : null}
      {unreadHere ? (
        <span
          aria-hidden="true"
          data-testid="chat-unread-dot"
          className="size-2 shrink-0 rounded-full bg-brand-solid"
        />
      ) : null}
    </RowShell>
  );
}

function channelLabel(
  isPrivate: boolean,
  name: string,
  unreadHere: boolean,
  muted: boolean,
  copy: ChatCopy,
  mentions = 0,
): string {
  const parts = [
    `${isPrivate ? copy.sidebar.privateLabel : copy.sidebar.publicLabel}: ${name}`,
  ];
  if (unreadHere) parts.push(copy.sidebar.unreadLabel);
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
