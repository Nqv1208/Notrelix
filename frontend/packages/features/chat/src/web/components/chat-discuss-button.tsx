import { MessageSquare } from "lucide-react";
import {
  Button,
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuTrigger,
} from "@notrelix/ui-web";
import type { ChatLinkKind } from "../../core/types/chat";
import type { ChatHooks } from "../query/create-chat-hooks";
import { fillCopy, useChatCopy } from "./use-chat-copy";

export interface DiscussButtonProps {
  readonly hooks: ChatHooks;
  readonly workspaceId: string;
  /** The Board or Page the host page shows. */
  readonly resource: {
    readonly kind: ChatLinkKind;
    readonly resourceId: string;
  };
  /** Opens a channel in Chat; the host decides how (a route). */
  readonly onOpenChannel: (channelId: string) => void;
}

/**
 * "Discuss" on a Board or Page (UC-CHT-H10): opens the channel linked to this resource. It renders nothing while the lookup loads
 * or fails, and nothing when no channel the caller can see is linked (the server lists only those, and nothing at all to a
 * person who cannot view the resource). One channel opens directly; several are a menu.
 */
export function DiscussButton({
  hooks,
  workspaceId,
  resource,
  onOpenChannel,
}: DiscussButtonProps) {
  const copy = useChatCopy();
  const linked = hooks.useLinkedChannels(workspaceId, resource);
  const channels = (linked.data ?? []).filter((c) => !c.isArchived);
  const only = channels.length === 1 ? channels[0] : undefined;
  if (channels.length === 0) return null;

  if (only) {
    return (
      <Button
        variant="outline"
        size="sm"
        aria-label={fillCopy(copy.discuss.buttonOne, { name: only.name })}
        onClick={() => onOpenChannel(only.id)}
      >
        <MessageSquare aria-hidden="true" />
        {copy.discuss.button}
      </Button>
    );
  }

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button variant="outline" size="sm">
          <MessageSquare aria-hidden="true" />
          {copy.discuss.button}
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end">
        <DropdownMenuLabel>{copy.discuss.menuLabel}</DropdownMenuLabel>
        {channels.map((channel) => (
          <DropdownMenuItem
            key={channel.id}
            onSelect={() => onOpenChannel(channel.id)}
          >
            {fillCopy(copy.discuss.open, { name: channel.name })}
          </DropdownMenuItem>
        ))}
      </DropdownMenuContent>
    </DropdownMenu>
  );
}
