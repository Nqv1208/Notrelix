"use client"

import { Activity, ArrowRight } from "lucide-react"
import Link from "next/link"
import { ACTIVITY_FEED } from "./workspace-mock-data"
import { isMockModeEnabled } from "@/lib/config/mock-mode"
import type { WorkspaceSnapshot } from "@/features/workspace"
import { cn } from "@/lib/utils"
import { EmptyState } from "@/components/feedback"

interface ActivityFeedProps {
  workspaceId: string
  snapshot: WorkspaceSnapshot
}

const avatarColors = [
  "bg-violet-100 text-violet-700 dark:bg-violet-900/40 dark:text-violet-300",
  "bg-sky-100 text-sky-700 dark:bg-sky-900/40 dark:text-sky-300",
  "bg-amber-100 text-amber-700 dark:bg-amber-900/40 dark:text-amber-300",
  "bg-emerald-100 text-emerald-700 dark:bg-emerald-900/40 dark:text-emerald-300",
  "bg-rose-100 text-rose-700 dark:bg-rose-900/40 dark:text-rose-300",
  "bg-cyan-100 text-cyan-700 dark:bg-cyan-900/40 dark:text-cyan-300",
  "bg-indigo-100 text-indigo-700 dark:bg-indigo-900/40 dark:text-indigo-300",
]

export function ActivityFeed({ workspaceId, snapshot }: ActivityFeedProps) {
  const isMock = isMockModeEnabled("work-management")

  if (isMock) {
    return (
      <section className="rounded-2xl border border-border/50 bg-card p-5">
        {/* Header */}
        <div className="flex items-center justify-between mb-4">
          <div className="flex items-center gap-2">
            <Activity className="size-4 text-primary" />
            <h3 className="text-[15px] font-semibold tracking-[-0.01em] text-foreground" style={{ fontFamily: "var(--font-poppins)" }}>
              Activity
            </h3>
          </div>
          <Link
            href={`/${workspaceId}?panel=settings&tab=activity`}
            className="flex items-center gap-1 text-xs font-medium text-muted-foreground hover:text-foreground transition-colors"
          >
            View all <ArrowRight className="size-3" />
          </Link>
        </div>

        {/* Feed */}
        <div className="space-y-0.5">
          {ACTIVITY_FEED.map((item, i) => (
            <div
              key={item.id}
              className="group flex items-center gap-3 rounded-lg px-2.5 py-2.5 -mx-1 transition-colors hover:bg-muted/50 cursor-pointer"
            >
              {/* Avatar */}
              <div className={cn(
                "size-7 rounded-full flex items-center justify-center text-[10px] font-semibold shrink-0",
                avatarColors[i % avatarColors.length]
              )}>
                {item.actorInitials}
              </div>

              {/* Content */}
              <div className="flex-1 min-w-0">
                <p className="text-sm text-foreground leading-snug truncate">
                  <span className="font-medium">{item.actor}</span>
                  <span className="text-muted-foreground"> {item.action} </span>
                  <span className="font-medium">{item.target}</span>
                </p>
              </div>

              {/* Timestamp */}
              <span className="text-[11px] text-muted-foreground shrink-0 tabular-nums">
                {item.timestamp}
              </span>
            </div>
          ))}
        </div>
      </section>
    )
  }

  // Real data mode
  const activityList = snapshot.activity

  if (activityList.length === 0) {
    return (
      <section className="rounded-2xl border border-border/50 bg-card p-5">
        <div className="flex items-center gap-2 mb-4">
          <Activity className="size-4 text-primary" />
          <h3 className="text-[15px] font-semibold tracking-[-0.01em] text-foreground" style={{ fontFamily: "var(--font-poppins)" }}>
            Activity
          </h3>
        </div>
        <EmptyState
          title="No activity yet"
          description="Actions performed in this workspace will appear here."
          className="py-6"
        />
      </section>
    )
  }

  return (
    <section className="rounded-2xl border border-border/50 bg-card p-5">
      {/* Header */}
      <div className="flex items-center justify-between mb-4">
        <div className="flex items-center gap-2">
          <Activity className="size-4 text-primary" />
          <h3 className="text-[15px] font-semibold tracking-[-0.01em] text-foreground" style={{ fontFamily: "var(--font-poppins)" }}>
            Activity
          </h3>
        </div>
        <Link
          href={`/${workspaceId}?panel=settings&tab=activity`}
          className="flex items-center gap-1 text-xs font-medium text-muted-foreground hover:text-foreground transition-colors"
        >
          View all <ArrowRight className="size-3" />
        </Link>
      </div>

      {/* Feed */}
      <div className="space-y-0.5">
        {activityList.map((item, i) => {
          const initials = item.actor.split(" ").map(n => n[0]).join("").toUpperCase().slice(0, 2)
          return (
            <div
              key={item.id}
              className="group flex items-center gap-3 rounded-lg px-2.5 py-2.5 -mx-1 transition-colors hover:bg-muted/50 cursor-pointer"
            >
              {/* Avatar */}
              <div className={cn(
                "size-7 rounded-full flex items-center justify-center text-[10px] font-semibold shrink-0",
                avatarColors[i % avatarColors.length]
              )}>
                {initials}
              </div>

              {/* Content */}
              <div className="flex-1 min-w-0">
                <p className="text-sm text-foreground leading-snug truncate">
                  <span className="font-medium">{item.actor}</span>
                  <span className="text-muted-foreground"> {item.action} </span>
                  <span className="font-medium">{item.target}</span>
                </p>
              </div>

              {/* Timestamp */}
              <span className="text-[11px] text-muted-foreground shrink-0 tabular-nums">
                {item.createdAt}
              </span>
            </div>
          )
        })}
      </div>
    </section>
  )
}
