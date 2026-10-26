import { Bell } from "lucide-react";
import { useFormatters } from "@notrelix/i18n";
import { Button } from "@notrelix/ui-web";
import { pluralCopy } from "./notification-bell.copy";
import { useNotificationBellCopy } from "./use-notification-bell-copy";

const UNREAD_BADGE_CAP = 99;

export interface InboxBellProps {
  /** Unread notifications of the current workspace's Inbox. */
  readonly unreadCount: number;
  /** Opens the Inbox, where the notifications are read and answered. */
  readonly onOpen: () => void;
}

/**
 * The bell of the phone top bar: the workspace Inbox's unread count, and a
 * tap opens the Inbox. The notifications themselves live in the Inbox
 * (`Collaboration.Notifications.*`), not in a second list here.
 */
export function InboxBell({ unreadCount, onOpen }: InboxBellProps) {
  const copy = useNotificationBellCopy();
  const formatters = useFormatters();
  const name =
    unreadCount > 0
      ? pluralCopy(
          { one: copy.bellUnread_one, other: copy.bellUnread_other },
          unreadCount,
        )
      : copy.bell;
  return (
    <Button
      variant="ghost"
      size="icon"
      className="relative h-9 w-9 text-muted-foreground"
      aria-label={name}
      title={copy.bell}
      onClick={onOpen}
    >
      <Bell className="h-4 w-4" aria-hidden="true" />
      {unreadCount > 0 ? (
        <span
          aria-hidden="true"
          className="absolute -top-0.5 -right-0.5 inline-flex h-4 min-w-4 items-center justify-center rounded-full bg-destructive px-1 text-caption font-semibold leading-none text-destructive-foreground tabular-nums"
        >
          {unreadCount > UNREAD_BADGE_CAP
            ? copy.unreadOverflow
            : formatters.number(unreadCount)}
        </span>
      ) : null}
    </Button>
  );
}
