"use client"

import { useDemoTimeline } from "@/hooks/use-demo-timeline"
import { MockBrowserFrame } from "./MockBrowserFrame"
import { WorkspaceSidebarMock } from "./WorkspaceSidebarMock"
import { KanbanBoardMock } from "./KanbanBoardMock"
import { DocsEditorMock } from "./DocsEditorMock"
import { DashboardMock } from "./DashboardMock"
import { TaskModalMock } from "./TaskModalMock"
import { AnimatedCursor } from "./AnimatedCursor"
import { LayoutDashboard, FileText, CheckSquare, Sparkles } from "lucide-react"
import { cn } from "@/lib/utils"

export function HeroInteractiveDemo() {
  const {
    activeScene,
    activeView,
    activeWorkspace,
    tasks,
    isModalOpen,
    selectedTask,
    aiCommandText,
    docsText,
    cursorPos,
    cursorAction,
  } = useDemoTimeline()

  return (
    <MockBrowserFrame url="notrelix.com/workspace/product-launch">
      <div className="flex h-full w-full bg-zinc-50 text-zinc-950 dark:bg-zinc-950 dark:text-zinc-50 relative select-none">
        
        {/* Workspace Sidebar Left */}
        <WorkspaceSidebarMock
          activeWorkspace={activeWorkspace}
          activeView={activeView}
        />

        {/* Main Workspace Canvas Right */}
        <div className="flex-1 flex flex-col min-w-0 h-full relative">
          
          {/* Workspace Tab Header */}
          <div className="flex h-12 items-center justify-between border-b border-zinc-200/80 bg-white px-4 shrink-0 dark:border-zinc-800 dark:bg-zinc-900">
            <div className="flex items-center gap-3">
              <span className="font-bold text-xs text-zinc-900 dark:text-white">🚀 Product Launch</span>
              
              {/* Dynamic status dots */}
              <div className="hidden sm:flex items-center gap-1.5 rounded-full bg-emerald-50 border border-emerald-100 px-2 py-0.5 text-[9px] font-bold text-emerald-800 dark:bg-emerald-950/30 dark:border-emerald-900/60 dark:text-emerald-300">
                <span className="h-1.5 w-1.5 rounded-full bg-emerald-500 animate-pulse" />
                <span>Live Plan</span>
              </div>
            </div>

            {/* View Switching Navigation */}
            <div className="flex items-center gap-1 rounded-lg bg-zinc-100 p-0.5 dark:bg-zinc-950">
              <button
                className={cn(
                  "flex items-center gap-1 rounded px-2.5 py-1 text-[10px] font-semibold transition-colors duration-150",
                  activeView === "board"
                    ? "bg-white text-zinc-900 shadow-xs dark:bg-zinc-900 dark:text-white"
                    : "text-zinc-550 dark:text-zinc-400 hover:text-zinc-700"
                )}
              >
                <CheckSquare className="h-3 w-3 shrink-0" />
                <span>Board</span>
              </button>
              <button
                className={cn(
                  "flex items-center gap-1 rounded px-2.5 py-1 text-[10px] font-semibold transition-colors duration-150",
                  activeView === "docs"
                    ? "bg-white text-zinc-900 shadow-xs dark:bg-zinc-900 dark:text-white"
                    : "text-zinc-550 dark:text-zinc-400 hover:text-zinc-700"
                )}
              >
                <FileText className="h-3 w-3 shrink-0" />
                <span>Docs</span>
              </button>
              <button
                className={cn(
                  "flex items-center gap-1 rounded px-2.5 py-1 text-[10px] font-semibold transition-colors duration-150",
                  activeView === "dashboard"
                    ? "bg-white text-zinc-900 shadow-xs dark:bg-zinc-900 dark:text-white"
                    : "text-zinc-550 dark:text-zinc-400 hover:text-zinc-700"
                )}
              >
                <LayoutDashboard className="h-3 w-3 shrink-0" />
                <span>Dashboard</span>
              </button>
            </div>
          </div>

          {/* Active View Container */}
          <div className="flex-1 overflow-hidden min-h-0 relative">
            {activeView === "board" && (
              <KanbanBoardMock
                tasks={tasks}
                aiCommandText={aiCommandText}
                activeScene={activeScene}
              />
            )}

            {activeView === "docs" && (
              <DocsEditorMock docsText={docsText} />
            )}

            {activeView === "dashboard" && (
              <DashboardMock />
            )}

            {/* Task Detail Modal Popup */}
            <TaskModalMock
              task={selectedTask}
              isOpen={isModalOpen}
            />
          </div>
        </div>

        {/* Animated Custom Cursor */}
        <AnimatedCursor
          x={cursorPos.x}
          y={cursorPos.y}
          action={cursorAction}
        />
      </div>
    </MockBrowserFrame>
  )
}
