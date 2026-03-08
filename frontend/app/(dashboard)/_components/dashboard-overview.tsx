"use client"

import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/registry/new-york-v4/ui/card"
import { Button } from "@/registry/new-york-v4/ui/button"
import { Badge } from "@/registry/new-york-v4/ui/badge"
import { Avatar, AvatarFallback, AvatarImage } from "@/registry/new-york-v4/ui/avatar"
import { Progress } from "@/registry/new-york-v4/ui/progress"
import {
  CheckCircle2,
  Clock,
  AlertCircle,
  TrendingUp,
  Calendar,
  Users,
  FolderKanban,
  ArrowRight,
  Plus,
} from "lucide-react"

// Sample stats
const stats = [
  {
    title: "Total Tasks",
    value: "128",
    change: "+12%",
    changeType: "positive" as const,
    icon: FolderKanban,
    color: "text-violet-500",
    bgColor: "bg-violet-500/10",
  },
  {
    title: "Completed",
    value: "86",
    change: "+8%",
    changeType: "positive" as const,
    icon: CheckCircle2,
    color: "text-emerald-500",
    bgColor: "bg-emerald-500/10",
  },
  {
    title: "In Progress",
    value: "24",
    change: "+4%",
    changeType: "positive" as const,
    icon: Clock,
    color: "text-amber-500",
    bgColor: "bg-amber-500/10",
  },
  {
    title: "Overdue",
    value: "6",
    change: "-2%",
    changeType: "negative" as const,
    icon: AlertCircle,
    color: "text-red-500",
    bgColor: "bg-red-500/10",
  },
]

// Sample recent tasks
const recentTasks = [
  {
    id: "1",
    title: "Design new landing page",
    status: "in_progress",
    priority: "high",
    dueDate: "Jan 20",
    assignee: { name: "John", avatar: "/avatars/1.jpg" },
    project: "Marketing",
  },
  {
    id: "2",
    title: "Fix authentication bug",
    status: "todo",
    priority: "urgent",
    dueDate: "Jan 18",
    assignee: { name: "Sarah", avatar: "/avatars/2.jpg" },
    project: "Development",
  },
  {
    id: "3",
    title: "Update user documentation",
    status: "in_progress",
    priority: "medium",
    dueDate: "Jan 22",
    assignee: { name: "Mike", avatar: "/avatars/3.jpg" },
    project: "Documentation",
  },
  {
    id: "4",
    title: "Review PR #234",
    status: "done",
    priority: "low",
    dueDate: "Jan 17",
    assignee: { name: "Emma", avatar: "/avatars/4.jpg" },
    project: "Development",
  },
]

// Sample upcoming deadlines
const upcomingDeadlines = [
  { id: "1", title: "Sprint Review", date: "Today, 3:00 PM", type: "meeting" },
  { id: "2", title: "Deploy v2.0", date: "Tomorrow", type: "milestone" },
  { id: "3", title: "Client presentation", date: "Jan 20", type: "meeting" },
]

const priorityColors = {
  low: "bg-slate-100 text-slate-700 dark:bg-slate-800 dark:text-slate-300",
  medium: "bg-amber-100 text-amber-700 dark:bg-amber-900/30 dark:text-amber-400",
  high: "bg-orange-100 text-orange-700 dark:bg-orange-900/30 dark:text-orange-400",
  urgent: "bg-red-100 text-red-700 dark:bg-red-900/30 dark:text-red-400",
}

const statusColors = {
  todo: "bg-slate-100 text-slate-700 dark:bg-slate-800 dark:text-slate-300",
  in_progress: "bg-blue-100 text-blue-700 dark:bg-blue-900/30 dark:text-blue-400",
  done: "bg-emerald-100 text-emerald-700 dark:bg-emerald-900/30 dark:text-emerald-400",
}

export function DashboardOverview() {
  return (
    <div className="p-6 space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold tracking-tight">Dashboard</h1>
          <p className="text-muted-foreground">
            Welcome back! Here's your task overview.
          </p>
        </div>
        <Button className="gap-2 bg-gradient-to-r from-violet-500 to-purple-600 hover:from-violet-600 hover:to-purple-700">
          <Plus className="size-4" />
          Create Task
        </Button>
      </div>

      {/* Stats Grid */}
      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
        {stats.map((stat) => (
          <Card key={stat.title}>
            <CardContent className="p-6">
              <div className="flex items-center justify-between">
                <div className={`p-2 rounded-lg ${stat.bgColor}`}>
                  <stat.icon className={`size-5 ${stat.color}`} />
                </div>
                <Badge
                  variant="secondary"
                  className={
                    stat.changeType === "positive"
                      ? "bg-emerald-100 text-emerald-700 dark:bg-emerald-900/30 dark:text-emerald-400"
                      : "bg-red-100 text-red-700 dark:bg-red-900/30 dark:text-red-400"
                  }
                >
                  <TrendingUp className={`size-3 mr-1 ${stat.changeType === "negative" ? "rotate-180" : ""}`} />
                  {stat.change}
                </Badge>
              </div>
              <div className="mt-4">
                <p className="text-3xl font-bold">{stat.value}</p>
                <p className="text-sm text-muted-foreground">{stat.title}</p>
              </div>
            </CardContent>
          </Card>
        ))}
      </div>

      <div className="grid gap-6 lg:grid-cols-3">
        {/* Recent Tasks */}
        <Card className="lg:col-span-2">
          <CardHeader className="flex flex-row items-center justify-between">
            <div>
              <CardTitle>Recent Tasks</CardTitle>
              <CardDescription>Your latest task activities</CardDescription>
            </div>
            <Button variant="ghost" size="sm" className="gap-1">
              View All <ArrowRight className="size-4" />
            </Button>
          </CardHeader>
          <CardContent>
            <div className="space-y-4">
              {recentTasks.map((task) => (
                <div
                  key={task.id}
                  className="flex items-center gap-4 p-3 rounded-lg hover:bg-muted/50 transition-colors cursor-pointer"
                >
                  <Avatar className="size-9">
                    <AvatarImage src={task.assignee.avatar} />
                    <AvatarFallback className="bg-gradient-to-br from-violet-500 to-purple-600 text-white text-xs">
                      {task.assignee.name.charAt(0)}
                    </AvatarFallback>
                  </Avatar>
                  <div className="flex-1 min-w-0">
                    <p className="font-medium truncate">{task.title}</p>
                    <p className="text-sm text-muted-foreground">{task.project}</p>
                  </div>
                  <Badge variant="secondary" className={priorityColors[task.priority as keyof typeof priorityColors]}>
                    {task.priority}
                  </Badge>
                  <Badge variant="secondary" className={statusColors[task.status as keyof typeof statusColors]}>
                    {task.status.replace("_", " ")}
                  </Badge>
                  <span className="text-sm text-muted-foreground whitespace-nowrap">
                    {task.dueDate}
                  </span>
                </div>
              ))}
            </div>
          </CardContent>
        </Card>

        {/* Sidebar Cards */}
        <div className="space-y-6">
          {/* Progress Card */}
          <Card>
            <CardHeader>
              <CardTitle className="text-base">Weekly Progress</CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="space-y-2">
                <div className="flex justify-between text-sm">
                  <span>Tasks Completed</span>
                  <span className="font-medium">67%</span>
                </div>
                <Progress value={67} className="h-2" />
              </div>
              <div className="space-y-2">
                <div className="flex justify-between text-sm">
                  <span>Sprint Progress</span>
                  <span className="font-medium">45%</span>
                </div>
                <Progress value={45} className="h-2" />
              </div>
              <div className="space-y-2">
                <div className="flex justify-between text-sm">
                  <span>Team Velocity</span>
                  <span className="font-medium">82%</span>
                </div>
                <Progress value={82} className="h-2" />
              </div>
            </CardContent>
          </Card>

          {/* Upcoming Deadlines */}
          <Card>
            <CardHeader>
              <CardTitle className="text-base flex items-center gap-2">
                <Calendar className="size-4" />
                Upcoming
              </CardTitle>
            </CardHeader>
            <CardContent>
              <div className="space-y-3">
                {upcomingDeadlines.map((item) => (
                  <div
                    key={item.id}
                    className="flex items-center gap-3 p-2 rounded-lg hover:bg-muted/50 transition-colors cursor-pointer"
                  >
                    <div className={`size-2 rounded-full ${
                      item.type === "meeting" ? "bg-violet-500" : "bg-emerald-500"
                    }`} />
                    <div className="flex-1">
                      <p className="text-sm font-medium">{item.title}</p>
                      <p className="text-xs text-muted-foreground">{item.date}</p>
                    </div>
                  </div>
                ))}
              </div>
            </CardContent>
          </Card>

          {/* Team Card */}
          <Card>
            <CardHeader>
              <CardTitle className="text-base flex items-center gap-2">
                <Users className="size-4" />
                Team
              </CardTitle>
            </CardHeader>
            <CardContent>
              <div className="flex -space-x-2">
                {[1, 2, 3, 4, 5].map((i) => (
                  <Avatar key={i} className="size-8 border-2 border-background">
                    <AvatarImage src={`/avatars/${i}.jpg`} />
                    <AvatarFallback className="bg-gradient-to-br from-violet-500 to-purple-600 text-white text-xs">
                      U{i}
                    </AvatarFallback>
                  </Avatar>
                ))}
                <div className="flex items-center justify-center size-8 rounded-full bg-muted border-2 border-background text-xs font-medium">
                  +8
                </div>
              </div>
              <Button variant="outline" size="sm" className="w-full mt-4 gap-1">
                <Plus className="size-4" />
                Invite Member
              </Button>
            </CardContent>
          </Card>
        </div>
      </div>
    </div>
  )
}
