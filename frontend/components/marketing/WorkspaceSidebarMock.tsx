"use client"

import { Folder, Inbox, Calendar, LayoutDashboard, ChevronDown, Sparkles, FileText, CheckSquare, BarChart } from "lucide-react"
import { cn } from "@/lib/utils"

interface WorkspaceSidebarMockProps {
  activeWorkspace: "none" | "product-launch"
  activeView: "board" | "docs" | "dashboard"
  onViewChange?: (view: "board" | "docs" | "dashboard") => void
}

export function WorkspaceSidebarMock({
  activeWorkspace,
  activeView,
  onViewChange,
}: WorkspaceSidebarMockProps) {
  const isWorkspaceOpen = activeWorkspace === "product-launch"

  return (
    <div className="flex h-full w-48 flex-col border-r border-zinc-200/80 bg-zinc-50 px-3 py-4 text-xs font-medium text-zinc-700 dark:border-zinc-800 dark:bg-zinc-900/60 dark:text-zinc-300">
      {/* Workspace Switcher Header */}
      <div className="mb-4 flex items-center justify-between rounded-lg border border-zinc-200 bg-white p-2 shadow-xs dark:border-zinc-800 dark:bg-zinc-900">
        <div className="flex items-center gap-1.5 min-w-0">
          <span className="flex h-5.5 w-5.5 shrink-0 items-center justify-center rounded-md bg-blue-600 font-bold text-white text-[10px]">
            N
          </span>
          <span className="truncate font-semibold text-zinc-950 dark:text-white">Notrelix Inc.</span>
        </div>
        <ChevronDown className="h-3 w-3 text-zinc-500 shrink-0" />
      </div>

      {/* Global Navigation Items */}
      <div className="space-y-1">
        <div className="flex items-center gap-2 rounded-md px-2 py-1.5 text-zinc-500 hover:bg-zinc-200/50 dark:hover:bg-zinc-800/50 cursor-pointer">
          <Inbox className="h-3.5 w-3.5" />
          <span>Inbox</span>
          <span className="ml-auto flex h-4 w-4 items-center justify-center rounded-full bg-zinc-200 text-[9px] font-semibold text-zinc-600 dark:bg-zinc-800 dark:text-zinc-400">
            3
          </span>
        </div>
        <div className="flex items-center gap-2 rounded-md px-2 py-1.5 text-zinc-500 hover:bg-zinc-200/50 dark:hover:bg-zinc-800/50 cursor-pointer">
          <Calendar className="h-3.5 w-3.5" />
          <span>Calendar</span>
        </div>
      </div>

      {/* Workspace Tree List */}
      <div className="mt-5 flex-1 space-y-4">
        <div>
          <div className="flex items-center justify-between px-2 py-1 text-[10px] font-bold tracking-wider text-zinc-400 uppercase">
            <span>Workspaces</span>
          </div>
          
          <div className="mt-1 space-y-1">
            {/* Product Launch Workspace */}
            <div
              className={cn(
                "group flex flex-col rounded-md px-2 py-1.5 cursor-pointer transition-colors duration-200",
                isWorkspaceOpen
                  ? "bg-zinc-200/80 text-zinc-950 dark:bg-zinc-850 dark:text-white"
                  : "text-zinc-600 hover:bg-zinc-200/50 dark:text-zinc-400 dark:hover:bg-zinc-800/50"
              )}
            >
              <div className="flex items-center gap-2">
                <Folder className={cn("h-3.5 w-3.5 shrink-0", isWorkspaceOpen ? "text-blue-500" : "text-zinc-400")} />
                <span className="truncate">Product Launch</span>
              </div>

              {/* Child view links if workspace open */}
              {isWorkspaceOpen && (
                <div className="mt-2 ml-4.5 border-l border-zinc-300 pl-2 space-y-1.5 dark:border-zinc-800">
                  <div
                    onClick={() => onViewChange?.("board")}
                    className={cn(
                      "flex items-center gap-1.5 py-0.5 cursor-pointer hover:text-blue-500",
                      activeView === "board" ? "text-blue-600 font-bold dark:text-blue-400" : "text-zinc-500 dark:text-zinc-400"
                    )}
                  >
                    <CheckSquare className="h-3 w-3 shrink-0" />
                    <span>Tasks Board</span>
                  </div>
                  <div
                    onClick={() => onViewChange?.("docs")}
                    className={cn(
                      "flex items-center gap-1.5 py-0.5 cursor-pointer hover:text-blue-500",
                      activeView === "docs" ? "text-blue-600 font-bold dark:text-blue-400" : "text-zinc-500 dark:text-zinc-400"
                    )}
                  >
                    <FileText className="h-3 w-3 shrink-0" />
                    <span>Launch Plan Docs</span>
                  </div>
                  <div
                    onClick={() => onViewChange?.("dashboard")}
                    className={cn(
                      "flex items-center gap-1.5 py-0.5 cursor-pointer hover:text-blue-500",
                      activeView === "dashboard" ? "text-blue-600 font-bold dark:text-blue-400" : "text-zinc-500 dark:text-zinc-400"
                    )}
                  >
                    <BarChart className="h-3 w-3 shrink-0" />
                    <span>Dashboard</span>
                  </div>
                </div>
              )}
            </div>

            {/* Inactive Workspaces */}
            <div className="flex items-center gap-2 rounded-md px-2 py-1.5 text-zinc-500 hover:bg-zinc-200/50 dark:hover:bg-zinc-800/50 cursor-pointer">
              <Folder className="h-3.5 w-3.5 text-zinc-450 dark:text-zinc-655 shrink-0" />
              <span className="truncate">Marketing Campaign</span>
            </div>
            <div className="flex items-center gap-2 rounded-md px-2 py-1.5 text-zinc-500 hover:bg-zinc-200/50 dark:hover:bg-zinc-800/50 cursor-pointer">
              <Folder className="h-3.5 w-3.5 text-zinc-450 dark:text-zinc-655 shrink-0" />
              <span className="truncate">Engineering QA</span>
            </div>
          </div>
        </div>
      </div>

      {/* User Section at bottom */}
      <div className="mt-auto border-t border-zinc-200 pt-3 dark:border-zinc-800">
        <div className="flex items-center gap-2">
          <div className="flex h-6 w-6 items-center justify-center rounded-full bg-zinc-300 text-[10px] font-bold text-zinc-800 dark:bg-zinc-800 dark:text-zinc-200">
            VN
          </div>
          <div className="min-w-0">
            <p className="truncate text-[10px] font-semibold text-zinc-900 dark:text-white">Vinh Nguyen</p>
            <p className="truncate text-[8px] text-zinc-500">vinh@notrelix.com</p>
          </div>
        </div>
      </div>
    </div>
  )
}
