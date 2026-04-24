"use client"

import * as React from "react"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Button } from "@/components/ui/button"
import { Badge } from "@/components/ui/badge"
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar"
import { Progress } from "@/components/ui/progress"
import { 
  Plus, 
  MoreHorizontal, 
  MessageSquare, 
  Paperclip,
  Calendar,
  CheckSquare,
} from "lucide-react"
import { cn } from "@/lib/utils"

// Sample data
const columns = [
  {
    id: "todo",
    title: "To-do",
    color: "bg-slate-500",
    tasks: [
      {
        id: "1",
        title: "Employee Details",
        description: "Create a page where there is information about employees",
        labels: ["#mobile app", "#client"],
        priority: "medium",
        dueDate: "Feb 14, 2024",
        assignees: ["/avatars/1.jpg", "/avatars/2.jpg"],
        comments: 12,
        attachments: 8,
        progress: 40,
        subtasks: { done: 2, total: 5 },
      },
      {
        id: "2",
        title: "Darkmode version",
        description: "Darkmode version for all screens",
        labels: ["#product", "#client"],
        priority: "low",
        dueDate: "Feb 14, 2024",
        assignees: ["/avatars/3.jpg"],
        comments: 6,
        attachments: 1,
      },
      {
        id: "3",
        title: "Super Admin Role",
        description: "",
        labels: ["#dashboard"],
        priority: "medium",
        dueDate: "Feb 1, 2024",
        assignees: ["/avatars/1.jpg", "/avatars/2.jpg", "/avatars/3.jpg"],
        comments: 0,
        attachments: 0,
      },
    ],
  },
  {
    id: "in_progress",
    title: "On Progress",
    color: "bg-amber-500",
    tasks: [
      {
        id: "4",
        title: "Weihu product task and the task process pages",
        description: "",
        labels: ["#mobile app", "#product"],
        priority: "high",
        dueDate: "Feb 14, 2024",
        assignees: ["/avatars/2.jpg"],
        comments: 6,
        attachments: 1,
        progress: 90,
        hasImage: true,
      },
      {
        id: "5",
        title: "Ginko mobile app design",
        description: "",
        labels: ["#mobile app", "#client"],
        priority: "medium",
        dueDate: "Feb 1, 2024",
        assignees: ["/avatars/1.jpg"],
        comments: 7,
        attachments: 2,
        progress: 45,
        subtasks: { done: 1, total: 3 },
      },
      {
        id: "6",
        title: "Design CRM shop product page responsive website",
        description: "",
        labels: ["#products", "#client"],
        priority: "low",
        dueDate: "Feb 14, 2024",
        assignees: ["/avatars/3.jpg", "/avatars/4.jpg"],
        comments: 2,
        attachments: 0,
        progress: 15,
      },
    ],
  },
  {
    id: "review",
    title: "In Review",
    color: "bg-violet-500",
    tasks: [
      {
        id: "7",
        title: "Orypto product landing page create in webflow",
        description: "",
        labels: ["#development", "#client"],
        priority: "medium",
        dueDate: "Feb 14, 2024",
        assignees: ["/avatars/1.jpg", "/avatars/2.jpg"],
        comments: 8,
        attachments: 2,
      },
      {
        id: "8",
        title: "Natverk video platform web app design and develop",
        description: "",
        labels: ["#product", "#client"],
        priority: "high",
        dueDate: "Feb 1, 2024",
        assignees: ["/avatars/3.jpg"],
        comments: 5,
        attachments: 1,
        subtasks: { done: 3, total: 4 },
      },
      {
        id: "9",
        title: "Redesign grab website landing and login pages",
        description: "",
        labels: ["#mobile app", "#rebuild"],
        priority: "low",
        dueDate: "Feb 14, 2024",
        assignees: ["/avatars/2.jpg", "/avatars/4.jpg"],
        comments: 3,
        attachments: 0,
      },
    ],
  },
  {
    id: "done",
    title: "Done",
    color: "bg-emerald-500",
    tasks: [
      {
        id: "10",
        title: "Affitto product full service",
        description: "Branding, Mobile app design & development, Dashboard design",
        labels: ["#mobile app", "#client"],
        priority: "high",
        dueDate: "Feb 14, 2024",
        assignees: ["/avatars/1.jpg"],
        comments: 6,
        attachments: 2,
      },
      {
        id: "11",
        title: "Design Moli app product page redesign",
        description: "",
        labels: ["#products", "#client"],
        priority: "medium",
        dueDate: "Feb 1, 2024",
        assignees: ["/avatars/2.jpg", "/avatars/3.jpg"],
        comments: 12,
        attachments: 1,
      },
    ],
  },
]

const priorityColors = {
  low: "bg-emerald-100 text-emerald-700 dark:bg-emerald-900/30 dark:text-emerald-400",
  medium: "bg-amber-100 text-amber-700 dark:bg-amber-900/30 dark:text-amber-400",
  high: "bg-red-100 text-red-700 dark:bg-red-900/30 dark:text-red-400",
}

const labelColors = [
  "bg-pink-100 text-pink-700 dark:bg-pink-900/30 dark:text-pink-400",
  "bg-violet-100 text-violet-700 dark:bg-violet-900/30 dark:text-violet-400",
  "bg-blue-100 text-blue-700 dark:bg-blue-900/30 dark:text-blue-400",
  "bg-emerald-100 text-emerald-700 dark:bg-emerald-900/30 dark:text-emerald-400",
]

interface Task {
  id: string
  title: string
  description?: string
  labels: string[]
  priority: string
  dueDate: string
  assignees: string[]
  comments: number
  attachments: number
  progress?: number
  subtasks?: { done: number; total: number }
  hasImage?: boolean
}

function TaskCard({ task }: { task: Task }) {
  return (
    <Card className="group cursor-pointer hover:shadow-md transition-all duration-200 hover:border-violet-200 dark:hover:border-violet-800">
      <CardContent className="p-4 space-y-3">
        {/* Labels */}
        <div className="flex flex-wrap gap-1.5">
          {task.labels.map((label, i) => (
            <Badge
              key={label}
              variant="secondary"
              className={cn("text-[10px] px-2 py-0", labelColors[i % labelColors.length])}
            >
              {label}
            </Badge>
          ))}
        </div>

        {/* Title */}
        <h3 className="font-medium leading-tight line-clamp-2">{task.title}</h3>

        {/* Description */}
        {task.description && (
          <p className="text-sm text-muted-foreground line-clamp-2">{task.description}</p>
        )}

        {/* Image placeholder */}
        {task.hasImage && (
          <div className="h-32 rounded-lg bg-gradient-to-br from-violet-100 to-purple-100 dark:from-violet-900/20 dark:to-purple-900/20" />
        )}

        {/* Progress */}
        {task.progress !== undefined && (
          <div className="space-y-1.5">
            <div className="flex justify-between text-xs">
              <span className="text-muted-foreground">Progress</span>
              <span className="font-medium">{task.progress}%</span>
            </div>
            <Progress value={task.progress} className="h-1.5" />
          </div>
        )}

        {/* Subtasks */}
        {task.subtasks && (
          <div className="flex items-center gap-1.5 text-xs text-muted-foreground">
            <CheckSquare className="size-3.5" />
            <span>{task.subtasks.done}/{task.subtasks.total} subtasks</span>
          </div>
        )}

        {/* Footer */}
        <div className="flex items-center justify-between pt-2 border-t">
          <div className="flex -space-x-1.5">
            {task.assignees.slice(0, 3).map((avatar, i) => (
              <Avatar key={i} className="size-6 border-2 border-background">
                <AvatarImage src={avatar} />
                <AvatarFallback className="text-[10px]">U</AvatarFallback>
              </Avatar>
            ))}
            {task.assignees.length > 3 && (
              <div className="flex items-center justify-center size-6 rounded-full bg-muted border-2 border-background text-[10px]">
                +{task.assignees.length - 3}
              </div>
            )}
          </div>
          <div className="flex items-center gap-3 text-xs text-muted-foreground">
            {task.comments > 0 && (
              <span className="flex items-center gap-1">
                <MessageSquare className="size-3.5" />
                {task.comments}
              </span>
            )}
            {task.attachments > 0 && (
              <span className="flex items-center gap-1">
                <Paperclip className="size-3.5" />
                {task.attachments}
              </span>
            )}
          </div>
        </div>

        {/* Due Date */}
        <div className="flex items-center gap-1.5 text-xs text-muted-foreground">
          <Calendar className="size-3.5" />
          <span>{task.dueDate}</span>
          <Badge variant="secondary" className={cn("ml-auto text-[10px]", priorityColors[task.priority as keyof typeof priorityColors])}>
            {task.priority}
          </Badge>
        </div>
      </CardContent>
    </Card>
  )
}

export function TaskKanbanView() {
  return (
    <div className="flex gap-4 p-4 overflow-x-auto min-h-full">
      {columns.map((column) => (
        <div key={column.id} className="flex flex-col w-80 shrink-0">
          {/* Column Header */}
          <div className="flex items-center justify-between mb-3 px-1">
            <div className="flex items-center gap-2">
              <div className={cn("size-3 rounded-full", column.color)} />
              <h3 className="font-semibold">{column.title}</h3>
              <Badge variant="secondary" className="text-xs">
                {column.tasks.length}
              </Badge>
            </div>
            <div className="flex items-center gap-1">
              <Button variant="ghost" size="icon-sm" className="size-7">
                <Plus className="size-4" />
              </Button>
              <Button variant="ghost" size="icon-sm" className="size-7">
                <MoreHorizontal className="size-4" />
              </Button>
            </div>
          </div>

          {/* Tasks */}
          <div className="flex flex-col gap-3 flex-1 min-h-0 overflow-y-auto pb-4">
            {column.tasks.map((task) => (
              <TaskCard key={task.id} task={task} />
            ))}

            {/* Add Task Button */}
            <Button
              variant="ghost"
              className="w-full justify-start gap-2 text-muted-foreground hover:text-foreground border-2 border-dashed border-muted hover:border-violet-300 dark:hover:border-violet-700"
            >
              <Plus className="size-4" />
              Add Task
            </Button>
          </div>
        </div>
      ))}
    </div>
  )
}
