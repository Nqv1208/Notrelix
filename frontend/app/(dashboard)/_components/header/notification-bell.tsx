"use client"

import { Bell } from "lucide-react"

export function NotificationBell() {
  const hasUnread = true // Mock state

  return (
    <button
      className="relative rounded-lg p-2 text-muted-foreground transition-colors hover:bg-muted hover:text-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
      aria-label="Notifications"
    >
      <Bell size={18} />
      {hasUnread && (
        <span className="absolute right-2 top-1.5 h-2 w-2 rounded-full border-2 border-card bg-destructive" />
      )}
    </button>
  )
}
