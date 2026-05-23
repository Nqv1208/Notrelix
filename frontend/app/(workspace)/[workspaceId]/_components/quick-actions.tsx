"use client"

import { FileText, Kanban, ListTodo, Calendar } from "lucide-react"

const actions = [
  {
    label: "New Page",
    icon: FileText,
    bg: "bg-blue-50 hover:bg-blue-100/80 dark:bg-blue-950/30 dark:hover:bg-blue-950/50",
    iconColor: "text-blue-600 dark:text-blue-400",
    border: "border-blue-200/60 dark:border-blue-800/40",
  },
  {
    label: "New Board",
    icon: Kanban,
    bg: "bg-violet-50 hover:bg-violet-100/80 dark:bg-violet-950/30 dark:hover:bg-violet-950/50",
    iconColor: "text-violet-600 dark:text-violet-400",
    border: "border-violet-200/60 dark:border-violet-800/40",
  },
  {
    label: "Add Task",
    icon: ListTodo,
    bg: "bg-emerald-50 hover:bg-emerald-100/80 dark:bg-emerald-950/30 dark:hover:bg-emerald-950/50",
    iconColor: "text-emerald-600 dark:text-emerald-400",
    border: "border-emerald-200/60 dark:border-emerald-800/40",
  },
  {
    label: "Calendar",
    icon: Calendar,
    bg: "bg-sky-50 hover:bg-sky-100/80 dark:bg-sky-950/30 dark:hover:bg-sky-950/50",
    iconColor: "text-sky-600 dark:text-sky-400",
    border: "border-sky-200/60 dark:border-sky-800/40",
  },
]

export function QuickActions() {
  return (
    <section>
      <div className="grid grid-cols-2 sm:grid-cols-4 gap-2.5">
        {actions.map((action) => (
          <button
            key={action.label}
            className={`group flex items-center gap-2.5 rounded-xl border ${action.border} ${action.bg} px-4 py-3 transition-all duration-150 hover:-translate-y-0.5 hover:shadow-sm cursor-pointer active:scale-[0.98]`}
          >
            <div className="flex items-center justify-center size-8 rounded-lg bg-card/60 dark:bg-card/5 shadow-xs">
              <action.icon className={`size-4 ${action.iconColor} stroke-[1.75]`} />
            </div>
            <span className="text-sm font-medium text-foreground" style={{ fontFamily: "var(--font-poppins)" }}>
              {action.label}
            </span>
          </button>
        ))}
      </div>
    </section>
  )
}
