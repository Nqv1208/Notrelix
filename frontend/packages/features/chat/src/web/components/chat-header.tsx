import {
  Archive,
  ArchiveRestore,
  ChevronLeft,
  Hash,
  Link2,
  Link2Off,
  Lock,
  LogOut,
  Pencil,
  ShieldCheck,
  Trash2,
} from "lucide-react";
import {
  Button,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuRadioGroup,
  DropdownMenuRadioItem,
  DropdownMenuSeparator,
} from "@notrelix/ui-web";
import type { ChatChannel, ChatNotificationLevel } from "../../core/types/chat";
import type { ChatCopy } from "./chat.copy";
import { ChatLinkChip } from "./chat-resource-link";
import { ChatHeaderActions, type ChatCallKind } from "./chat-header-actions";
import type { ChatInfoSectionKey } from "./chat-info-panel";

export interface ChatHeaderProps {
  readonly channel: ChatChannel;
  readonly canManage: boolean;
  readonly joining: boolean;
  readonly copy: ChatCopy;
  /** Shown on narrow screens: leaves the room for the channel list. */
  readonly onOpenList: () => void;
  readonly onRename: () => void;
  readonly onToggleArchive: () => void;
  /** Leave the channel (a member); without it the settings menu has no Leave entry. */
  readonly onLeave?: () => void;
  /** Delete the channel (a manager, an Owner or an Admin). */
  readonly onDelete?: () => void;
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
  /** The call buttons exist only when calls are enabled and a handler is given. */
  readonly callsEnabled?: boolean;
  readonly onStartCall?: (kind: ChatCallKind) => void;
  readonly infoOpen: boolean;
  readonly onToggleInfo: () => void;
  readonly onOpenSection: (key: ChatInfoSectionKey) => void;
  readonly onSetLevel: (level: ChatNotificationLevel) => void;
}

const LEVELS: readonly ChatNotificationLevel[] = ["all", "mentions", "none"];

/**
 * The header of an open channel: its name and topic, the call, search and info buttons and a "…" menu with the
 * pinned and following panels, the notification level, and for a manager the rename and archive actions. A
 * public channel the caller is not in offers to join it. On a narrow screen a
 * back button returns to the channel list.
 */
export function ChatHeader({
  channel,
  canManage,
  joining,
  copy,
  onOpenList,
  onRename,
  onToggleArchive,
  onLeave,
  onDelete,
  canManagePermissions = false,
  onPermissions,
  workspaceId,
  onLinkResource,
  onUnlinkResource,
  onJoin,
  onSearch,
  callsEnabled,
  onStartCall,
  infoOpen,
  onToggleInfo,
  onOpenSection,
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
        <h1 className="flex min-w-0 items-center gap-1.5 text-lg leading-[1.45] font-[650] text-foreground max-md:text-calendar-title">
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
          className="truncate text-crumb leading-[1.45] text-muted-foreground max-md:text-xs"
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

        <ChatHeaderActions
          copy={copy}
          callsEnabled={callsEnabled}
          onStartCall={onStartCall}
          onSearch={onSearch}
          infoOpen={infoOpen}
          onToggleInfo={onToggleInfo}
          infoLabel={copy.panel.channelInfo}
          onOpenSection={onOpenSection}
          menu={
            channel.isMember || canManage || onLeave ? (
              <>
                {channel.isMember ? (
                  <>
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
                    <DropdownMenuSeparator />
                  </>
                ) : null}
                {canManage ? (
                  <DropdownMenuItem onSelect={onRename}>
                    <Pencil aria-hidden="true" />
                    {copy.header.rename}
                  </DropdownMenuItem>
                ) : null}
                {canManage && canManagePermissions && onPermissions ? (
                  <DropdownMenuItem onSelect={onPermissions}>
                    <ShieldCheck aria-hidden="true" />
                    {copy.header.permissions}
                  </DropdownMenuItem>
                ) : null}
                {canManage && onLinkResource ? (
                  <DropdownMenuItem onSelect={onLinkResource}>
                    <Link2 aria-hidden="true" />
                    {channel.link
                      ? copy.header.changeLink
                      : copy.header.linkResource}
                  </DropdownMenuItem>
                ) : null}
                {canManage && channel.link && onUnlinkResource ? (
                  <DropdownMenuItem onSelect={onUnlinkResource}>
                    <Link2Off aria-hidden="true" />
                    {copy.header.unlinkResource}
                  </DropdownMenuItem>
                ) : null}
                {canManage ? (
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
                ) : null}
                {onLeave || (canManage && onDelete) ? (
                  <DropdownMenuSeparator />
                ) : null}
                {onLeave ? (
                  <DropdownMenuItem variant="destructive" onSelect={onLeave}>
                    <LogOut aria-hidden="true" />
                    {copy.membership.leave}
                  </DropdownMenuItem>
                ) : null}
                {canManage && onDelete ? (
                  <DropdownMenuItem variant="destructive" onSelect={onDelete}>
                    <Trash2 aria-hidden="true" />
                    {copy.membership.delete}
                  </DropdownMenuItem>
                ) : null}
              </>
            ) : null
          }
        />
      </div>
    </header>
  );
}
