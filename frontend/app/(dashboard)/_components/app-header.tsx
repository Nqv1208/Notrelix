"use client"

import type { ComponentType } from "react"
import { useState } from "react"
import { SidebarTrigger } from "@/components/ui/sidebar"
import { GlobalSearchDialog } from "./header/global-search-dialog"
import { UserMenu } from "./header/user-menu"
import { Bell, Grip, HelpCircle, Hexagon, Inbox, Puzzle, Search, UserPlus } from "lucide-react"
import { cn } from "@/lib/utils"

type UtilityButtonProps = {
  icon: ComponentType<{ className?: string }>
  onClick?: () => void
  badge?: number
}

function UtilityButton({ icon: Icon, onClick, badge }: UtilityButtonProps) {
  return (
    <button
      type="button"
      onClick={onClick}
      className="relative rounded-lg p-2 text-muted-foreground transition-colors hover:bg-muted hover:text-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
    >
      <Icon className="size-[18px]" />
      {badge !== undefined && badge > 0 ? (
        <span className="absolute right-1 top-1 flex size-3.5 items-center justify-center rounded-full border border-card bg-primary text-[9px] font-bold text-primary-foreground">
          {badge}
        </span>
      ) : null}
    </button>
  )
}

interface AppHeaderProps {
  showSidebarTrigger?: boolean
  className?: string
}

export function AppHeader({
  showSidebarTrigger = true,
  className,
}: AppHeaderProps) {
  const [searchOpen, setSearchOpen] = useState(false)

  return (
    <>
      <header
        className={cn(
          "sticky top-0 z-[80] flex h-12 shrink-0 items-center justify-between border-b border-border bg-app-header/95 px-4 shadow-sm backdrop-blur-xl",
          className
        )}
      >
        <div className="flex min-w-0 items-center gap-3">
          {showSidebarTrigger ? (
            <SidebarTrigger className="-ml-1 text-muted-foreground hover:bg-muted hover:text-foreground" />
          ) : null}
          <div className="flex min-w-0 items-center gap-2">
            <Hexagon className="size-5 fill-primary text-primary" />
            <span className="hidden truncate text-lg font-bold text-foreground sm:block">
              Notrelix <span className="font-normal text-muted-foreground">work management</span>
            </span>
          </div>
        </div>

        <div className="mx-4 hidden min-w-0 flex-1 justify-center lg:flex" />

        <div className="flex items-center gap-1 sm:gap-2">
          <UtilityButton icon={Bell} badge={1} />
          <UtilityButton icon={Inbox} />
          <UtilityButton icon={UserPlus} />
          <UtilityButton icon={Puzzle} />
          <UtilityButton icon={Search} onClick={() => setSearchOpen(true)} />
          <UtilityButton icon={HelpCircle} />
          <div className="mx-1 h-6 w-px bg-border" />
          <UtilityButton icon={Grip} />
          <UserMenu />
        </div>
      </header>

      <GlobalSearchDialog open={searchOpen} onOpenChange={setSearchOpen} />
    </>
  )
}
