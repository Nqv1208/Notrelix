import { useMemo, useState } from "react";
import {
  MoreHorizontal,
  ShieldCheck,
  ShieldOff,
  UserMinus,
  UserPlus,
} from "lucide-react";
import {
  Avatar,
  AvatarFallback,
  Button,
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
  Input,
  cn,
} from "@notrelix/ui-web";
import type { ChatChannelMember } from "../../core/types/chat";
import type { ChatPresenceState } from "../../core/model/ephemeral-model";
import { PresenceDot, presenceText } from "./chat-presence";
import type { ChatCopy } from "./chat.copy";
import { avatarToneOf, initialsOf } from "./chat-initials";
import { fillCopy } from "./use-chat-copy";
import { ChatPersonPicker, type PickablePerson } from "./chat-person-picker";

export interface ChatChannelMemberRowsProps {
  readonly members: readonly ChatChannelMember[];
  readonly viewerId: string;
  readonly authorName: (userId: string) => string;
  readonly presenceOf?: (userId: string) => ChatPresenceState;
  readonly copy: ChatCopy;
  /** Remove another member (a manager); without it no row has a Remove button. */
  readonly onRemoveMember?: (userId: string) => void;
  /** Make a member a manager or a manager a member (a manager); without it no row has the role button. */
  readonly onSetRole?: (userId: string, role: "manager" | "member") => void;
  /** Open the invite dialog (a manager of a live channel); without it the section has no Invite button. */
  readonly onInvite?: () => void;
}

/** The members of a channel, one row each, for the Members section of the info panel (it used to be a popover of the header). */
export function ChatChannelMemberRows({
  members,
  viewerId,
  authorName,
  presenceOf,
  copy,
  onRemoveMember,
  onInvite,
  onSetRole,
}: ChatChannelMemberRowsProps) {
  const [query, setQuery] = useState("");
  if (members.length === 0) {
    return (
      <p className="px-4 py-1 text-sm text-muted-foreground">
        {copy.header.membersEmpty}
      </p>
    );
  }
  const needle = query.trim().toLowerCase();
  const shown = needle
    ? members.filter((member) =>
        authorName(member.userId).toLowerCase().includes(needle),
      )
    : members;
  return (
    <>
      <div className="px-4 pb-2">
        <Input
          type="search"
          value={query}
          placeholder={copy.membership.memberSearch}
          aria-label={copy.membership.memberSearch}
          onChange={(event) => setQuery(event.target.value)}
        />
      </div>
      {shown.length === 0 ? (
        <p className="px-4 py-1 text-sm text-muted-foreground">
          {copy.membership.memberSearchNone}
        </p>
      ) : null}
      <ul className="flex flex-col px-4">
        {shown.map((member) => {
          const name = authorName(member.userId);
          const isViewer = member.userId === viewerId;
          const presence = isViewer ? undefined : presenceOf?.(member.userId);
          const role =
            member.role === "manager"
              ? copy.header.manager
              : copy.membership.memberRole;
          return (
            <li
              key={member.userId}
              className="flex min-h-11 items-center gap-3 py-1 text-sm"
            >
              <span className="relative inline-flex shrink-0">
                <Avatar className="size-8" aria-hidden="true">
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
              <span className="flex min-w-0 flex-1 flex-col">
                <span className="truncate font-medium text-foreground">
                  {name}
                  {presence ? (
                    <span className="sr-only">
                      {`, ${presenceText(presence, copy)}`}
                    </span>
                  ) : null}
                </span>
                <span className="truncate text-xs text-muted-foreground">
                  {isViewer ? `${role} · ${copy.header.you}` : role}
                </span>
              </span>
              {!isViewer && (onSetRole || onRemoveMember) ? (
                <DropdownMenu>
                  <DropdownMenuTrigger asChild>
                    <Button
                      variant="ghost"
                      size="icon-sm"
                      aria-label={fillCopy(copy.membership.memberActions, {
                        name,
                      })}
                    >
                      <MoreHorizontal aria-hidden="true" />
                    </Button>
                  </DropdownMenuTrigger>
                  <DropdownMenuContent
                    align="end"
                    className="w-auto min-w-[220px] rounded-[12px] border border-border p-1.5 ring-0"
                  >
                    {onSetRole ? (
                      <DropdownMenuItem
                        className="min-h-9 gap-2.5 px-3.5 text-item-ui"
                        onSelect={() =>
                          onSetRole(
                            member.userId,
                            member.role === "manager" ? "member" : "manager",
                          )
                        }
                      >
                        {member.role === "manager" ? (
                          <ShieldOff className="size-3.5" aria-hidden="true" />
                        ) : (
                          <ShieldCheck
                            className="size-3.5"
                            aria-hidden="true"
                          />
                        )}
                        {fillCopy(
                          member.role === "manager"
                            ? copy.membership.makeMember
                            : copy.membership.makeManager,
                          { name },
                        )}
                      </DropdownMenuItem>
                    ) : null}
                    {onRemoveMember ? (
                      <DropdownMenuItem
                        variant="destructive"
                        className="min-h-9 gap-2.5 px-3.5 text-item-ui"
                        onSelect={() => onRemoveMember(member.userId)}
                      >
                        <UserMinus className="size-3.5" aria-hidden="true" />
                        {fillCopy(copy.membership.remove, { name })}
                      </DropdownMenuItem>
                    ) : null}
                  </DropdownMenuContent>
                </DropdownMenu>
              ) : null}
            </li>
          );
        })}
      </ul>
      {onInvite ? (
        <div className="px-4 pt-2">
          <Button variant="outline" size="sm" onClick={onInvite}>
            <UserPlus aria-hidden="true" />
            {copy.membership.invite}
          </Button>
        </div>
      ) : null}
    </>
  );
}

export interface ChatInviteDialogProps {
  readonly open: boolean;
  readonly busy: boolean;
  readonly channelName: string;
  /** The Workspace members that are not yet in the channel. */
  readonly candidates: readonly PickablePerson[];
  readonly error: string | null;
  readonly copy: ChatCopy;
  readonly onOpenChange: (open: boolean) => void;
  readonly onSubmit: (userIds: readonly string[]) => void;
}

/** Invites Workspace members into a channel (a private channel is joined by invitation only). */
export function ChatInviteDialog(props: ChatInviteDialogProps) {
  return (
    <Dialog open={props.open} onOpenChange={props.onOpenChange}>
      <DialogContent>
        <InviteForm {...props} />
      </DialogContent>
    </Dialog>
  );
}

function InviteForm({
  busy,
  channelName,
  candidates,
  error,
  copy,
  onOpenChange,
  onSubmit,
}: ChatInviteDialogProps) {
  const [picked, setPicked] = useState<ReadonlySet<string>>(new Set());
  const ordered = useMemo(
    () => [...candidates].sort((a, b) => a.name.localeCompare(b.name)),
    [candidates],
  );
  return (
    <form
      className="flex flex-col gap-4"
      onSubmit={(event) => {
        event.preventDefault();
        if (picked.size > 0 && !busy) onSubmit([...picked]);
      }}
    >
      <DialogHeader>
        <DialogTitle>
          {fillCopy(copy.membership.inviteTitle, { channel: channelName })}
        </DialogTitle>
        <DialogDescription>{copy.membership.inviteHint}</DialogDescription>
      </DialogHeader>
      <ChatPersonPicker
        people={ordered}
        selected={picked}
        copy={copy}
        emptyText={copy.membership.inviteNobody}
        onToggle={(id, value) =>
          setPicked((current) => {
            const next = new Set(current);
            if (value) next.add(id);
            else next.delete(id);
            return next;
          })
        }
      />
      {error ? (
        <p role="alert" className="text-sm text-destructive">
          {error}
        </p>
      ) : null}
      <DialogFooter>
        <Button
          type="button"
          variant="ghost"
          onClick={() => onOpenChange(false)}
        >
          {copy.membership.cancel}
        </Button>
        <Button type="submit" disabled={picked.size === 0 || busy}>
          {picked.size > 0
            ? `${copy.membership.inviteSubmit} (${picked.size})`
            : copy.membership.inviteSubmit}
        </Button>
      </DialogFooter>
    </form>
  );
}
