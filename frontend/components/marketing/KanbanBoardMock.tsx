"use client"

import { Sparkles } from "lucide-react"
import { Task } from "@/hooks/use-demo-timeline"
import { TaskCardMock } from "./TaskCardMock"
import { StreamingText } from "./StreamingText"

interface KanbanBoardMockProps {
  tasks: Task[]
  aiCommandText: string
  activeScene: number
  onCardClick?: (taskId: string) => void
}

export function KanbanBoardMock({
  tasks,
  aiCommandText,
  activeScene,
  onCardClick,
}: KanbanBoardMockProps) {
  const columns = [
    { id: "backlog", name: "Backlog", color: "bg-zinc-200 text-zinc-800 dark:bg-zinc-800 dark:text-zinc-250" },
    { id: "in-progress", name: "In Progress", color: "bg-blue-100 text-blue-700 dark:bg-blue-950/60 dark:text-blue-300" },
    { id: "review", name: "Review", color: "bg-amber-100 text-amber-700 dark:bg-amber-950/50 dark:text-amber-300" },
    { id: "done", name: "Done", color: "bg-emerald-100 text-emerald-700 dark:bg-emerald-950/50 dark:text-emerald-300" },
  ] as const

  const getTasksByColumn = (colId: "backlog" | "in-progress" | "review" | "done") => {
    return tasks.filter((t) => t.column === colId)
  }

  return (
    <div className="flex h-full w-full flex-col bg-white dark:bg-zinc-950">
      
      {/* Dynamic AI Generation Prompt Bar */}
      <div className="flex items-center gap-3 border-b border-zinc-200/80 bg-zinc-50/50 px-4 py-3 dark:border-zinc-850 dark:bg-zinc-900/30">
        <div className="flex h-7 w-7 shrink-0 items-center justify-center rounded-lg bg-blue-600 shadow-md text-white">
          <Sparkles className="h-4 w-4" />
        </div>
        <div className="flex-1 rounded-lg border border-zinc-200 bg-white px-3 py-1 flex items-center shadow-xs dark:border-zinc-800 dark:bg-zinc-900">
          {aiCommandText ? (
            <StreamingText text={aiCommandText} showCursor={activeScene === 2} className="text-xs text-zinc-800 dark:text-zinc-200" />
          ) : (
            <span className="text-xs text-zinc-400 italic font-medium select-none">Ask AI to build a list, generate tasks, or plan...</span>
          )}
        </div>
        
        {/* Dynamic button that changes color based on action */}
        <button
          className={`rounded-lg px-3 py-1 text-xs font-bold shadow-xs transition-colors duration-200 ${
            activeScene >= 3
              ? "bg-emerald-600 hover:bg-emerald-500 text-white"
              : "bg-blue-600 hover:bg-blue-500 text-white"
          }`}
        >
          {activeScene >= 3 ? "Generated" : "Generate"}
        </button>
      </div>

      {/* Board Columns Grid */}
      <div className="flex-1 overflow-x-auto p-4.5">
        <div className="flex gap-4 h-full min-w-[640px]">
          {columns.map((col) => {
            const colTasks = getTasksByColumn(col.id)
            return (
              <div key={col.id} className="flex-1 flex flex-col rounded-xl bg-zinc-50/60 p-2.5 dark:bg-zinc-900/20 border border-zinc-150/50 dark:border-zinc-850/50">
                
                {/* Column Header */}
                <div className="mb-3 flex items-center justify-between px-1">
                  <div className="flex items-center gap-2">
                    <span className="text-[11px] font-bold text-zinc-900 dark:text-white">{col.name}</span>
                    <span className={`rounded-full px-2 py-0.5 text-[9px] font-bold ${col.color}`}>
                      {colTasks.length}
                    </span>
                  </div>
                </div>

                {/* Column Tasks List */}
                <div className="flex-1 space-y-2.5 overflow-y-auto min-h-0">
                  {colTasks.map((task) => (
                    <TaskCardMock
                      key={task.id}
                      task={task}
                      // visual dragging state when cursor pulls task-4 in Scene 4
                      isDragging={activeScene === 4 && task.id === "task-4"}
                      onClick={() => onCardClick?.(task.id)}
                    />
                  ))}
                  {colTasks.length === 0 && (
                    <div className="flex h-16 items-center justify-center rounded-xl border border-dashed border-zinc-200 bg-white/40 text-[10px] text-zinc-400 dark:border-zinc-800 dark:bg-zinc-900/10">
                      Empty column
                    </div>
                  )}
                </div>
              </div>
            )
          })}
        </div>
      </div>
    </div>
  )
}
