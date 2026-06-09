"use client"

import { BarChart, CheckCircle2, TrendingUp, Users, Zap, ShieldCheck } from "lucide-react"

export function DashboardMock() {
  const statusSummary = [
    { name: "Done", count: 8, color: "bg-emerald-500", pct: "60%" },
    { name: "Review", count: 2, color: "bg-amber-500", pct: "15%" },
    { name: "In Progress", count: 2, color: "bg-blue-500", pct: "15%" },
    { name: "Backlog", count: 1, color: "bg-zinc-400", pct: "10%" },
  ]

  const members = [
    { name: "Sarah Connor", role: "Product Designer", tasks: "4 tasks completed", avatar: "S", bg: "bg-purple-100 text-purple-700 dark:bg-purple-950 dark:text-purple-300" },
    { name: "David Miller", role: "Frontend Dev", tasks: "3 tasks completed", avatar: "D", bg: "bg-amber-100 text-amber-700 dark:bg-amber-950 dark:text-amber-300" },
    { name: "Alex Rover", role: "Backend Dev", tasks: "5 tasks completed", avatar: "A", bg: "bg-blue-100 text-blue-700 dark:bg-blue-950 dark:text-blue-300" },
  ]

  return (
    <div className="h-full w-full bg-zinc-50/50 p-5 overflow-y-auto space-y-5 text-xs dark:bg-zinc-950/20">
      
      {/* Dashboard Top Title */}
      <div className="flex items-center justify-between">
        <div>
          <h3 className="text-sm font-bold text-zinc-950 dark:text-white">Workspace Analytics</h3>
          <p className="text-[10px] text-zinc-500">Real-time team performance metrics</p>
        </div>
        <div className="flex items-center gap-1 text-[10px] bg-emerald-50 border border-emerald-100 text-emerald-800 rounded-full px-2 py-0.5 font-bold dark:bg-emerald-950/30 dark:border-emerald-900/60 dark:text-emerald-300">
          <ShieldCheck className="h-3 w-3" />
          <span>System Healthy</span>
        </div>
      </div>

      {/* Grid of Mini Stats Cards */}
      <div className="grid grid-cols-4 gap-3">
        <div className="rounded-xl border border-zinc-200/80 bg-white p-3 shadow-xs dark:border-zinc-800 dark:bg-zinc-900">
          <div className="flex items-center justify-between text-zinc-450 dark:text-zinc-550">
            <span className="font-semibold text-[10px]">TOTAL TASKS</span>
            <Zap className="h-3.5 w-3.5 text-blue-500" />
          </div>
          <div className="mt-1 flex items-baseline gap-1.5">
            <span className="text-lg font-extrabold text-zinc-950 dark:text-white">13</span>
            <span className="text-[9px] font-bold text-blue-600 dark:text-blue-400">+3 AI gen</span>
          </div>
        </div>

        <div className="rounded-xl border border-zinc-200/80 bg-white p-3 shadow-xs dark:border-zinc-800 dark:bg-zinc-900">
          <div className="flex items-center justify-between text-zinc-450 dark:text-zinc-550">
            <span className="font-semibold text-[10px]">COMPLETED</span>
            <CheckCircle2 className="h-3.5 w-3.5 text-emerald-500" />
          </div>
          <div className="mt-1 flex items-baseline gap-1.5">
            <span className="text-lg font-extrabold text-zinc-950 dark:text-white">82%</span>
            <span className="text-[9px] font-bold text-emerald-600 dark:text-emerald-400">+12%</span>
          </div>
        </div>

        <div className="rounded-xl border border-zinc-200/80 bg-white p-3 shadow-xs dark:border-zinc-800 dark:bg-zinc-900">
          <div className="flex items-center justify-between text-zinc-450 dark:text-zinc-550">
            <span className="font-semibold text-[10px]">VELOCITY</span>
            <TrendingUp className="h-3.5 w-3.5 text-indigo-500" />
          </div>
          <div className="mt-1 flex items-baseline gap-1.5">
            <span className="text-lg font-extrabold text-zinc-950 dark:text-white">4.8</span>
            <span className="text-[9px] font-bold text-zinc-500">tasks/day</span>
          </div>
        </div>

        <div className="rounded-xl border border-zinc-200/80 bg-white p-3 shadow-xs dark:border-zinc-800 dark:bg-zinc-900">
          <div className="flex items-center justify-between text-zinc-450 dark:text-zinc-550">
            <span className="font-semibold text-[10px]">ACTIVE TEAM</span>
            <Users className="h-3.5 w-3.5 text-amber-500" />
          </div>
          <div className="mt-1 flex items-baseline gap-1.5">
            <span className="text-lg font-extrabold text-zinc-950 dark:text-white">6</span>
            <span className="text-[9px] font-bold text-zinc-500">members</span>
          </div>
        </div>
      </div>

      {/* Metrics Row: Task breakdown & Team status */}
      <div className="grid grid-cols-5 gap-4">
        {/* Task status graph */}
        <div className="col-span-3 rounded-xl border border-zinc-200/80 bg-white p-4 shadow-xs dark:border-zinc-800 dark:bg-zinc-900">
          <h4 className="font-bold text-zinc-900 dark:text-white">Task Delivery Progress</h4>
          <div className="mt-4 space-y-3">
            {statusSummary.map((item) => (
              <div key={item.name} className="space-y-1">
                <div className="flex items-center justify-between text-[10px] font-medium">
                  <span className="text-zinc-700 dark:text-zinc-350">{item.name}</span>
                  <span className="font-bold text-zinc-900 dark:text-white">{item.count} tasks ({item.pct})</span>
                </div>
                {/* Custom CSS Bar */}
                <div className="h-2 w-full rounded-full bg-zinc-100 dark:bg-zinc-800 overflow-hidden">
                  <div
                    className={`${item.color} h-full rounded-full transition-all duration-1000 ease-out`}
                    style={{ width: item.pct }}
                  />
                </div>
              </div>
            ))}
          </div>
        </div>

        {/* Team list status */}
        <div className="col-span-2 rounded-xl border border-zinc-200/80 bg-white p-4 shadow-xs dark:border-zinc-800 dark:bg-zinc-900">
          <h4 className="font-bold text-zinc-900 dark:text-white">Top Performers</h4>
          <div className="mt-4.5 space-y-3">
            {members.map((m) => (
              <div key={m.name} className="flex items-center gap-2.5">
                <div className={`flex h-6 w-6 shrink-0 items-center justify-center rounded-full text-[10px] font-bold ${m.bg}`}>
                  {m.avatar}
                </div>
                <div className="min-w-0 flex-1">
                  <p className="truncate font-semibold text-zinc-950 dark:text-white leading-tight">{m.name}</p>
                  <p className="truncate text-[9px] text-zinc-450 dark:text-zinc-500">{m.role}</p>
                </div>
                <span className="text-[9px] font-bold text-zinc-650 dark:text-zinc-400 bg-zinc-50 border border-zinc-150 rounded px-1.5 py-0.5 shrink-0 dark:bg-zinc-950/30 dark:border-zinc-800">
                  {m.tasks}
                </span>
              </div>
            ))}
          </div>
        </div>
      </div>
    </div>
  )
}
