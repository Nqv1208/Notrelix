"use client"

import * as React from "react"
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { 
  LayoutGrid,
  List,
  Calendar,
  Filter,
  Search,
  Plus,
  MoreHorizontal,
  Star,
  Share2,
} from "lucide-react"
import { TaskListView } from "./task-list-view"
import { TaskKanbanView } from "./task-kanban-view"
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar"

interface TaskPageProps {
  workspaceId: string
  pageId: string
}

export function TaskPage({ workspaceId, pageId }: TaskPageProps) {
  const [view, setView] = React.useState<"list" | "kanban" | "calendar">("kanban")

  // Sample page data - will be replaced with API
  const pageData = {
    id: pageId,
    title: "Sprint Tasks",
    icon: "🏃",
    workspace: "Notrelix Project",
  }

  return (
    <div className="flex flex-col h-full">
      {/* Page Header */}
      <div className="border-b bg-background/95 backdrop-blur supports-[backdrop-filter]:bg-background/60">
        <div className="flex items-center justify-between p-4">
          <div className="flex items-center gap-3">
            <span className="text-3xl">{pageData.icon}</span>
            <div>
              <h1 className="text-xl font-bold">{pageData.title}</h1>
              <p className="text-sm text-muted-foreground">{pageData.workspace}</p>
            </div>
          </div>
          <div className="flex items-center gap-2">
            {/* Team Avatars */}
            <div className="flex -space-x-2 mr-2">
              {[1, 2, 3].map((i) => (
                <Avatar key={i} className="size-7 border-2 border-background">
                  <AvatarImage src={`/avatars/${i}.jpg`} />
                  <AvatarFallback className="text-xs">U{i}</AvatarFallback>
                </Avatar>
              ))}
            </div>
            <Button variant="ghost" size="icon-sm">
              <Star className="size-4" />
            </Button>
            <Button variant="ghost" size="icon-sm">
              <Share2 className="size-4" />
            </Button>
            <Button variant="ghost" size="icon-sm">
              <MoreHorizontal className="size-4" />
            </Button>
          </div>
        </div>

        {/* View Tabs & Actions */}
        <div className="flex items-center justify-between px-4 pb-3">
          <Tabs value={view} onValueChange={(v) => setView(v as typeof view)}>
            <TabsList className="h-9">
              <TabsTrigger value="kanban" className="gap-1.5">
                <LayoutGrid className="size-4" />
                Kanban
              </TabsTrigger>
              <TabsTrigger value="list" className="gap-1.5">
                <List className="size-4" />
                List
              </TabsTrigger>
              <TabsTrigger value="calendar" className="gap-1.5">
                <Calendar className="size-4" />
                Timeline
              </TabsTrigger>
            </TabsList>
          </Tabs>

          <div className="flex items-center gap-2">
            <div className="relative">
              <Search className="absolute left-2.5 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" />
              <Input
                type="search"
                placeholder="Search..."
                className="w-48 pl-8 h-9"
              />
            </div>
            <Button variant="outline" size="sm" className="gap-1.5">
              <Filter className="size-4" />
              Filter
            </Button>
            <Button size="sm" className="gap-1.5 bg-gradient-to-r from-violet-500 to-purple-600 hover:from-violet-600 hover:to-purple-700">
              <Plus className="size-4" />
              New Task
            </Button>
          </div>
        </div>
      </div>

      {/* Content */}
      <div className="flex-1 overflow-auto">
        {view === "kanban" && <TaskKanbanView />}
        {view === "list" && <TaskListView />}
        {view === "calendar" && (
          <div className="flex items-center justify-center h-full text-muted-foreground">
            Timeline view coming soon...
          </div>
        )}
      </div>
    </div>
  )
}
