"use client"

import * as React from "react"

import {
  Sidebar,
  SidebarContent,
  useSidebar,
} from "@/components/ui/sidebar"
import { WorkspaceSwitcher } from "./sidebar/workspace-switcher"
import { PrimaryNav } from "./sidebar/primary-nav"
import { FavoritesSection } from "./sidebar/favorites-section"
import { RecentSection } from "./sidebar/recent-section"
import { AINav } from "./sidebar/ai-nav"
import { ChevronLeft, ChevronRight } from "lucide-react"
import { cn } from "@/lib/utils"

export function AppSidebar({ ...props }: React.ComponentProps<typeof Sidebar>) {
  return (
    <Sidebar collapsible="icon" {...props} className="border-r border-sidebar-border bg-sidebar text-sidebar-foreground">
      {/* <SidebarHeader className="pt-3 pb-1">
        <LogoNav />
      </SidebarHeader> */}
      <SidebarContent className="gap-1 pt-2">
        <PrimaryNav />
        <FavoritesSection />
        <RecentSection />
        <WorkspaceSwitcher />
        <AINav />
      </SidebarContent>
      <AppSidebarEdgeTrigger />
    </Sidebar>
  )
}

function AppSidebarEdgeTrigger() {
  const { state, toggleSidebar } = useSidebar()
  const collapsed = state === "collapsed"

  return (
    <button
      type="button"
      aria-label={collapsed ? "Expand sidebar" : "Collapse sidebar"}
      onClick={toggleSidebar}
      className={cn(
        "absolute right-[-14px] top-5 z-20 hidden size-7 items-center justify-center rounded-full border border-border bg-card text-muted-foreground opacity-0 shadow-sm transition group-hover/sidebar:opacity-100 hover:bg-muted hover:text-foreground lg:flex",
        collapsed && "right-[-18px] opacity-0"
      )}
    >
      {collapsed ? <ChevronRight className="size-4" /> : <ChevronLeft className="size-4" />}
    </button>
  )
}
