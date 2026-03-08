"use client"

import * as React from "react"
import { CheckSquare, Clock } from "lucide-react"

import { Card, CardContent, CardHeader, CardTitle } from "@/registry/new-york-v4/ui/card"
import { Badge } from "@/registry/new-york-v4/ui/badge"
import { Input } from "@/registry/new-york-v4/ui/input"
import { Checkbox } from "@/registry/new-york-v4/ui/checkbox"

interface Task {
  id: number
  title: string
  completed: boolean
  priority: "high" | "medium" | "low"
}

const initialTasks: Task[] = [
  { id: 1, title: "Hoàn thành báo cáo Q4", completed: true, priority: "high" },
  { id: 2, title: "Họp với team marketing", completed: true, priority: "medium" },
  { id: 3, title: "Review PR #234", completed: false, priority: "high" },
  { id: 4, title: "Cập nhật documentation", completed: false, priority: "low" },
  { id: 5, title: "Triển khai feature mới", completed: false, priority: "medium" },
]

function TaskItem({
  task,
  onToggle,
}: {
  task: Task
  onToggle: (id: number) => void
}) {
  const priorityLabels = {
    high: "Cao",
    medium: "TB",
    low: "Thấp",
  }

  const priorityVariants = {
    high: "destructive" as const,
    medium: "secondary" as const,
    low: "outline" as const,
  }

  return (
    <div
      className="flex items-center gap-4 p-4 hover:bg-muted/50 transition-colors cursor-pointer group"
      onClick={() => onToggle(task.id)}
    >
      <Checkbox
        checked={task.completed}
        className="transition-transform group-hover:scale-110"
      />
      <span
        className={`flex-1 transition-all ${
          task.completed
            ? "line-through text-muted-foreground"
            : "text-foreground"
        }`}
      >
        {task.title}
      </span>
      <Badge variant={priorityVariants[task.priority]} className="text-xs">
        {priorityLabels[task.priority]}
      </Badge>
    </div>
  )
}

function ProgressStats({
  completed,
  total,
}: {
  completed: number
  total: number
}) {
  const percentage = (completed / total) * 100

  return (
    <div className="flex items-center gap-4 p-4 rounded-xl bg-muted/50 border border-border/50">
      <div className="flex items-center justify-center w-12 h-12 rounded-full bg-emerald-500/20">
        <CheckSquare className="w-6 h-6 text-emerald-600" />
      </div>
      <div>
        <div className="text-2xl font-bold">
          {completed}/{total}
        </div>
        <div className="text-sm text-muted-foreground">Task hoàn thành</div>
      </div>
      <div className="ml-auto">
        <div className="w-24 h-2 rounded-full bg-muted overflow-hidden">
          <div
            className="h-full bg-gradient-to-r from-emerald-500 to-teal-500 transition-all duration-500"
            style={{ width: `${percentage}%` }}
          />
        </div>
      </div>
    </div>
  )
}

export function DemoSection() {
  const [tasks, setTasks] = React.useState(initialTasks)

  const toggleTask = (id: number) => {
    setTasks(
      tasks.map((task) =>
        task.id === id ? { ...task, completed: !task.completed } : task
      )
    )
  }

  const completedCount = tasks.filter((t) => t.completed).length
  const totalCount = tasks.length

  return (
    <section id="demo" className="py-24">
      <div className="container mx-auto px-4 sm:px-6 lg:px-8">
        <div className="grid lg:grid-cols-2 gap-12 items-center">
          {/* Left Column - Description */}
          <div>
            <Badge variant="outline" className="mb-4">
              Demo trực tiếp
            </Badge>
            <h2 className="text-3xl sm:text-4xl font-bold tracking-tight mb-4">
              Trải nghiệm ngay
            </h2>
            <p className="text-muted-foreground mb-8 leading-relaxed">
              Hãy thử click vào các task bên dưới để đánh dấu hoàn thành. Đây chỉ
              là một phần nhỏ trong những gì TodoApp có thể làm!
            </p>

            <div className="space-y-4">
              <ProgressStats completed={completedCount} total={totalCount} />
            </div>
          </div>

          {/* Right Column - Interactive Demo */}
          <Card className="border-border/50 bg-card/80 backdrop-blur-sm shadow-2xl shadow-violet-500/5">
            <CardHeader className="border-b border-border/50">
              <div className="flex items-center justify-between">
                <CardTitle className="flex items-center gap-2">
                  <Clock className="w-4 h-4 text-muted-foreground" />
                  Việc cần làm hôm nay
                </CardTitle>
                <Badge variant="secondary" className="font-mono">
                  {new Date().toLocaleDateString("vi-VN")}
                </Badge>
              </div>
            </CardHeader>
            <CardContent className="p-0">
              <div className="divide-y divide-border/50">
                {tasks.map((task) => (
                  <TaskItem key={task.id} task={task} onToggle={toggleTask} />
                ))}
              </div>
              <div className="p-4 border-t border-border/50">
                <Input
                  placeholder="Thêm task mới..."
                  className="bg-muted/30 border-border/50"
                />
              </div>
            </CardContent>
          </Card>
        </div>
      </div>
    </section>
  )
}
