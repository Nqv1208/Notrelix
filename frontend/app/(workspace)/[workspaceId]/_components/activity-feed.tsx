"use client"

import { Activity, ArrowRight } from "lucide-react"
import { ACTIVITY_FEED } from "./workspace-mock-data"
import { cn } from "@/lib/utils"

const avatarColors = [
  "bg-violet-100 text-violet-700 dark:bg-violet-900/40 dark:text-violet-300",
  "bg-sky-100 text-sky-700 dark:bg-sky-900/40 dark:text-sky-300",
  "bg-amber-100 text-amber-700 dark:bg-amber-900/40 dark:text-amber-300",
  "bg-emerald-100 text-emerald-700 dark:bg-emerald-900/40 dark:text-emerald-300",
  "bg-rose-100 text-rose-700 dark:bg-rose-900/40 dark:text-rose-300",
  "bg-cyan-100 text-cyan-700 dark:bg-cyan-900/40 dark:text-cyan-300",
  "bg-indigo-100 text-indigo-700 dark:bg-indigo-900/40 dark:text-indigo-300",
]

export function ActivityFeed() {
  return (
    <section className="rounded-2xl border border-border/50 bg-card overflow-hidden">
      {/* Header */}
      <div className="flex items-center justify-between px-5 pt-5 pb-3">
        <div className="flex items-center gap-2">
          <Activity className="size-4 text-muted-foreground" />
          <h3 className="text-[15px] font-semibold tracking-[-0.01em] text-foreground" style={{ fontFamily: "var(--font-poppins)" }}>
            Activity
          </h3>
        </div>
        <button className="flex items-center gap-1 text-xs font-medium text-muted-foreground hover:text-foreground transition-colors">
          View all <ArrowRight className="size-3" />
        </button>
      </div>

      {/* Feed */}
      <div className="px-5 pb-5 space-y-0.5">
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
