"use client"

import { Search, Bell, UserPlus, Settings } from "lucide-react"
import { Button } from "@/components/ui/button"
import { WORKSPACE, MEMBER_INITIALS } from "./workspace-mock-data"
import { cn } from "@/lib/utils"

export function WorkspaceHeader() {
  return (
    <header className="flex items-center justify-between gap-4 pb-2">
      {/* Left — Identity */}
      <div className="flex items-center gap-3.5 min-w-0">
        <div className="flex items-center justify-center size-11 rounded-2xl bg-gradient-to-br from-violet-600 via-indigo-600 to-purple-600 text-xl shadow-sm shadow-violet-500/20 shrink-0 select-none">
          <span>{WORKSPACE.icon}</span>
        </div>
        <div className="min-w-0">
          <h1 className="text-[22px] font-semibold tracking-[-0.02em] text-foreground truncate" style={{ fontFamily: "var(--font-poppins)" }}>
            {WORKSPACE.name}
          </h1>
          <p className="text-xs text-muted-foreground tracking-wide mt-0.5">
            <span className="inline-flex items-center gap-1.5">
              <span className="size-1.5 rounded-full bg-emerald-500 animate-pulse" />
              {WORKSPACE.memberCount} members · {WORKSPACE.plan.charAt(0).toUpperCase() + WORKSPACE.plan.slice(1)} plan
            </span>
          </p>
        </div>
      </div>

      {/* Center — Member Stack */}
      <div className="hidden md:flex items-center -space-x-2">
        {MEMBER_INITIALS.slice(0, 5).map((initials, i) => (
          <div
            key={initials}
            className={cn(
              "size-8 rounded-full border-2 border-background flex items-center justify-center text-[10px] font-semibold shadow-sm",
              i === 0 && "bg-violet-100 text-violet-700 dark:bg-violet-900/40 dark:text-violet-300",
              i === 1 && "bg-sky-100 text-sky-700 dark:bg-sky-900/40 dark:text-sky-300",
              i === 2 && "bg-amber-100 text-amber-700 dark:bg-amber-900/40 dark:text-amber-300",
              i === 3 && "bg-emerald-100 text-emerald-700 dark:bg-emerald-900/40 dark:text-emerald-300",
              i === 4 && "bg-rose-100 text-rose-700 dark:bg-rose-900/40 dark:text-rose-300",
            )}
          >
            {initials}
          </div>
        ))}
        {MEMBER_INITIALS.length > 5 && (
          <div className="size-8 rounded-full border-2 border-background bg-muted flex items-center justify-center text-[10px] font-medium text-muted-foreground">
            +{MEMBER_INITIALS.length - 5}
          </div>
        )}
      </div>

      {/* Right — Actions */}
      <div className="flex items-center gap-1.5">
        <Button variant="ghost" size="icon" className="size-9 rounded-xl text-muted-foreground hover:text-foreground">
          <Search className="size-[18px] stroke-[1.75]" />
        </Button>
        <Button variant="ghost" size="icon" className="size-9 rounded-xl text-muted-foreground hover:text-foreground relative">
          <Bell className="size-[18px] stroke-[1.75]" />
          <span className="absolute top-1.5 right-1.5 size-2 rounded-full bg-violet-500 ring-2 ring-background" />
        </Button>
        <Button
          variant="outline"
          size="sm"
          className="hidden sm:flex h-8 rounded-xl gap-1.5 text-xs font-medium border-border/60 hover:border-violet-300 hover:text-violet-700 dark:hover:border-violet-600 dark:hover:text-violet-300 transition-colors"
        >
          <UserPlus className="size-3.5" />
          Invite
        </Button>
        <Button variant="ghost" size="icon" className="size-9 rounded-xl text-muted-foreground hover:text-foreground">
          <Settings className="size-[18px] stroke-[1.75]" />
        </Button>
      </div>
    </header>
  )
}
