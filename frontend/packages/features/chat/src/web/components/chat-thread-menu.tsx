import { useState } from "react";
import {
  BellOff,
  BellPlus,
  BellRing,
  Link2,
  MoreHorizontal,
} from "lucide-react";
import {
  Button,
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
  useIsMobile,
} from "@notrelix/ui-web";
import type { ChatCopy } from "./chat.copy";
import { ChatActionSheet, type ChatSheetAction } from "./chat-action-sheet";

export interface ThreadMoreMenuProps {
  readonly copy: ChatCopy;
  readonly following: boolean;
  readonly muted: boolean;
  /** A follow change is in flight: the entries wait. */
  readonly busy: boolean;
  readonly onToggleFollow: () => void;
  readonly onToggleMute: () => void;
  readonly onCopyLink: () => void;
}

/**
 * The thread "more" menu (Chat-Thread boards): follow or unfollow the thread, mute it while following, copy the link. A
 * 220 popup from 768 px, a bottom sheet titled "Thread" on a phone. Archiving a thread is not offered: no sweep stores it.
 */
export function ThreadMoreMenu({
  copy,
  following,
  muted,
  busy,
  onToggleFollow,
  onToggleMute,
  onCopyLink,
}: ThreadMoreMenuProps) {
  const isMobile = useIsMobile();
  const [sheetOpen, setSheetOpen] = useState(false);
  const text = copy.thread;
  const actions: ChatSheetAction[] = [
    {
      key: "follow",
      label: following ? text.unfollowThread : text.followThread,
      icon: following ? BellOff : BellPlus,
      onSelect: onToggleFollow,
    },
    ...(following
      ? [
          {
            key: "mute",
            label: muted ? text.unmute : text.mute,
            icon: muted ? BellRing : BellOff,
            onSelect: onToggleMute,
          },
        ]
      : []),
    { key: "link", label: text.copyLink, icon: Link2, onSelect: onCopyLink },
  ];
  const trigger = (
    <Button
      type="button"
      size="icon-sm"
      variant="ghost"
      className="text-muted-foreground"
      aria-label={text.moreActions}
      title={text.moreActions}
      aria-busy={busy}
      data-testid="chat-thread-more"
      onClick={isMobile ? () => setSheetOpen(true) : undefined}
    >
      <MoreHorizontal aria-hidden="true" />
    </Button>
  );
  if (isMobile) {
    return (
      <>
        {trigger}
        <ChatActionSheet
          open={sheetOpen}
          title={text.title}
          closeLabel={text.sheetClose}
          onOpenChange={setSheetOpen}
          testId="chat-thread-sheet"
          actions={actions}
        />
      </>
    );
  }
  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>{trigger}</DropdownMenuTrigger>
      <DropdownMenuContent
        align="end"
        aria-label={text.menuLabel}
        className="w-auto min-w-[220px] rounded-xl p-1.5"
      >
        {actions.map((action) => {
          const Icon = action.icon;
          return (
            <DropdownMenuItem
              key={action.key}
              disabled={busy && action.key !== "link"}
              className="min-h-9 gap-2.5 px-3.5 text-item-ui"
              onSelect={action.onSelect}
            >
              <Icon className="size-3.5" aria-hidden="true" />
              {action.label}
            </DropdownMenuItem>
          );
        })}
      </DropdownMenuContent>
    </DropdownMenu>
  );
}
