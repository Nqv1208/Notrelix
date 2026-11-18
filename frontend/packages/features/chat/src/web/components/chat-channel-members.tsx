import { useMemo, useState } from "react";
import { ShieldCheck, ShieldOff, UserMinus, UserPlus } from "lucide-react";
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
  if (members.length === 0) {
    return (
      <p className="px-4 py-1 text-sm text-muted-foreground">
        {copy.header.membersEmpty}
      </p>
    );
  }
  return (
    <>
      {onInvite ? (
        <div className="px-4 pb-2">
          <Button variant="outline" size="sm" onClick={onInvite}>
            <UserPlus aria-hidden="true" />
            {copy.membership.invite}
          </Button>
        </div>
      ) : null}
      <ul className="flex flex-col px-4">
        {members.map((member) => {
          const name = authorName(member.userId);
          const presence =
            member.userId === viewerId
              ? undefined
              : presenceOf?.(member.userId);
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
              <span className="min-w-0 flex-1 truncate font-medium text-foreground">
                {name}
                {member.userId === viewerId ? ` (${copy.header.you})` : ""}
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
              {onSetRole && member.userId !== viewerId ? (
                <Button
                  variant="ghost"
                  size="icon-sm"
                  aria-label={fillCopy(
                    member.role === "manager"
                      ? copy.membership.makeMember
                      : copy.membership.makeManager,
                    { name },
                  )}
                  onClick={() =>
                    onSetRole(
                      member.userId,
                      member.role === "manager" ? "member" : "manager",
                    )
                  }
                >
                  {member.role === "manager" ? (
                    <ShieldOff aria-hidden="true" />
                  ) : (
                    <ShieldCheck aria-hidden="true" />
                  )}
                </Button>
              ) : null}
              {onRemoveMember && member.userId !== viewerId ? (
                <Button
                  variant="ghost"
                  size="icon-sm"
                  aria-label={fillCopy(copy.membership.remove, { name })}
                  onClick={() => onRemoveMember(member.userId)}
                >
                  <UserMinus aria-hidden="true" />
                </Button>
              ) : null}
            </li>
          );
        })}
      </ul>
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
