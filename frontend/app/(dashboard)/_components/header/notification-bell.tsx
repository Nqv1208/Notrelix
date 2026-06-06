"use client"

import { useState } from "react"
import { useRouter } from "next/navigation"
import {
  Bell,
  Check,
  Loader2,
  Mail,
  Inbox,
  CheckCheck,
  ShieldAlert,
  Calendar,
  UserPlus,
  MessageSquare,
  AlertCircle,
  FileText,
  CheckCircle2,
  X,
  BellOff
} from "lucide-react"
import { Button } from "@/components/ui/button"
import { Popover, PopoverContent, PopoverTrigger } from "@/components/ui/popover"
import { useNotifications, useMarkNotificationRead, useMarkAllNotificationsRead } from "@/features/notifications"
import { useAcceptInvitation } from "@/features/workspace/hooks"
import { cn } from "@/lib/utils"

// Helper format thời gian tương đối như Monday.com
function formatRelativeTime(dateString: string) {
  const date = new Date(dateString)
  const now = new Date()
  const diffInSeconds = Math.floor((now.getTime() - date.getTime()) / 1000)

  if (diffInSeconds < 30) return "Vừa xong"
  if (diffInSeconds < 60) return `${diffInSeconds} giây trước`
  
  const diffInMinutes = Math.floor(diffInSeconds / 60)
  if (diffInMinutes < 60) return `${diffInMinutes} phút trước`

  const diffInHours = Math.floor(diffInMinutes / 60)
  if (diffInHours < 24) return `${diffInHours} giờ trước`

  const diffInDays = Math.floor(diffInHours / 24)
  if (diffInDays === 1) return "Hôm qua"
  if (diffInDays < 7) return `${diffInDays} ngày trước`

  return date.toLocaleDateString("vi-VN", { day: "numeric", month: "short" })
}

// Sinh màu ngẫu nhiên dựa trên tên của người gửi để avatar đồng bộ
function getAvatarColor(name: string) {
  const colors = [
    "bg-red-500/15 text-red-600 dark:text-red-400",
    "bg-orange-500/15 text-orange-600 dark:text-orange-400",
    "bg-amber-500/15 text-amber-600 dark:text-amber-400",
    "bg-emerald-500/15 text-emerald-600 dark:text-emerald-400",
    "bg-blue-500/15 text-blue-600 dark:text-blue-400",
    "bg-indigo-500/15 text-indigo-600 dark:text-indigo-400",
    "bg-violet-500/15 text-violet-600 dark:text-violet-400",
    "bg-purple-500/15 text-purple-600 dark:text-purple-400",
    "bg-pink-500/15 text-pink-600 dark:text-pink-400",
    "bg-rose-500/15 text-rose-600 dark:text-rose-400",
  ]
  
  let hash = 0
  const cleanName = name || "System"
  for (let i = 0; i < cleanName.length; i++) {
    hash = cleanName.charCodeAt(i) + ((hash << 5) - hash)
  }
  
  const index = Math.abs(hash) % colors.length
  return colors[index]
}

function getInitials(name: string) {
  if (!name) return "HT"
  const parts = name.split(" ")
  if (parts.length === 1) return parts[0].substring(0, 2).toUpperCase()
  return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase()
}

type TabType = "all" | "unread" | "invitations"

export function NotificationBell() {
  const router = useRouter()
  const [open, setOpen] = useState(false)
  const [activeTab, setActiveTab] = useState<TabType>("all")
  
  const { data: notifications, isLoading } = useNotifications()
  const markReadMutation = useMarkNotificationRead()
  const markAllReadMutation = useMarkAllNotificationsRead()
  const acceptMutation = useAcceptInvitation()
  const [acceptingToken, setAcceptingToken] = useState<string | null>(null)

  const unreadNotifications = notifications?.filter(n => !n.isRead) || []
  const invitationNotifications = notifications?.filter(n => n.type === "invitation") || []
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

  // Lọc thông báo theo tab đang chọn
  const filteredNotifications = notifications?.filter(notification => {
    if (activeTab === "unread") return !notification.isRead
    if (activeTab === "invitations") return notification.type === "invitation"
    return true
  }) || []

  // Nhận diện icon dựa trên loại thông báo
  const getNotificationIcon = (type: string) => {
    switch (type) {
      case "invitation":
        return <UserPlus className="size-3 text-emerald-500" />
      case "comment":
        return <MessageSquare className="size-3 text-blue-500" />
      case "mention":
        return <AlertCircle className="size-3 text-amber-500" />
      case "task":
      case "card":
        return <FileText className="size-3 text-violet-500" />
      case "calendar":
        return <Calendar className="size-3 text-pink-500" />
      default:
        return <Bell className="size-3 text-muted-foreground" />
    }
  }

  return (
    <Popover open={open} onOpenChange={setOpen}>
      <PopoverTrigger asChild>
        <button
          type="button"
          className={cn(
            "relative rounded-lg p-2 text-muted-foreground transition-all hover:bg-muted hover:text-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring",
            hasUnread && "text-foreground"
          )}
          aria-label="Notifications"
        >
          <Bell className="size-[18px]" />
          {hasUnread && (
            <span className="absolute right-1.5 top-1.5 flex size-2 items-center justify-center rounded-full bg-primary ring-2 ring-background">
              <span className="absolute inline-flex h-full w-full animate-ping rounded-full bg-primary opacity-75"></span>
            </span>
          )}
        </button>
      </PopoverTrigger>

      <PopoverContent align="end" className="w-[380px] p-0 border border-border/80 bg-card/98 shadow-xl backdrop-blur-md rounded-2xl overflow-hidden z-[100]">
        {/* Header */}
        <div className="flex items-center justify-between px-4 py-3 border-b border-border/40 bg-muted/20">
          <div>
            <h4 className="text-sm font-semibold text-foreground">
              Thông báo
            </h4>
            {hasUnread && (
              <p className="text-[10.5px] text-muted-foreground mt-0.5">
                Bạn có {unreadNotifications.length} thông báo chưa đọc
              </p>
            )}
          </div>
          {hasUnread && (
            <Button
              variant="ghost"
              size="sm"
              onClick={handleMarkAllRead}
              disabled={markAllReadMutation.isPending}
              className="h-7 px-2.5 rounded-lg text-xs font-semibold text-primary hover:bg-primary/5 hover:text-primary gap-1"
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

        {/* Tabs Filter - chuẩn Monday.com style */}
        <div className="flex border-b border-border/40 px-2 pt-1 bg-muted/10">
          <button
            onClick={() => setActiveTab("all")}
            className={cn(
              "px-3 py-2 text-xs font-medium border-b-2 transition-all relative",
              activeTab === "all"
                ? "border-primary text-primary font-semibold"
                : "border-transparent text-muted-foreground hover:text-foreground"
            )}
          >
            Tất cả
            {notifications && notifications.length > 0 && (
              <span className="ml-1.5 text-[10px] bg-muted px-1.5 py-0.5 rounded-full text-muted-foreground font-medium">
                {notifications.length}
              </span>
            )}
          </button>
          
          <button
            onClick={() => setActiveTab("unread")}
            className={cn(
              "px-3 py-2 text-xs font-medium border-b-2 transition-all relative",
              activeTab === "unread"
                ? "border-primary text-primary font-semibold"
                : "border-transparent text-muted-foreground hover:text-foreground"
            )}
          >
            Chưa đọc
            {hasUnread && (
              <span className="ml-1.5 text-[10px] bg-primary/10 px-1.5 py-0.5 rounded-full text-primary font-bold">
                {unreadNotifications.length}
              </span>
            )}
          </button>

          <button
            onClick={() => setActiveTab("invitations")}
            className={cn(
              "px-3 py-2 text-xs font-medium border-b-2 transition-all relative",
              activeTab === "invitations"
                ? "border-primary text-primary font-semibold"
                : "border-transparent text-muted-foreground hover:text-foreground"
            )}
          >
            Lời mời
            {invitationNotifications.length > 0 && (
              <span className="ml-1.5 text-[10px] bg-emerald-500/10 px-1.5 py-0.5 rounded-full text-emerald-600 dark:text-emerald-400 font-bold">
                {invitationNotifications.length}
              </span>
            )}
          </button>
        </div>

        {/* Danh sách thông báo */}
        <div className="max-h-[380px] overflow-y-auto divide-y divide-border/30">
          {isLoading && !notifications ? (
            <div className="flex flex-col items-center justify-center py-12 text-center text-xs text-muted-foreground gap-2">
              <Loader2 className="size-6 animate-spin text-primary" />
              <span className="font-medium text-[13px]">Đang tải thông báo...</span>
            </div>
          ) : filteredNotifications.length === 0 ? (
            <div className="flex flex-col items-center justify-center py-12 px-6 text-center text-muted-foreground gap-3">
              <div className="rounded-full bg-muted/60 p-3.5 text-muted-foreground/50 border border-border/20 shadow-inner">
                <BellOff className="size-6" />
              </div>
              <div className="space-y-1">
                <p className="font-semibold text-foreground/80 text-[13px]">Hộp thư trống</p>
                <p className="text-[11px] text-muted-foreground max-w-[240px] leading-normal">
                  {activeTab === "unread" 
                    ? "Tuyệt vời! Bạn đã đọc toàn bộ các thông báo." 
                    : activeTab === "invitations"
                    ? "Hiện không có lời mời tham gia workspace nào mới."
                    : "Bạn sẽ nhận được các thông báo cập nhật công việc hoặc lời mời tại đây."}
                </p>
              </div>
            </div>
          ) : (
            filteredNotifications.map((notification) => {
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
                    "group flex items-start gap-3 p-4 transition-all duration-200 text-left relative overflow-hidden",
                    !notification.isRead 
                      ? "bg-primary/5 hover:bg-primary/[0.08] cursor-pointer" 
                      : "hover:bg-muted/15",
                    !notification.isRead && "after:absolute after:left-0 after:top-0 after:bottom-0 after:w-1 after:bg-primary"
                  )}
                >
                  {/* Left Column: Avatar / Type Icon */}
                  <div className="relative flex-shrink-0">
                    <div className={cn(
                      "size-9 rounded-full flex items-center justify-center text-xs font-semibold shadow-sm",
                      getAvatarColor(notification.actorName)
                    )}>
                      {getInitials(notification.actorName)}
                    </div>
                    {/* Small type badge on bottom-right of avatar */}
                    <div className="absolute -bottom-1 -right-1 size-5 rounded-full border border-background bg-card flex items-center justify-center shadow-sm">
                      {getNotificationIcon(notification.type)}
                    </div>
                  </div>

                  {/* Middle Column: Details */}
                  <div className="flex-1 min-w-0 space-y-1">
                    <div className="flex items-center justify-between gap-2">
                      <span className="font-semibold text-foreground text-[12.5px] truncate">
                        {notification.actorName || "Hệ thống"}
                      </span>
                      <span className="text-[10px] text-muted-foreground flex-shrink-0">
                        {formatRelativeTime(notification.createdAt)}
                      </span>
                    </div>

                    <p className="text-muted-foreground text-xs leading-relaxed break-words">
                      {isInvitation ? (
                        <>
                          đã mời bạn tham gia workspace{" "}
                          <strong className="text-foreground font-semibold">{inviteData.workspaceName}</strong>.
                        </>
                      ) : (
                        notification.payload
                      )}
                    </p>

                    {/* Meta info & Action buttons */}
                    <div className="flex flex-col gap-2 pt-1">
                      {/* Workspace name label */}
                      {notification.workspaceName && (
                        <div className="inline-flex items-center gap-1 text-[10px] font-medium text-muted-foreground bg-muted px-1.5 py-0.5 rounded w-fit">
                          <span className="size-1.5 rounded-full bg-primary/60" />
                          {notification.workspaceName}
                        </div>
                      )}

                      {/* Lời mời Workspace - các nút Accept / Decline */}
                      {isInvitation && !notification.isRead && (
                        <div className="flex items-center gap-2 pt-1.5">
                          <Button
                            size="sm"
                            onClick={(e) => {
                              e.stopPropagation()
                              handleAcceptInvite(inviteData.token, notification.id)
                            }}
                            disabled={acceptingToken === inviteData.token || acceptMutation.isPending}
                            className="h-7 px-3 rounded-lg text-xs font-semibold gap-1 bg-emerald-600 hover:bg-emerald-500 text-white shadow-sm transition-all"
                          >
                            {acceptingToken === inviteData.token ? (
                              <Loader2 className="size-3 animate-spin" />
                            ) : (
                              <Check className="size-3" />
                            )}
                            Đồng ý
                          </Button>
                          <Button
                            size="sm"
                            variant="outline"
                            onClick={(e) => {
                              e.stopPropagation()
                              handleMarkRead(notification.id)
                            }}
                            className="h-7 px-2.5 rounded-lg text-xs font-medium border-border hover:bg-muted text-muted-foreground hover:text-foreground transition-all"
                          >
                            Bỏ qua
                          </Button>
                        </div>
                      )}
                    </div>
                  </div>

                  {/* Right Column: Mark Read dot/action */}
                  {!notification.isRead && !isInvitation && (
                    <div className="flex-shrink-0 flex items-center justify-center self-center pl-1">
                      {/* Clickable Blue Dot that turns into Checkmark on hover */}
                      <button
                        onClick={(e) => {
                          e.stopPropagation()
                          handleMarkRead(notification.id)
                        }}
                        className="relative size-5 flex items-center justify-center rounded-full hover:bg-primary/10 transition-all text-primary"
                        title="Đánh dấu đã đọc"
                      >
                        <span className="size-2 rounded-full bg-primary group-hover:hidden transition-all duration-200" />
                        <Check className="size-3.5 hidden group-hover:block transition-all duration-200" />
                      </button>
                    </div>
                  )}
                </div>
              )
            })
          )}
        </div>
      </PopoverContent>
    </Popover>
  )
}

