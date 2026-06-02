"use client"

import { CheckCircle2, Circle, SquareKanban } from "lucide-react"
import { Badge } from "@/components/ui/badge"
import type { LinkedBoard, LinkedTask } from "@/features/docs/types"

interface LinkedContentProps {
  tasks: LinkedTask[]
  boards: LinkedBoard[]
}

export function LinkedContent({ tasks, boards }: LinkedContentProps) {
  if (!tasks.length && !boards.length) return null

  return (
    <section className="mb-7 grid gap-3 sm:grid-cols-2">
      {boards.map((board) => (
        <div key={board.id} className="rounded-xl border border-border bg-muted p-3">
          <div className="mb-2 flex items-center gap-2 text-sm font-semibold text-foreground">
            <SquareKanban className="size-4" style={{ color: board.color }} />
            {board.name}
          </div>
          <p className="text-xs text-muted-foreground">{board.openTasks} open tasks · {board.doneTasks} done</p>
        </div>
      ))}
      {tasks.map((task) => (
        <div key={task.id} className="rounded-xl border border-border bg-card p-3">
          <div className="mb-2 flex items-center gap-2 text-sm font-medium text-foreground">
            {task.status === "done" ? <CheckCircle2 className="size-4 text-[#1aae39]" /> : <Circle className="size-4 text-muted-foreground" />}
            {task.title}
          </div>
          <Badge variant="secondary" className="rounded-full text-[11px]">
            {task.status.replace("_", " ")}
          </Badge>
        </div>
      ))}
    </section>
  )
}
