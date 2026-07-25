import React, { useState } from 'react';
import {
  createUseNotifications,
  createUseUnreadCount,
  createUseMarkRead,
  createUseArchiveNotification,
} from '../../core';
import type { NotificationsApiClient, NotificationsEndpoints } from '../../core/api/notifications.service';
import {
  Button,
  Popover,
  PopoverTrigger,
  PopoverContent,
  ScrollArea,
} from '@notrelix/ui-web';
import { Bell, Check, Trash2, MailOpen, MessageSquare, AlertCircle, UserPlus, LayoutGrid } from 'lucide-react';

interface CreateNotificationBellDeps {
  api: NotificationsApiClient;
  endpoints: NotificationsEndpoints;
  options?: {
    mockMode?: boolean;
  };
}

export function createNotificationBell(deps: CreateNotificationBellDeps) {
  const useNotifications = createUseNotifications(deps);
  const useUnreadCount = createUseUnreadCount(deps);
  const useMarkRead = createUseMarkRead(deps);
  const useArchiveNotification = createUseArchiveNotification(deps);

  return function NotificationBell() {
    const { data: notifications = [], isLoading: listLoading } = useNotifications();
    const { data: countData } = useUnreadCount();
    const { markRead, markAllRead } = useMarkRead();
    const archiveMutation = useArchiveNotification();

    const [open, setOpen] = useState(false);
    const unreadCount = countData?.count ?? 0;

    const handleMarkAllRead = () => {
      markAllRead();
    };

    const handleMarkRead = (id: string, e: React.MouseEvent) => {
      e.stopPropagation();
      markRead(id);
    };

    const handleArchive = (id: string, e: React.MouseEvent) => {
      e.stopPropagation();
      archiveMutation.mutate(id);
    };

    const formatTime = (isoString: string) => {
      const date = new Date(isoString);
      const seconds = Math.floor((Date.now() - date.getTime()) / 1000);
      if (seconds < 60) return 'Just now';
      const minutes = Math.floor(seconds / 60);
      if (minutes < 60) return `${minutes}m ago`;
      const hours = Math.floor(minutes / 60);
      if (hours < 24) return `${hours}h ago`;
      return date.toLocaleDateString();
    };

    const getIcon = (type: string) => {
      switch (type) {
        case 'mention':
          return <UserPlus className="h-4 w-4 text-blue-500" />;
        case 'comment':
          return <MessageSquare className="h-4 w-4 text-green-500" />;
        case 'assignment':
          return <LayoutGrid className="h-4 w-4 text-amber-500" />;
        case 'system':
        default:
          return <AlertCircle className="h-4 w-4 text-red-500" />;
      }
    };

    return (
      <Popover open={open} onOpenChange={setOpen}>
        <PopoverTrigger asChild>
          <Button
            variant="ghost"
            size="icon"
            className="h-9 w-9 text-muted-foreground relative"
            title="Notifications"
          >
            <Bell className="h-4 w-4" />
            {unreadCount > 0 && (
              <span className="absolute top-1.5 right-1.5 h-2 w-2 rounded-full bg-destructive animate-pulse" />
            )}
          </Button>
        </PopoverTrigger>
        <PopoverContent className="w-80 p-0" align="end">
          <div className="flex items-center justify-between px-4 py-3 border-b">
            <span className="text-sm font-semibold">Notifications</span>
            {unreadCount > 0 && (
              <Button
                variant="ghost"
                size="sm"
                onClick={handleMarkAllRead}
                className="h-7 text-xs gap-1 text-primary hover:text-primary-foreground"
              >
                <Check className="h-3 w-3" /> Mark all read
              </Button>
            )}
          </div>
          <ScrollArea className="h-72">
            {listLoading ? (
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
                    className={`p-3 text-left space-y-1 hover:bg-muted/30 transition-colors flex items-start gap-3 relative group ${
                      !notif.isRead ? 'bg-muted/10 font-medium' : ''
                    }`}
                  >
                    <div className="mt-0.5 shrink-0 bg-muted/40 p-1.5 rounded-lg">
                      {getIcon(notif.type)}
                    </div>
                    <div className="flex-1 space-y-0.5 overflow-hidden">
                      <p className="text-xs text-foreground leading-tight truncate">{notif.title}</p>
                      <p className="text-[11px] text-muted-foreground leading-snug">{notif.body}</p>
                      <span className="text-[10px] text-muted-foreground">{formatTime(notif.createdAt)}</span>
                    </div>

                    {/* Actions Panel */}
                    <div className="opacity-0 group-hover:opacity-100 transition-opacity absolute right-2 top-2.5 flex items-center gap-1 bg-background/80 backdrop-blur-sm p-0.5 rounded-md shadow-sm border">
                      {!notif.isRead && (
                        <Button
                          variant="ghost"
                          size="icon"
                          className="h-5 w-5 hover:bg-primary/10 hover:text-primary"
                          onClick={(e) => handleMarkRead(notif.id, e)}
                          title="Mark as read"
                        >
                          <MailOpen className="h-3 w-3" />
                        </Button>
                      )}
                      <Button
                        variant="ghost"
                        size="icon"
                        className="h-5 w-5 hover:bg-destructive/10 hover:text-destructive"
                        onClick={(e) => handleArchive(notif.id, e)}
                        title="Archive"
                      >
                        <Trash2 className="h-3 w-3" />
                      </Button>
                    </div>
                  </div>
                ))}
              </div>
            )}
          </ScrollArea>
        </PopoverContent>
      </Popover>
    );
  };
}
