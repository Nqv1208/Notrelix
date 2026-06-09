"use client"

import { Calendar, CheckSquare, MessageSquare, AlertCircle } from "lucide-react"
import { Task } from "@/hooks/use-demo-timeline"
import { cn } from "@/lib/utils"

interface TaskCardMockProps {
  task: Task
  isDragging?: boolean
  onClick?: () => void
}

export function TaskCardMock({ task, isDragging = false, onClick }: TaskCardMockProps) {
  const getPriorityColor = (priority?: string) => {
    switch (priority) {
      case "urgent":
        return "bg-rose-50 text-rose-800 border-rose-250 dark:bg-rose-950/40 dark:text-rose-200 dark:border-rose-900"
      case "high":
        return "bg-amber-50 text-amber-900 border-amber-250 dark:bg-amber-950/40 dark:text-amber-200 dark:border-amber-900"
      case "medium":
        return "bg-blue-50 text-blue-800 border-blue-250 dark:bg-blue-950/40 dark:text-blue-200 dark:border-blue-900"
      default:
        return "bg-zinc-100 text-zinc-700 border-zinc-200 dark:bg-zinc-800/80 dark:text-zinc-350 dark:border-zinc-700"
    }
  }

  return (
    <div
      onClick={onClick}
      className={cn(
        "group select-none rounded-xl border border-zinc-200 bg-white p-3.5 shadow-xs transition-all duration-200 hover:border-zinc-350 hover:shadow-md cursor-pointer dark:border-zinc-800 dark:bg-zinc-900 dark:hover:border-zinc-700",
        isDragging && "scale-[1.02] -rotate-1 border-blue-500 shadow-xl dark:border-blue-400 opacity-90 ring-1 ring-blue-500/20"
      )}
    >
      {/* Title */}
      <h4 className="text-xs font-semibold text-zinc-900 leading-normal group-hover:text-blue-600 transition-colors duration-150 dark:text-zinc-100 dark:group-hover:text-blue-400">
        {task.title}
      </h4>

      {/* Details Row (Priority & Metadata) */}
      <div className="mt-3.5 flex items-center justify-between gap-2">
        {/* Left indicators: Priority Badge, Date */}
        <div className="flex flex-wrap items-center gap-1.5">
          {task.priority && (
            <span
              className={cn(
                "rounded px-1.5 py-0.5 text-[9px] font-bold tracking-wide uppercase border",
                getPriorityColor(task.priority)
              )}
            >
              {task.priority}
            </span>
          )}

          {task.dueDate && (
            <span className="flex items-center gap-1 text-[10px] text-zinc-500 font-medium dark:text-zinc-400">
              <Calendar className="h-3 w-3 shrink-0" />
              <span>{task.dueDate}</span>
            </span>
          )}
        </div>

        {/* Right Info: Assignee Avatar */}
        {task.assignee ? (
          <div
            title={task.assignee.name}
            className="flex h-5 w-5 shrink-0 items-center justify-center rounded-full bg-zinc-250 text-[10px] font-bold border border-white text-zinc-800 ring-2 ring-zinc-50 dark:bg-zinc-800 dark:text-zinc-200 dark:border-zinc-900 dark:ring-zinc-950"
          >
            {task.assignee.avatar}
          </div>
        ) : (
          <div className="h-5 w-5 rounded-full border border-dashed border-zinc-300 bg-zinc-50/50 flex items-center justify-center dark:border-zinc-800 dark:bg-zinc-900">
            <span className="text-[9px] text-zinc-405 font-bold">+</span>
          </div>
        )}
      </div>
    </div>
  )
}
