"use client"

import * as React from "react"
import {
  Sheet,
  SheetContent,
  SheetDescription,
  SheetHeader,
  SheetTitle,
} from "@/registry/new-york-v4/ui/sheet"
import { Button } from "@/registry/new-york-v4/ui/button"
import { Input } from "@/registry/new-york-v4/ui/input"
import { Textarea } from "@/registry/new-york-v4/ui/textarea"
import { Badge } from "@/registry/new-york-v4/ui/badge"
import { Avatar, AvatarFallback, AvatarImage } from "@/registry/new-york-v4/ui/avatar"
import { Separator } from "@/registry/new-york-v4/ui/separator"
import { Progress } from "@/registry/new-york-v4/ui/progress"
import { Checkbox } from "@/registry/new-york-v4/ui/checkbox"
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/registry/new-york-v4/ui/select"
import {
  Calendar,
  Clock,
  Users,
  Tag,
  Flag,
  MessageSquare,
  Paperclip,
  Plus,
  X,
  CheckSquare,
  MoreHorizontal,
} from "lucide-react"
import { cn } from "@/lib/utils"

interface TaskDetailSheetProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  taskId?: string
}

// Sample task data
const sampleTask = {
  id: "1",
  title: "Employee Details Page",
  description: "Create a comprehensive page that displays all employee information including personal details, contact information, employment history, and performance metrics.",
  status: "in_progress",
  priority: "high",
  dueDate: "2024-02-14",
  estimatedHours: 16,
  labels: ["#dashboard", "#client"],
  assignees: [
    { id: "1", name: "Alex Johnson", avatar: "/avatars/1.jpg", email: "alex@todoapp.com" },
    { id: "2", name: "Sarah Wilson", avatar: "/avatars/2.jpg", email: "sarah@todoapp.com" },
  ],
  subtasks: [
    { id: "s1", title: "Design wireframes", completed: true },
    { id: "s2", title: "Create UI components", completed: true },
    { id: "s3", title: "Implement API integration", completed: false },
    { id: "s4", title: "Add unit tests", completed: false },
    { id: "s5", title: "Documentation", completed: false },
  ],
  comments: [
    {
      id: "c1",
      author: { name: "Alex Johnson", avatar: "/avatars/1.jpg" },
      content: "I've finished the initial wireframes. Please review and provide feedback.",
      timestamp: "2 hours ago",
    },
    {
      id: "c2",
      author: { name: "Sarah Wilson", avatar: "/avatars/2.jpg" },
      content: "Looks great! Just a few minor suggestions on the layout.",
      timestamp: "1 hour ago",
    },
  ],
  attachments: [
    { id: "a1", name: "wireframes.fig", size: "2.4 MB" },
    { id: "a2", name: "requirements.pdf", size: "156 KB" },
  ],
}

const statusOptions = [
  { value: "todo", label: "To Do", color: "bg-slate-500" },
  { value: "in_progress", label: "In Progress", color: "bg-amber-500" },
  { value: "review", label: "In Review", color: "bg-violet-500" },
  { value: "done", label: "Done", color: "bg-emerald-500" },
]

const priorityOptions = [
  { value: "low", label: "Low", color: "text-emerald-500" },
  { value: "medium", label: "Medium", color: "text-amber-500" },
  { value: "high", label: "High", color: "text-red-500" },
]

export function TaskDetailSheet({ open, onOpenChange, taskId }: TaskDetailSheetProps) {
  const task = sampleTask
  const completedSubtasks = task.subtasks.filter((s) => s.completed).length
  const progress = (completedSubtasks / task.subtasks.length) * 100

  return (
    <Sheet open={open} onOpenChange={onOpenChange}>
      <SheetContent className="w-full sm:max-w-xl overflow-y-auto">
        <SheetHeader className="space-y-1">
          <div className="flex items-center gap-2">
            <Badge variant="secondary" className="bg-amber-100 text-amber-700">
              In Progress
            </Badge>
            <Badge variant="secondary" className="bg-red-100 text-red-700">
              High Priority
            </Badge>
          </div>
          <SheetTitle className="text-xl">{task.title}</SheetTitle>
          <SheetDescription className="text-sm">
            Task ID: #{task.id}
          </SheetDescription>
        </SheetHeader>

        <div className="mt-6 space-y-6">
          {/* Description */}
          <div className="space-y-2">
            <label className="text-sm font-medium">Description</label>
            <Textarea
              defaultValue={task.description}
              className="min-h-[100px] resize-none"
              placeholder="Add description..."
            />
          </div>

          <Separator />

          {/* Properties Grid */}
          <div className="grid gap-4">
            {/* Status */}
            <div className="flex items-center gap-4">
              <div className="flex items-center gap-2 w-32 text-sm text-muted-foreground">
                <CheckSquare className="size-4" />
                Status
              </div>
              <Select defaultValue={task.status}>
                <SelectTrigger className="flex-1 h-9">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {statusOptions.map((option) => (
                    <SelectItem key={option.value} value={option.value}>
                      <div className="flex items-center gap-2">
                        <div className={cn("size-2 rounded-full", option.color)} />
                        {option.label}
                      </div>
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>

            {/* Priority */}
            <div className="flex items-center gap-4">
              <div className="flex items-center gap-2 w-32 text-sm text-muted-foreground">
                <Flag className="size-4" />
                Priority
              </div>
              <Select defaultValue={task.priority}>
                <SelectTrigger className="flex-1 h-9">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {priorityOptions.map((option) => (
                    <SelectItem key={option.value} value={option.value}>
                      <span className={option.color}>{option.label}</span>
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>

            {/* Assignees */}
            <div className="flex items-center gap-4">
              <div className="flex items-center gap-2 w-32 text-sm text-muted-foreground">
                <Users className="size-4" />
                Assignees
              </div>
              <div className="flex items-center gap-2 flex-1">
                <div className="flex -space-x-2">
                  {task.assignees.map((assignee) => (
                    <Avatar key={assignee.id} className="size-8 border-2 border-background">
                      <AvatarImage src={assignee.avatar} />
                      <AvatarFallback>{assignee.name.charAt(0)}</AvatarFallback>
                    </Avatar>
                  ))}
                </div>
                <Button variant="ghost" size="icon-sm" className="size-8 rounded-full">
                  <Plus className="size-4" />
                </Button>
              </div>
            </div>

            {/* Due Date */}
            <div className="flex items-center gap-4">
              <div className="flex items-center gap-2 w-32 text-sm text-muted-foreground">
                <Calendar className="size-4" />
                Due Date
              </div>
              <Input type="date" defaultValue={task.dueDate} className="flex-1 h-9" />
            </div>

            {/* Labels */}
            <div className="flex items-center gap-4">
              <div className="flex items-center gap-2 w-32 text-sm text-muted-foreground">
                <Tag className="size-4" />
                Labels
              </div>
              <div className="flex items-center gap-2 flex-1 flex-wrap">
                {task.labels.map((label) => (
                  <Badge key={label} variant="secondary" className="bg-violet-100 text-violet-700">
                    {label}
                    <X className="ml-1 size-3 cursor-pointer" />
                  </Badge>
                ))}
                <Button variant="ghost" size="sm" className="h-6 px-2 text-xs">
                  <Plus className="size-3 mr-1" />
                  Add
                </Button>
              </div>
            </div>
          </div>

          <Separator />

          {/* Subtasks */}
          <div className="space-y-3">
            <div className="flex items-center justify-between">
              <label className="text-sm font-medium">
                Subtasks ({completedSubtasks}/{task.subtasks.length})
              </label>
              <span className="text-sm text-muted-foreground">{Math.round(progress)}%</span>
            </div>
            <Progress value={progress} className="h-2" />
            <div className="space-y-2">
              {task.subtasks.map((subtask) => (
                <div key={subtask.id} className="flex items-center gap-3 p-2 rounded-lg hover:bg-muted/50">
                  <Checkbox checked={subtask.completed} />
                  <span className={cn("text-sm flex-1", subtask.completed && "line-through text-muted-foreground")}>
                    {subtask.title}
                  </span>
                  <Button variant="ghost" size="icon-sm" className="size-6 opacity-0 group-hover:opacity-100">
                    <MoreHorizontal className="size-4" />
                  </Button>
                </div>
              ))}
              <Button variant="ghost" className="w-full justify-start gap-2 text-muted-foreground">
                <Plus className="size-4" />
                Add subtask
              </Button>
            </div>
          </div>

          <Separator />

          {/* Attachments */}
          <div className="space-y-3">
            <div className="flex items-center justify-between">
              <label className="text-sm font-medium flex items-center gap-2">
                <Paperclip className="size-4" />
                Attachments ({task.attachments.length})
              </label>
              <Button variant="ghost" size="sm" className="h-7 px-2 text-xs">
                <Plus className="size-3 mr-1" />
                Add
              </Button>
            </div>
            <div className="space-y-2">
              {task.attachments.map((file) => (
                <div key={file.id} className="flex items-center gap-3 p-2 rounded-lg border bg-muted/30">
                  <div className="size-8 rounded bg-violet-100 flex items-center justify-center">
                    <Paperclip className="size-4 text-violet-600" />
                  </div>
                  <div className="flex-1">
                    <p className="text-sm font-medium">{file.name}</p>
                    <p className="text-xs text-muted-foreground">{file.size}</p>
                  </div>
                  <Button variant="ghost" size="icon-sm" className="size-7">
                    <X className="size-4" />
                  </Button>
                </div>
              ))}
            </div>
          </div>

          <Separator />

          {/* Comments */}
          <div className="space-y-3">
            <label className="text-sm font-medium flex items-center gap-2">
              <MessageSquare className="size-4" />
              Comments ({task.comments.length})
            </label>
            <div className="space-y-4">
              {task.comments.map((comment) => (
                <div key={comment.id} className="flex gap-3">
                  <Avatar className="size-8">
                    <AvatarImage src={comment.author.avatar} />
                    <AvatarFallback>{comment.author.name.charAt(0)}</AvatarFallback>
                  </Avatar>
                  <div className="flex-1 space-y-1">
                    <div className="flex items-center gap-2">
                      <span className="text-sm font-medium">{comment.author.name}</span>
                      <span className="text-xs text-muted-foreground">{comment.timestamp}</span>
                    </div>
                    <p className="text-sm text-muted-foreground">{comment.content}</p>
                  </div>
                </div>
              ))}
            </div>
            <div className="flex gap-3">
              <Avatar className="size-8">
                <AvatarFallback>AD</AvatarFallback>
              </Avatar>
              <div className="flex-1">
                <Textarea placeholder="Write a comment..." className="min-h-[80px] resize-none" />
                <div className="flex justify-end mt-2">
                  <Button size="sm" className="bg-gradient-to-r from-violet-500 to-purple-600">
                    Send
                  </Button>
                </div>
              </div>
            </div>
          </div>
        </div>
      </SheetContent>
    </Sheet>
  )
}
