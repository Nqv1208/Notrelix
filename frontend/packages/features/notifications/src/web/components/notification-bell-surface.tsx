import { useState } from "react";
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
import { Button } from "@notrelix/ui-web";
import type { Notification } from "../../core/types/notifications";

export interface NotificationBellSurfaceProps {
  notifications: readonly Notification[];
  unreadCount?: number;
  status?: "idle" | "loading";
  referenceDate?: string;
  onMarkAllRead?: () => void;
  onMarkRead?: (id: string) => void;
  onArchive?: (id: string) => void;
}

function getIcon(type: Notification["type"]) {
  switch (type) {
    case "mention":
      return <UserPlus className="h-4 w-4 text-blue-500" />;
    case "comment":
      return <MessageSquare className="h-4 w-4 text-green-500" />;
    case "assignment":
      return <LayoutGrid className="h-4 w-4 text-amber-500" />;
    case "invitation":
      return <UserPlus className="h-4 w-4 text-violet-500" />;
    case "status_change":
      return <AlertCircle className="h-4 w-4 text-cyan-500" />;
    case "system":
    default:
      return <AlertCircle className="h-4 w-4 text-red-500" />;
  }
}

function formatTime(isoString: string): string {
  const date = new Date(isoString);
  return date.toISOString().slice(0, 10);
}

export function NotificationBellSurface({
  notifications,
  unreadCount = 0,
  status = "idle",
  onMarkAllRead,
  onMarkRead,
  onArchive,
}: NotificationBellSurfaceProps) {
  const [open, setOpen] = useState(false);
  const isLoading = status === "loading";

  return (
    <div className="relative">
      <button
        type="button"
        aria-label="Notifications"
        onClick={() => setOpen((value) => !value)}
        className="relative inline-flex h-9 w-9 items-center justify-center rounded-lg text-muted-foreground transition hover:bg-muted hover:text-foreground"
      >
        <Bell className="h-4 w-4" />
        {unreadCount > 0 ? (
          <span className="absolute top-1.5 right-1.5 h-2 w-2 rounded-full bg-destructive animate-pulse" />
        ) : null}
      </button>

      {open ? (
        <div className="absolute right-0 top-11 z-20 w-80 rounded-xl border border-border bg-card p-0 shadow-xl">
          <div className="flex items-center justify-between px-4 py-3 border-b">
            <span className="text-sm font-semibold">Notifications</span>
            {unreadCount > 0 ? (
              <Button
                variant="ghost"
                size="sm"
                onClick={onMarkAllRead}
                className="h-7 text-xs gap-1 text-primary"
              >
                <Check className="h-3 w-3" /> Mark all read
              </Button>
            ) : null}
          </div>
          <div className="max-h-72 overflow-y-auto">
            {isLoading ? (
              <div className="text-center py-10 text-xs text-muted-foreground">
                Loading notifications...
              </div>
            ) : notifications.length === 0 ? (
              <div className="text-center py-10 text-xs text-muted-foreground italic">
                No notifications found
              </div>
            ) : (
              <div className="divide-y">
                {notifications.map((notif) => (
                  <div
                    key={notif.id}
                    className={`group relative flex items-start gap-3 space-y-1 p-3 text-left transition-colors hover:bg-muted/30 ${
                      !notif.isRead ? "bg-muted/10 font-medium" : ""
                    }`}
                  >
                    <div className="mt-0.5 shrink-0 bg-muted/40 p-1.5 rounded-lg">
                      {getIcon(notif.type)}
                    </div>
                    <div className="flex-1 space-y-0.5 overflow-hidden">
                      <p className="text-xs text-foreground leading-tight truncate">
                        {notif.title}
                      </p>
                      <p className="text-[11px] text-muted-foreground leading-snug">
                        {notif.body}
                      </p>
                      <span className="text-[10px] text-muted-foreground">
                        {formatTime(notif.createdAt)}
                      </span>
                    </div>
                    <div className="absolute right-2 top-2.5 flex items-center gap-1 rounded-md border bg-background/80 p-0.5 opacity-0 shadow-sm backdrop-blur-sm transition-opacity group-hover:opacity-100">
                      {!notif.isRead ? (
                        <button
                          type="button"
                          aria-label={`Mark ${notif.id} as read`}
                          className="inline-flex h-5 w-5 items-center justify-center rounded hover:bg-primary/10 hover:text-primary"
                          onClick={() => onMarkRead?.(notif.id)}
                        >
                          <MailOpen className="h-3 w-3" />
                        </button>
                      ) : null}
                      <button
                        type="button"
                        aria-label={`Archive ${notif.id}`}
                        className="inline-flex h-5 w-5 items-center justify-center rounded hover:bg-destructive/10 hover:text-destructive"
                        onClick={() => onArchive?.(notif.id)}
                      >
                        <Trash2 className="h-3 w-3" />
                      </button>
                    </div>
                  </div>
                ))}
              </div>
            )}
          </div>
        </div>
      ) : null}
    </div>
  );
}
