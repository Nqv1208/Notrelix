import { UserMinus } from "lucide-react";
import { Avatar, AvatarFallback, Button, cn } from "@notrelix/ui-web";
import type { ChatChannelMember } from "../../core/types/chat";
import type { ChatPresenceState } from "../../core/model/ephemeral-model";
import { PresenceDot, presenceText } from "./chat-presence";
import type { ChatCopy } from "./chat.copy";
import { avatarToneOf, initialsOf } from "./chat-initials";
import { fillCopy } from "./use-chat-copy";

export interface ChatChannelMemberRowsProps {
  readonly members: readonly ChatChannelMember[];
  readonly viewerId: string;
  readonly authorName: (userId: string) => string;
  readonly presenceOf?: (userId: string) => ChatPresenceState;
  readonly copy: ChatCopy;
  /** Remove another member (a manager); without it no row has a Remove button. */
  readonly onRemoveMember?: (userId: string) => void;
}

/** The members of a channel, one row each, for the Members section of the info panel (it used to be a popover of the header). */
export function ChatChannelMemberRows({
  members,
  viewerId,
  authorName,
  presenceOf,
  copy,
  onRemoveMember,
}: ChatChannelMemberRowsProps) {
  if (members.length === 0) {
    return (
      <p className="px-4 py-1 text-sm text-muted-foreground">
        {copy.header.membersEmpty}
      </p>
    );
  }
  return (
    <ul className="flex flex-col px-4">
      {members.map((member) => {
        const name = authorName(member.userId);
        const presence =
          member.userId === viewerId ? undefined : presenceOf?.(member.userId);
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
  );
}
