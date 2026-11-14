import {
  Archive,
  ArchiveRestore,
  Bell,
  ChevronLeft,
  Hash,
  Link2,
  Link2Off,
  Lock,
  Pencil,
  Search,
  Settings,
  ShieldCheck,
  Users,
} from "lucide-react";
import {
  Avatar,
  AvatarFallback,
  Button,
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuRadioGroup,
  DropdownMenuRadioItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
  Popover,
  PopoverContent,
  PopoverTrigger,
  cn,
} from "@notrelix/ui-web";
import type {
  ChatChannel,
  ChatChannelMember,
  ChatNotificationLevel,
} from "../../core/types/chat";
import type { ChatPresenceState } from "../../core/model/ephemeral-model";
import { PresenceDot, presenceText } from "./chat-presence";
import type { ChatCopy } from "./chat.copy";
import { ChatLinkChip } from "./chat-resource-link";
import { avatarToneOf, initialsOf } from "./chat-initials";
import { fillCopy } from "./use-chat-copy";

export interface ChatHeaderProps {
  readonly channel: ChatChannel;
  readonly members: readonly ChatChannelMember[];
  readonly viewerId: string;
  readonly canManage: boolean;
  readonly joining: boolean;
  readonly authorName: (userId: string) => string;
  /** The presence of a person; the member list shows a dot with it. */
  readonly presenceOf?: (userId: string) => ChatPresenceState;
  readonly copy: ChatCopy;
  /** Shown on narrow screens: leaves the room for the channel list. */
  readonly onOpenList: () => void;
  readonly onRename: () => void;
  readonly onToggleArchive: () => void;
  /** Whether the caller may open the channel's permissions (a manager, an Owner or an Admin of a live channel). */
  readonly canManagePermissions?: boolean;
  readonly onPermissions?: () => void;
  /** The Workspace, for the address of the linked resource. */
  readonly workspaceId?: string;
  /** Opens the "Link a resource" dialog (a manager); without it the menu has no link entries. */
  readonly onLinkResource?: () => void;
  /** Removes the link (a manager). */
  readonly onUnlinkResource?: () => void;
  readonly onJoin: () => void;
  /** Opens the message search (UC-CHT-H07). Without it the header has no search button. */
  readonly onSearch?: () => void;
  readonly onSetLevel: (level: ChatNotificationLevel) => void;
}

const LEVELS: readonly ChatNotificationLevel[] = ["all", "mentions", "none"];

/**
 * The header of an open channel: its name and topic, the members, the
 * notification level, and for a manager the rename and archive actions. A
 * public channel the caller is not in offers to join it. On a narrow screen a
 * back button returns to the channel list.
 */
export function ChatHeader({
  channel,
  members,
  viewerId,
  canManage,
  joining,
  authorName,
  presenceOf,
  copy,
  onOpenList,
  onRename,
  onToggleArchive,
  canManagePermissions = false,
  onPermissions,
  workspaceId,
  onLinkResource,
  onUnlinkResource,
  onJoin,
  onSearch,
  onSetLevel,
}: ChatHeaderProps) {
  const isPrivate = channel.kind === "channel_private";
  const Icon = isPrivate ? Lock : Hash;
  const level: ChatNotificationLevel = channel.notificationLevel ?? "all";
  const canJoin = !channel.isMember && !isPrivate && !channel.isArchived;

  return (
    <header className="flex min-h-16 flex-wrap items-center gap-x-3 gap-y-1 border-b border-border bg-card px-3 py-2 max-md:min-h-14 max-md:gap-x-2 max-md:py-0 md:px-5">
      <Button
        variant="ghost"
        size="icon-sm"
        className="md:hidden"
        aria-label={copy.list.backToList}
        onClick={onOpenList}
      >
        <ChevronLeft aria-hidden="true" />
      </Button>
      <div className="flex min-w-0 flex-1 flex-col">
        <h1 className="flex min-w-0 items-center gap-1.5 text-lg leading-[1.45] font-[650] text-foreground max-md:text-[17px]">
          <Icon
            className="size-4 shrink-0 text-muted-foreground"
            aria-hidden="true"
          />
          <span className="sr-only">
            {isPrivate ? copy.header.privateMark : copy.header.publicMark}:{" "}
          </span>
          <span className="truncate" title={channel.name}>
            {channel.name}
          </span>
          {channel.isArchived ? (
            <span className="ms-2 shrink-0 rounded-full border border-border px-2 py-0.5 text-xs font-medium text-muted-foreground">
              {copy.header.archivedBadge}
            </span>
          ) : null}
        </h1>
        <p
          className="truncate text-[12.5px] leading-[1.45] text-muted-foreground max-md:text-xs"
          title={channel.topic ?? undefined}
        >
          {channel.topic ?? copy.header.topicEmpty}
        </p>
      </div>
      {channel.link && workspaceId ? (
        <ChatLinkChip
          link={channel.link}
          workspaceId={workspaceId}
          copy={copy}
        />
      ) : null}

      <div className="flex shrink-0 items-center gap-1">
        {canJoin ? (
          <Button size="sm" onClick={onJoin} disabled={joining}>
            {copy.header.join}
          </Button>
        ) : null}

        {onSearch ? (
          <Button
            variant="ghost"
            size="icon-sm"
            aria-label={copy.search.open}
            title={copy.search.open}
            onClick={onSearch}
          >
            <Search aria-hidden="true" />
          </Button>
        ) : null}

        <Popover>
          <PopoverTrigger asChild>
            <Button
              variant="ghost"
              size="sm"
              className="text-muted-foreground"
              aria-label={fillCopy(copy.header.membersButton, {
                count: members.length,
              })}
            >
              <Users aria-hidden="true" />
              <span className="tabular-nums">{members.length}</span>
            </Button>
          </PopoverTrigger>
          <PopoverContent align="end" className="w-64 p-2">
            <h2 className="px-2 py-1 text-sm font-semibold">
              {copy.header.membersTitle}
            </h2>
            {members.length === 0 ? (
              <p className="px-2 py-1 text-sm text-muted-foreground">
                {copy.header.membersEmpty}
              </p>
            ) : (
              <ul className="flex flex-col">
                {members.map((member) => {
                  const name = authorName(member.userId);
                  const presence =
                    member.userId === viewerId
                      ? undefined
                      : presenceOf?.(member.userId);
                  return (
                    <li
                      key={member.userId}
                      className="flex items-center gap-2 px-2 py-1.5 text-sm"
                    >
                      <span className="relative inline-flex shrink-0">
                        <Avatar className="size-6" aria-hidden="true">
                          <AvatarFallback
                            className={cn(
                              "text-xs font-semibold",
                              avatarToneOf(member.userId),
                            )}
                          >
                            {initialsOf(name)}
                          </AvatarFallback>
                        </Avatar>
                        {presence ? <PresenceDot state={presence} /> : null}
                      </span>
                      <span className="min-w-0 flex-1 truncate">
                        {name}
                        {member.userId === viewerId
                          ? ` (${copy.header.you})`
                          : ""}
                        {presence ? (
                          <span className="sr-only">
                            {`, ${presenceText(presence, copy)}`}
                          </span>
                        ) : null}
                      </span>
                      {member.role === "manager" ? (
                        <span className="text-xs text-muted-foreground">
                          {copy.header.manager}
                        </span>
                      ) : null}
                    </li>
                  );
                })}
              </ul>
            )}
          </PopoverContent>
        </Popover>

        {channel.isMember ? (
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button
                variant="ghost"
                size="icon-sm"
                aria-label={copy.header.notifications}
              >
                <Bell aria-hidden="true" />
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end" className="w-56">
              <DropdownMenuLabel>{copy.level.label}</DropdownMenuLabel>
              <DropdownMenuRadioGroup
                value={level}
                onValueChange={(value) =>
                  onSetLevel(value as ChatNotificationLevel)
                }
              >
                {LEVELS.map((item) => (
                  <DropdownMenuRadioItem key={item} value={item}>
                    {copy.level[item]}
                  </DropdownMenuRadioItem>
                ))}
              </DropdownMenuRadioGroup>
            </DropdownMenuContent>
          </DropdownMenu>
        ) : null}

        {canManage ? (
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button
                variant="ghost"
                size="icon-sm"
                aria-label={copy.header.manage}
              >
                <Settings aria-hidden="true" />
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end">
              <DropdownMenuItem onSelect={onRename}>
                <Pencil aria-hidden="true" />
                {copy.header.rename}
              </DropdownMenuItem>
              {canManagePermissions && onPermissions ? (
                <DropdownMenuItem onSelect={onPermissions}>
                  <ShieldCheck aria-hidden="true" />
                  {copy.header.permissions}
                </DropdownMenuItem>
              ) : null}
              {onLinkResource ? (
                <DropdownMenuItem onSelect={onLinkResource}>
                  <Link2 aria-hidden="true" />
                  {channel.link
                    ? copy.header.changeLink
                    : copy.header.linkResource}
                </DropdownMenuItem>
              ) : null}
              {channel.link && onUnlinkResource ? (
                <DropdownMenuItem onSelect={onUnlinkResource}>
                  <Link2Off aria-hidden="true" />
                  {copy.header.unlinkResource}
                </DropdownMenuItem>
              ) : null}
              <DropdownMenuSeparator />
              <DropdownMenuItem onSelect={onToggleArchive}>
                {channel.isArchived ? (
                  <ArchiveRestore aria-hidden="true" />
                ) : (
                  <Archive aria-hidden="true" />
                )}
                {channel.isArchived
                  ? copy.header.unarchive
                  : copy.header.archive}
              </DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>
        ) : null}
      </div>
    </header>
  );
}
