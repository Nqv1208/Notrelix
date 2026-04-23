"use client"

import * as React from "react"
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table"
import { Checkbox } from "@/components/ui/checkbox"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar"
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"
import {
  Collapsible,
  CollapsibleContent,
  CollapsibleTrigger,
} from "@/components/ui/collapsible"
import { 
  MoreHorizontal, 
  ChevronDown,
  Plus,
  GripVertical,
  Edit,
  Trash2,
  Copy,
  Star,
} from "lucide-react"
import { cn } from "@/lib/utils"

// Sample data grouped by status
const taskGroups = [
  {
    id: "todo",
    title: "To-do",
    color: "bg-slate-500",
    count: 3,
    tasks: [
      {
        id: "1",
        title: "Employee Details",
        description: "Create a page where there is information about employees",
        status: "todo",
        priority: "medium",
        type: "Dashboard",
        typeColor: "bg-violet-100 text-violet-700",
        estimation: "Feb 14, 2024 - Feb 1, 2024",
        assignees: [
          { name: "AL", avatar: "/avatars/1.jpg" },
          { name: "DT", avatar: "/avatars/2.jpg" },
        ],
      },
      {
        id: "2",
        title: "Darkmode version",
        description: "Darkmode version for all screens",
        status: "todo",
        priority: "low",
        type: "Mobile App",
        typeColor: "bg-emerald-100 text-emerald-700",
        estimation: "Feb 14, 2024 - Feb 1, 2024",
        assignees: [
          { name: "AL", avatar: "/avatars/1.jpg" },
          { name: "DT", avatar: "/avatars/2.jpg" },
        ],
      },
      {
        id: "3",
        title: "Super Admin Role",
        description: "-",
        status: "todo",
        priority: "medium",
        type: "Dashboard",
        typeColor: "bg-violet-100 text-violet-700",
        estimation: "Feb 14, 2024 - Feb 1, 2024",
        assignees: [
          { name: "AL", avatar: "/avatars/1.jpg" },
          { name: "DT", avatar: "/avatars/2.jpg" },
          { name: "MK", avatar: "/avatars/3.jpg" },
        ],
      },
    ],
  },
  {
    id: "in_progress",
    title: "On Progress",
    color: "bg-amber-500",
    count: 3,
    tasks: [
      {
        id: "4",
        title: "Super Admin Role",
        description: "-",
        status: "in_progress",
        priority: "high",
        type: "Dashboard",
        typeColor: "bg-violet-100 text-violet-700",
        estimation: "Feb 14, 2024 - Feb 1, 2024",
        assignees: [{ name: "DT", avatar: "/avatars/2.jpg" }],
      },
      {
        id: "5",
        title: "Settings Page",
        description: "-",
        status: "in_progress",
        priority: "medium",
        type: "Mobile App",
        typeColor: "bg-emerald-100 text-emerald-700",
        estimation: "Feb 14, 2024 - Feb 1, 2024",
        assignees: [
          { name: "DT", avatar: "/avatars/2.jpg" },
          { name: "MK", avatar: "/avatars/3.jpg" },
        ],
      },
      {
        id: "6",
        title: "KPI and Employee Statistics",
        description: "Create a design that displays KPIs and employee statistics",
        status: "in_progress",
        priority: "low",
        type: "Dashboard",
        typeColor: "bg-violet-100 text-violet-700",
        estimation: "Feb 14, 2024 - Feb 1, 2024",
        assignees: [{ name: "DT", avatar: "/avatars/2.jpg" }],
      },
    ],
  },
  {
    id: "review",
    title: "In Review",
    color: "bg-violet-500",
    count: 2,
    tasks: [
      {
        id: "7",
        title: "Customer Role",
        description: "-",
        status: "review",
        priority: "medium",
        type: "Dashboard",
        typeColor: "bg-violet-100 text-violet-700",
        estimation: "Feb 14, 2024 - Feb 1, 2024",
        assignees: [
          { name: "AL", avatar: "/avatars/1.jpg" },
          { name: "DT", avatar: "/avatars/2.jpg" },
        ],
      },
      {
        id: "8",
        title: "Admin Role",
        description: "Set up with relevant information such as profile picture, phone number etc",
        status: "review",
        priority: "high",
        type: "Mobile App",
        typeColor: "bg-emerald-100 text-emerald-700",
        estimation: "Feb 14, 2024 - Feb 1, 2024",
        assignees: [
          { name: "AL", avatar: "/avatars/1.jpg" },
          { name: "DT", avatar: "/avatars/2.jpg" },
          { name: "MK", avatar: "/avatars/3.jpg" },
        ],
      },
    ],
  },
]

const priorityColors = {
  low: "bg-emerald-100 text-emerald-700 dark:bg-emerald-900/30 dark:text-emerald-400",
  medium: "bg-amber-100 text-amber-700 dark:bg-amber-900/30 dark:text-amber-400",
  high: "bg-red-100 text-red-700 dark:bg-red-900/30 dark:text-red-400",
}

export function TaskListView() {
  const [selectedTasks, setSelectedTasks] = React.useState<string[]>([])

  const toggleTask = (taskId: string) => {
    setSelectedTasks((prev) =>
      prev.includes(taskId)
        ? prev.filter((id) => id !== taskId)
        : [...prev, taskId]
    )
  }

  return (
    <div className="p-4 space-y-4">
      {taskGroups.map((group) => (
        <Collapsible key={group.id} defaultOpen>
          <div className="border rounded-lg overflow-hidden">
            {/* Group Header */}
            <CollapsibleTrigger asChild>
              <div className="flex items-center gap-3 p-3 bg-muted/30 hover:bg-muted/50 cursor-pointer transition-colors">
                <ChevronDown className="size-4 transition-transform duration-200 [[data-state=closed]_&]:-rotate-90" />
                <div className={cn("size-3 rounded-full", group.color)} />
                <span className="font-semibold">{group.title}</span>
                <Badge variant="secondary" className="text-xs">
                  {group.count}
                </Badge>
                <Button variant="ghost" size="icon-sm" className="ml-auto size-7">
                  <Plus className="size-4" />
                </Button>
              </div>
            </CollapsibleTrigger>

            <CollapsibleContent>
              <Table>
                <TableHeader>
                  <TableRow className="hover:bg-transparent">
                    <TableHead className="w-12">
                      <Checkbox />
                    </TableHead>
                    <TableHead className="w-8"></TableHead>
                    <TableHead>Task Name</TableHead>
                    <TableHead>Description</TableHead>
                    <TableHead>Estimation</TableHead>
                    <TableHead>Type</TableHead>
                    <TableHead>People</TableHead>
                    <TableHead>Priority</TableHead>
                    <TableHead className="w-10"></TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {group.tasks.map((task) => (
                    <TableRow key={task.id} className="group">
                      <TableCell>
                        <Checkbox
                          checked={selectedTasks.includes(task.id)}
                          onCheckedChange={() => toggleTask(task.id)}
                        />
                      </TableCell>
                      <TableCell>
                        <GripVertical className="size-4 text-muted-foreground opacity-0 group-hover:opacity-100 cursor-grab" />
                      </TableCell>
                      <TableCell className="font-medium">{task.title}</TableCell>
                      <TableCell className="text-muted-foreground max-w-[200px] truncate">
                        {task.description}
                      </TableCell>
                      <TableCell className="text-sm text-muted-foreground whitespace-nowrap">
                        {task.estimation}
                      </TableCell>
                      <TableCell>
                        <Badge variant="secondary" className={cn("text-xs", task.typeColor)}>
                          {task.type}
                        </Badge>
                      </TableCell>
                      <TableCell>
                        <div className="flex -space-x-1.5">
                          {task.assignees.slice(0, 3).map((assignee, i) => (
                            <Avatar key={i} className="size-7 border-2 border-background">
                              <AvatarImage src={assignee.avatar} />
                              <AvatarFallback className="text-[10px]">
                                {assignee.name}
                              </AvatarFallback>
                            </Avatar>
                          ))}
                          {task.assignees.length > 3 && (
                            <div className="flex items-center justify-center size-7 rounded-full bg-muted border-2 border-background text-[10px]">
                              +{task.assignees.length - 3}
                            </div>
                          )}
                        </div>
                      </TableCell>
                      <TableCell>
                        <Badge
                          variant="secondary"
                          className={cn(
                            "text-xs",
                            priorityColors[task.priority as keyof typeof priorityColors]
                          )}
                        >
                          {task.priority}
                        </Badge>
                      </TableCell>
                      <TableCell>
                        <DropdownMenu>
                          <DropdownMenuTrigger asChild>
                            <Button
                              variant="ghost"
                              size="icon-sm"
                              className="size-7 opacity-0 group-hover:opacity-100"
                            >
                              <MoreHorizontal className="size-4" />
                            </Button>
                          </DropdownMenuTrigger>
                          <DropdownMenuContent align="end">
                            <DropdownMenuItem>
                              <Edit className="mr-2 size-4" />
                              Edit
                            </DropdownMenuItem>
                            <DropdownMenuItem>
                              <Copy className="mr-2 size-4" />
                              Duplicate
                            </DropdownMenuItem>
                            <DropdownMenuItem>
                              <Star className="mr-2 size-4" />
                              Add to Favorites
                            </DropdownMenuItem>
                            <DropdownMenuSeparator />
                            <DropdownMenuItem className="text-destructive">
                              <Trash2 className="mr-2 size-4" />
                              Delete
                            </DropdownMenuItem>
                          </DropdownMenuContent>
                        </DropdownMenu>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </CollapsibleContent>
          </div>
        </Collapsible>
      ))}
    </div>
  )
}
