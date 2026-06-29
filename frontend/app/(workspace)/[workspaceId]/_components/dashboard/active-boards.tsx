"use client"

import { Kanban, ArrowRight } from "lucide-react"
import Link from "next/link"
import { ACTIVE_BOARDS } from "./workspace-mock-data"
import { isMockModeEnabled } from "@/lib/config/mock-mode"
import { useWorkspaceBoards } from "@/features/work-management"
import { getWorkspaceBoardHref, getWorkspaceBoardsHref } from "@/features/workspace"
import { cn } from "@/lib/utils"
import { LoadingState, EmptyState, ErrorState } from "@/components/feedback"

interface ActiveBoardsProps {
  workspaceId: string
}

export function ActiveBoards({ workspaceId }: ActiveBoardsProps) {
  const isMock = isMockModeEnabled("work-management")
  const realBoards = useWorkspaceBoards(workspaceId)

  if (isMock) {
    return (
      <section className="rounded-2xl border border-border/50 bg-card p-5">
        <div className="flex items-center justify-between mb-4">
          <div className="flex items-center gap-2">
            <Kanban className="size-4 text-primary" />
            <h3 className="text-[15px] font-semibold tracking-[-0.01em] text-foreground" style={{ fontFamily: "var(--font-poppins)" }}>
              Active Boards
            </h3>
          </div>
          <Link
            href={getWorkspaceBoardsHref(workspaceId) as never}
            className="flex items-center gap-1 text-xs font-medium text-muted-foreground hover:text-foreground transition-colors"
          >
            All boards <ArrowRight className="size-3" />
          </Link>
        </div>

        <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
          {ACTIVE_BOARDS.map((board) => {
            const progress = Math.round((board.completedCards / board.totalCards) * 100)
            return (
              <div
                key={board.id}
                className="group relative flex flex-col rounded-2xl border border-border/50 bg-muted/30 p-4 transition-all duration-150 hover:border-border hover:shadow-md hover:-translate-y-0.5 cursor-pointer overflow-hidden"
              >
                {/* Accent Bar */}
                <div
                  className="absolute top-0 left-0 w-1 h-full rounded-l-2xl"
                  style={{ backgroundColor: board.accentColor }}
                />

                {/* Title + Due */}
                <div className="flex items-start justify-between gap-2 pl-2.5">
                  <h4 className="text-sm font-semibold text-foreground leading-snug group-hover:text-primary transition-colors">
                    {board.title}
                  </h4>
                  {board.dueDate && (
                    <span className="shrink-0 text-[10px] font-medium px-2 py-0.5 rounded-full bg-muted text-muted-foreground whitespace-nowrap">
                      {board.dueDate}
                    </span>
                  )}
                </div>

                {/* Progress */}
                <div className="mt-3 pl-2.5 space-y-1.5">
                  <div className="flex items-center justify-between text-xs text-muted-foreground">
                    <span>{board.completedCards}/{board.totalCards} cards</span>
                    <span className="font-medium" style={{ color: board.accentColor }}>{progress}%</span>
                  </div>
                  <div className="h-1.5 rounded-full bg-muted overflow-hidden">
                    <div
                      className="h-full rounded-full transition-all duration-500 ease-out"
                      style={{
                        width: `${progress}%`,
                        backgroundColor: board.accentColor,
                      }}
                    />
                  </div>
                </div>

                {/* Footer — Avatars */}
                <div className="flex items-center gap-2 mt-3 pl-2.5">
                  <div className="flex -space-x-1.5">
                    {board.memberAvatars.map((initial, i) => (
                      <div
                        key={i}
                        className={cn(
                          "size-6 rounded-full border-2 border-card text-[9px] font-semibold flex items-center justify-center",
                          i === 0 && "bg-violet-100 text-violet-700 dark:bg-violet-900/40 dark:text-violet-300",
                          i === 1 && "bg-sky-100 text-sky-700 dark:bg-sky-900/40 dark:text-sky-300",
                          i === 2 && "bg-amber-100 text-amber-700 dark:bg-amber-900/40 dark:text-amber-300",
                        )}
                      >
                        {initial}
                      </div>
                    ))}
                  </div>
                  <span className="text-xs text-muted-foreground">
                    {board.totalCards - board.completedCards} remaining
                  </span>
                </div>
              </div>
            )
          })}
        </div>
      </section>
    )
  }

  // Real data mode
  if (realBoards.isLoading) {
    return (
      <section className="rounded-2xl border border-border/50 bg-card p-5">
        <div className="flex items-center justify-between mb-4">
          <div className="flex items-center gap-2">
            <Kanban className="size-4 text-primary" />
            <h3 className="text-[15px] font-semibold tracking-[-0.01em] text-foreground" style={{ fontFamily: "var(--font-poppins)" }}>
              Active Boards
            </h3>
          </div>
        </div>
        <LoadingState className="py-6" />
      </section>
    )
  }

  if (realBoards.error) {
    return (
      <section className="rounded-2xl border border-border/50 bg-card p-5">
        <ErrorState error={realBoards.error} className="py-4" />
      </section>
    )
  }

  if (!realBoards.data || realBoards.data.length === 0) {
    return (
      <section className="rounded-2xl border border-border/50 bg-card p-5">
        <div className="flex items-center justify-between mb-4">
          <div className="flex items-center gap-2">
            <Kanban className="size-4 text-primary" />
            <h3 className="text-[15px] font-semibold tracking-[-0.01em] text-foreground" style={{ fontFamily: "var(--font-poppins)" }}>
              Active Boards
            </h3>
          </div>
        </div>
        <EmptyState
          title="No active boards"
          description="Create a new board to start organizing your team tasks."
          className="py-6"
        />
      </section>
    )
  }

  return (
    <section className="rounded-2xl border border-border/50 bg-card p-5">
      <div className="flex items-center justify-between mb-4">
        <div className="flex items-center gap-2">
          <Kanban className="size-4 text-primary" />
          <h3 className="text-[15px] font-semibold tracking-[-0.01em] text-foreground" style={{ fontFamily: "var(--font-poppins)" }}>
            Active Boards
          </h3>
        </div>
        <Link
          href={getWorkspaceBoardsHref(workspaceId) as never}
          className="flex items-center gap-1 text-xs font-medium text-muted-foreground hover:text-foreground transition-colors"
        >
          All boards <ArrowRight className="size-3" />
        </Link>
      </div>

      <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
        {realBoards.data.map((board) => (
          <Link
            key={board.id}
            href={getWorkspaceBoardHref(workspaceId, board.id) as never}
            className="group relative flex flex-col rounded-2xl border border-border/50 bg-muted/30 p-4 transition-all duration-150 hover:border-border hover:shadow-md hover:-translate-y-0.5 cursor-pointer overflow-hidden"
          >
            {/* Accent Bar */}
            <div className="absolute top-0 left-0 w-1 h-full rounded-l-2xl bg-primary" />

            <div className="flex items-start justify-between gap-2 pl-2.5">
              <h4 className="text-sm font-semibold text-foreground leading-snug group-hover:text-primary transition-colors">
                {board.title}
              </h4>
            </div>

            <div className="mt-4 pl-2.5">
              <p className="text-xs text-muted-foreground line-clamp-2">
                {board.description || "No description provided for this board."}
              </p>
            </div>
          </Link>
        ))}
      </div>
    </section>
  )
}
