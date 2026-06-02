"use client"

import { Clock, ArrowRight } from "lucide-react"
import { UPCOMING_DEADLINES } from "./workspace-mock-data"
import { cn } from "@/lib/utils"

const priorityConfig = {
  urgent: { dot: "bg-red-500", badge: "bg-red-50 text-red-700 dark:bg-red-950/40 dark:text-red-400" },
  high: { dot: "bg-amber-500", badge: "bg-amber-50 text-amber-700 dark:bg-amber-950/40 dark:text-amber-400" },
  medium: { dot: "bg-violet-500", badge: "bg-violet-50 text-violet-700 dark:bg-violet-950/40 dark:text-violet-400" },
  low: { dot: "bg-slate-400", badge: "bg-muted text-muted-foreground" },
}

function groupByDate(items: typeof UPCOMING_DEADLINES) {
  const groups: Record<string, typeof UPCOMING_DEADLINES> = {}
  items.forEach((item) => {
    if (!groups[item.dueDate]) groups[item.dueDate] = []
    groups[item.dueDate].push(item)
  })
  return groups
}

export function UpcomingDeadlines() {
  const grouped = groupByDate(UPCOMING_DEADLINES)

  return (
    <section className="rounded-2xl border border-border/50 bg-card overflow-hidden">
      {/* Header */}
      <div className="flex items-center justify-between px-5 pt-5 pb-3">
        <div className="flex items-center gap-2">
          <Clock className="size-4 text-muted-foreground" />
          <h3 className="text-[15px] font-semibold tracking-[-0.01em] text-foreground" style={{ fontFamily: "var(--font-poppins)" }}>
            Upcoming
          </h3>
        </div>
        <button className="flex items-center gap-1 text-xs font-medium text-muted-foreground hover:text-foreground transition-colors">
          All <ArrowRight className="size-3" />
        </button>
      </div>

      {/* Grouped Timeline */}
      <div className="px-5 pb-5 space-y-4">
        {Object.entries(grouped).map(([date, items]) => (
          <div key={date}>
            {/* Date Label */}
            <p className={cn(
              "text-[10px] font-semibold uppercase tracking-widest mb-2",
              date === "Today" ? "text-red-600 dark:text-red-400" : "text-muted-foreground"
            )}>
              {date}
            </p>

            {/* Items */}
            <div className="space-y-1.5">
              {items.map((item) => {
                const pConfig = priorityConfig[item.priority]
                return (
                  <div
                    key={item.id}
                    className="group flex items-center gap-2.5 rounded-lg px-2.5 py-2 -mx-1 transition-colors hover:bg-muted/50 cursor-pointer"
                  >
                    {/* Priority Dot */}
                    <span className={cn("size-2 rounded-full shrink-0", pConfig.dot)} />

                    {/* Content */}
                    <div className="flex-1 min-w-0">
                      <p className="text-sm text-foreground truncate leading-tight">
                        {item.title}
                      </p>
                      <p className="text-[11px] text-muted-foreground mt-0.5 truncate">
                        {item.source}
                      </p>
                    </div>

                    {/* Assignee */}
                    <div className="size-6 rounded-full bg-muted text-[9px] font-semibold flex items-center justify-center text-muted-foreground shrink-0">
                      {item.assignee}
                    </div>
                  </div>
                )
              })}
            </div>
          </div>
        ))}
      </div>
    </section>
  )
}
