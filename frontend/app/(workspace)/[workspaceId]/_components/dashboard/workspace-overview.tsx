"use client"

import { FileText, Kanban, CheckSquare, Users } from "lucide-react"
import { WORKSPACE_STATS } from "./workspace-mock-data"

const stats = [
  { label: "Pages", value: WORKSPACE_STATS.totalPages, icon: FileText, color: "text-blue-600 dark:text-blue-400", bg: "bg-blue-50 dark:bg-blue-950/40" },
  { label: "Active Boards", value: WORKSPACE_STATS.activeBoards, icon: Kanban, color: "text-violet-600 dark:text-violet-400", bg: "bg-violet-50 dark:bg-violet-950/40" },
  { label: "Pending Tasks", value: WORKSPACE_STATS.pendingTasks, icon: CheckSquare, color: "text-amber-600 dark:text-amber-400", bg: "bg-amber-50 dark:bg-amber-950/40" },
  { label: "Team Members", value: WORKSPACE_STATS.teamMembers, icon: Users, color: "text-emerald-600 dark:text-emerald-400", bg: "bg-emerald-50 dark:bg-emerald-950/40" },
]

export function WorkspaceOverview() {
  const hour = new Date().getHours()
  const greeting = hour < 12 ? "Good morning" : hour < 18 ? "Good afternoon" : "Good evening"

  return (
    <section className="space-y-5">
      {/* Greeting */}
      <div>
        <h2
          className="text-lg font-medium tracking-[-0.01em] text-foreground"
          style={{ fontFamily: "var(--font-poppins)" }}
        >
          {greeting} 👋
        </h2>
        <p className="text-sm text-muted-foreground mt-0.5">
          Here&apos;s what&apos;s happening in your workspace today.
        </p>
      </div>

      {/* Stats Grid */}
      <div className="grid grid-cols-2 lg:grid-cols-4 gap-3">
        {stats.map((stat) => (
          <div
            key={stat.label}
            className="group relative flex items-center gap-3 rounded-2xl border border-border/50 bg-card p-4 transition-all duration-150 hover:border-border hover:shadow-sm"
          >
            <div className={`flex items-center justify-center size-10 rounded-xl ${stat.bg} shrink-0`}>
              <stat.icon className={`size-[18px] ${stat.color} stroke-[1.75]`} />
            </div>
            <div>
              <p className="text-2xl font-semibold tracking-tight text-foreground leading-none" style={{ fontFamily: "var(--font-poppins)" }}>
                {stat.value}
              </p>
              <p className="text-xs text-muted-foreground mt-1 font-medium">{stat.label}</p>
            </div>
          </div>
        ))}
      </div>
    </section>
  )
}
