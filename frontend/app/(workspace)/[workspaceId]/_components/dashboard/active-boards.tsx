"use client"

import { Kanban, ArrowRight } from "lucide-react"
import { ACTIVE_BOARDS } from "./workspace-mock-data"
import { cn } from "@/lib/utils"

export function ActiveBoards() {
  return (
    <section>
      <div className="flex items-center justify-between mb-4">
        <div className="flex items-center gap-2">
          <Kanban className="size-4 text-muted-foreground" />
          <h3 className="text-[15px] font-semibold tracking-[-0.01em] text-foreground" style={{ fontFamily: "var(--font-poppins)" }}>
            Active Boards
          </h3>
        </div>
        <button className="flex items-center gap-1 text-xs font-medium text-muted-foreground hover:text-foreground transition-colors">
          All boards <ArrowRight className="size-3" />
        </button>
      </div>

      <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
        {ACTIVE_BOARDS.map((board) => {
          const progress = Math.round((board.completedCards / board.totalCards) * 100)
          return (
            <div
              key={board.id}
              className="group relative flex flex-col rounded-2xl border border-border/50 bg-card p-4 transition-all duration-150 hover:border-border hover:shadow-md hover:-translate-y-0.5 cursor-pointer overflow-hidden"
            >
              {/* Accent Bar */}
              <div
                className="absolute top-0 left-0 w-1 h-full rounded-l-2xl"
                style={{ backgroundColor: board.accentColor }}
              />

              {/* Title + Due */}
              <div className="flex items-start justify-between gap-2 pl-2.5">
                <h4 className="text-sm font-semibold text-foreground leading-snug group-hover:text-violet-700 dark:group-hover:text-violet-300 transition-colors">
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
