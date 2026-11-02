import {
  AlertCircle,
  Bell,
  Check,
  LayoutGrid,
  MailOpen,
  MessageSquare,
  Trash2,
  UserPlus,
} from "lucide-react";
import {
  Button,
  Popover,
  PopoverContent,
  PopoverTrigger,
} from "@notrelix/ui-web";
import type { Notification } from "../../core/types/notifications";
import {
  fillCopy,
  pluralCopy,
  type NotificationBellCopy,
} from "./notification-bell.copy";

type DateInput = Date | number | string;

/** Locale-bound formatters the surface needs; the container supplies them. */
export interface NotificationBellFormatters {
  readonly relativeTime: (value: DateInput, now?: DateInput) => string;
  readonly number: (value: number) => string;
}

export interface NotificationBellSurfaceProps {
  notifications: readonly Notification[];
  unreadCount?: number;
  status?: "idle" | "loading";
  /** Injected "now" for relative times; defaults to the formatter's clock. */
  referenceDate?: string;
  copy: NotificationBellCopy;
  formatters: NotificationBellFormatters;
  onMarkAllRead?: () => void;
  onMarkRead?: (id: string) => void;
  onArchive?: (id: string) => void;
}

/** Unread counts above this render as `99+` (the accessible name keeps the exact count). */
const UNREAD_BADGE_CAP = 99;

function getIcon(type: Notification["type"]) {
  switch (type) {
    case "mention":
      return <UserPlus className="h-4 w-4 text-info-text" aria-hidden="true" />;
    case "comment":
      return (
        <MessageSquare
          className="h-4 w-4 text-success-text"
          aria-hidden="true"
        />
      );
    case "assignment":
      return (
        <LayoutGrid className="h-4 w-4 text-warning-text" aria-hidden="true" />
      );
    case "invitation":
      return <UserPlus className="h-4 w-4 text-link" aria-hidden="true" />;
    case "status_change":
      return (
        <AlertCircle className="h-4 w-4 text-info-text" aria-hidden="true" />
      );
    case "system":
    default:
      return (
        <AlertCircle className="h-4 w-4 text-danger-text" aria-hidden="true" />
      );
  }
}

export function NotificationBellSurface({
  notifications,
  unreadCount = 0,
  status = "idle",
  referenceDate,
  copy,
  formatters,
  onMarkAllRead,
  onMarkRead,
  onArchive,
}: NotificationBellSurfaceProps) {
  const isLoading = status === "loading";
  const bellName =
    unreadCount > 0
      ? pluralCopy(
          { one: copy.bellUnread_one, other: copy.bellUnread_other },
          unreadCount,
        )
      : copy.bell;
  const badgeText =
    unreadCount > UNREAD_BADGE_CAP
      ? copy.unreadOverflow
      : formatters.number(unreadCount);

  return (
    <Popover>
      <PopoverTrigger asChild>
        <Button
          variant="ghost"
          size="icon"
          className="relative h-9 w-9 text-muted-foreground max-md:size-touch"
          aria-label={bellName}
          title={copy.bell}
        >
          <Bell className="h-4 w-4" aria-hidden="true" />
          {unreadCount > 0 ? (
            <span
              aria-hidden="true"
              className="absolute -top-0.5 -right-0.5 inline-flex h-4 min-w-4 items-center justify-center rounded-full bg-destructive px-1 text-caption font-semibold leading-none text-destructive-foreground tabular-nums"
            >
              {badgeText}
            </span>
          ) : null}
        </Button>
      </PopoverTrigger>
      <PopoverContent
        className="w-80 max-w-(--radix-popover-content-available-width) gap-0 p-0"
        align="end"
      >
        <div className="flex flex-wrap items-center justify-between gap-x-2 gap-y-1 border-b px-4 py-3">
          <span className="min-w-0 text-sm font-semibold">
            {copy.panelTitle}
          </span>
          {unreadCount > 0 ? (
            <Button
              variant="ghost"
              size="sm"
              onClick={onMarkAllRead}
              className="h-7 shrink-0 gap-1 text-xs text-link"
            >
              <Check className="h-3 w-3" aria-hidden="true" />
              {copy.markAllRead}
            </Button>
          ) : null}
        </div>
        <div className="max-h-72 overflow-y-auto">
          {isLoading ? (
            <div className="py-10 text-center text-xs text-muted-foreground">
              {copy.loading}
            </div>
          ) : notifications.length === 0 ? (
            <div className="py-10 text-center text-xs italic text-muted-foreground">
              {copy.empty}
            </div>
          ) : (
            <div className="divide-y">
              {notifications.map((notif) => (
                <div
                  key={notif.id}
                  className={`group relative flex items-start gap-3 p-3 text-left transition-colors hover:bg-muted/30 ${
                    !notif.isRead ? "bg-muted/10 font-medium" : ""
                  }`}
                >
                  <div className="mt-0.5 shrink-0 rounded-lg bg-muted/40 p-1.5">
                    {getIcon(notif.type)}
                  </div>
                  <div className="min-w-0 flex-1 space-y-0.5">
                    <p
                      className="truncate pr-14 text-xs leading-tight text-foreground"
                      title={notif.title}
                    >
                      {notif.title}
                    </p>
                    <p className="break-words text-caption leading-snug text-muted-foreground">
                      {notif.body}
                    </p>
                    <time
                      dateTime={notif.createdAt}
                      className="block text-caption text-muted-foreground"
                    >
                      {formatters.relativeTime(notif.createdAt, referenceDate)}
                    </time>
                  </div>
                  <div className="absolute right-2 top-2.5 flex items-center gap-1 rounded-md border bg-background/80 p-0.5 opacity-0 shadow-sm backdrop-blur-sm transition-opacity group-hover:opacity-100 group-focus-within:opacity-100 pointer-coarse:opacity-100">
                    {!notif.isRead ? (
                      <Button
                        variant="ghost"
                        size="icon"
                        className="h-5 w-5 hover:bg-primary/10 hover:text-link"
                        onClick={() => onMarkRead?.(notif.id)}
                        title={copy.markReadHint}
                        aria-label={fillCopy(copy.markRead, {
                          title: notif.title,
                        })}
                      >
                        <MailOpen className="h-3 w-3" aria-hidden="true" />
                      </Button>
                    ) : null}
                    <Button
                      variant="ghost"
                      size="icon"
                      className="h-5 w-5 hover:bg-destructive/10 hover:text-destructive"
                      onClick={() => onArchive?.(notif.id)}
                      title={copy.archiveHint}
                      aria-label={fillCopy(copy.archive, {
                        title: notif.title,
                      })}
                    >
                      <Trash2 className="h-3 w-3" aria-hidden="true" />
                    </Button>
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>
      </PopoverContent>
    </Popover>
  );
}
