"use client"

import { useState } from "react"
import { useRouter } from "next/navigation"
import { Bell, Check, Loader2, Mail, Inbox, CheckCheck, ShieldAlert, Calendar } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Popover, PopoverContent, PopoverTrigger } from "@/components/ui/popover"
import { useNotifications, useMarkNotificationRead, useMarkAllNotificationsRead } from "@/features/notifications"
import { useAcceptInvitation } from "@/features/workspace/hooks"
import { cn } from "@/lib/utils"

export function NotificationBell() {
  const router = useRouter()
  const [open, setOpen] = useState(false)
  const { data: notifications, isLoading } = useNotifications()
  const markReadMutation = useMarkNotificationRead()
  const markAllReadMutation = useMarkAllNotificationsRead()
  const acceptMutation = useAcceptInvitation()
  const [acceptingToken, setAcceptingToken] = useState<string | null>(null)

  const unreadNotifications = notifications?.filter(n => !n.isRead) || []
  const hasUnread = unreadNotifications.length > 0

  const handleMarkRead = (id: string) => {
    markReadMutation.mutate(id)
  }

  const handleMarkAllRead = () => {
    if (hasUnread) {
      markAllReadMutation.mutate()
    }
  }

  const handleAcceptInvite = (token: string, notificationId: string) => {
    setAcceptingToken(token)
    acceptMutation.mutate(token, {
      onSuccess: (data) => {
        setOpen(false)
        setAcceptingToken(null)
        // Đánh dấu thông báo là đã đọc
        markReadMutation.mutate(notificationId)
        
        if (data && data.workspaceSlug) {
          router.push(`/${data.workspaceSlug}`)
        } else {
          router.push("/")
        }
      },
      onError: () => {
        setAcceptingToken(null)
      }
    })
  }

  return (
    <Popover open={open} onOpenChange={setOpen}>
      <PopoverTrigger asChild>
        <button
          type="button"
          className={cn(
            "relative rounded-lg p-2 text-muted-foreground transition-all hover:bg-muted hover:text-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring",
            hasUnread && "text-primary animate-pulse"
          )}
          aria-label="Notifications"
        >
          <Bell className="size-[18px]" />
          {hasUnread && (
            <span className="absolute right-1 top-1 flex size-4 items-center justify-center rounded-full border border-card bg-destructive text-[9px] font-bold text-white shadow-sm">
              {unreadNotifications.length}
            </span>
          )}
        </button>
      </PopoverTrigger>

      <PopoverContent align="end" className="w-80 p-0 border-border/60 bg-card/95 shadow-xl backdrop-blur-md rounded-2xl overflow-hidden z-[100]">
        <div className="flex items-center justify-between border-b border-border/40 px-4 py-3 bg-muted/30">
          <h4 className="text-sm font-semibold text-foreground flex items-center gap-2">
            <Bell className="size-4 text-primary" />
            Thông báo ({unreadNotifications.length} chưa đọc)
          </h4>
          {hasUnread && (
            <Button
              variant="ghost"
              size="sm"
              onClick={handleMarkAllRead}
              disabled={markAllReadMutation.isPending}
              className="h-7 px-2 rounded-lg text-xs font-medium text-muted-foreground hover:text-primary gap-1"
            >
              {markAllReadMutation.isPending ? (
                <Loader2 className="size-3 animate-spin" />
              ) : (
                <CheckCheck className="size-3.5" />
              )}
              Đọc tất cả
            </Button>
          )}
        </div>

        <div className="max-h-80 overflow-y-auto divide-y divide-border/40">
          {isLoading && !notifications ? (
            <div className="flex flex-col items-center justify-center py-8 text-center text-xs text-muted-foreground gap-2">
              <Loader2 className="size-6 animate-spin text-primary" />
              <span>Đang tải thông báo...</span>
            </div>
          ) : !notifications || notifications.length === 0 ? (
            <div className="flex flex-col items-center justify-center py-8 px-4 text-center text-xs text-muted-foreground gap-2">
              <div className="rounded-full bg-muted p-2 text-muted-foreground/60">
                <Inbox className="size-5" />
              </div>
              <p className="font-medium text-foreground/80">Hộp thư trống</p>
              <p className="text-[11px] text-muted-foreground/80 max-w-[200px]">
                Bạn sẽ nhận được các thông báo cập nhật công việc hoặc lời mời tại đây.
              </p>
            </div>
          ) : (
            notifications.map((notification) => {
              const isInvitation = notification.type === "invitation"
              let inviteData = { token: "", workspaceName: "", invitationId: "" }
              
              if (isInvitation) {
                try {
                  inviteData = JSON.parse(notification.payload)
                } catch (e) {
                  console.error("Lỗi parse payload notification:", e)
                }
              }

              return (
                <div
                  key={notification.id}
                  onClick={() => !notification.isRead && !isInvitation && handleMarkRead(notification.id)}
                  className={cn(
                    "p-3.5 transition-colors text-left text-xs relative",
                    !notification.isRead ? "bg-primary/5 hover:bg-primary/10 cursor-pointer" : "hover:bg-muted/10",
                    !notification.isRead && "after:absolute after:left-1 after:top-1/2 after:-translate-y-1/2 after:size-1.5 after:rounded-full after:bg-primary"
                  )}
                >
                  <div className="space-y-1.5 pl-2">
                    <div className="flex justify-between items-start gap-1">
                      <span className="font-bold text-foreground text-[13px] leading-snug">
                        {isInvitation ? "Lời mời Workspace" : "Thông báo hệ thống"}
                      </span>
                      <span className="text-[10px] text-muted-foreground whitespace-nowrap">
                        {new Date(notification.createdAt).toLocaleDateString("vi-VN")}
                      </span>
                    </div>

                    <div className="text-muted-foreground leading-relaxed text-xs">
                      {isInvitation ? (
                        <>
                          <strong className="text-foreground/90">{notification.actorName}</strong> đã mời bạn tham gia workspace{" "}
                          <strong className="text-foreground/90">{inviteData.workspaceName}</strong>.
                        </>
                      ) : (
                        notification.payload
                      )}
                    </div>

                    {isInvitation && !notification.isRead && (
                      <div className="flex items-center gap-2 pt-2">
                        <Button
                          size="sm"
                          onClick={(e) => {
                            e.stopPropagation()
                            handleAcceptInvite(inviteData.token, notification.id)
                          }}
                          disabled={acceptingToken === inviteData.token || acceptMutation.isPending}
                          className="h-7 px-3 rounded-lg text-xs font-semibold gap-1 bg-emerald-600 hover:bg-emerald-500 text-white shadow-sm"
                        >
                          {acceptingToken === inviteData.token ? (
                            <Loader2 className="size-3 animate-spin" />
                          ) : (
                            <Check className="size-3" />
                          )}
                          Chấp nhận
                        </Button>
                        <Button
                          size="sm"
                          variant="ghost"
                          onClick={(e) => {
                            e.stopPropagation()
                            handleMarkRead(notification.id)
                          }}
                          className="h-7 px-2 rounded-lg text-xs text-muted-foreground hover:text-foreground"
                        >
                          Bỏ qua
                        </Button>
                      </div>
                    )}
                  </div>
                </div>
              )
            })
          )}
        </div>
      </PopoverContent>
    </Popover>
  )
}
