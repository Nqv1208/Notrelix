"use client"

import type { ReactNode } from "react"
import { useState } from "react"
import { Menu } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Sheet, SheetContent, SheetHeader, SheetTitle, SheetTrigger } from "@/components/ui/sheet"
import { AppHeader } from "@/app/(dashboard)/_components/app-header"
import { WorkspaceSidebar } from "./workspace-sidebar"

export function WorkspaceShell({ workspaceId, children }: { workspaceId: string; children: ReactNode }) {
  const [collapsed, setCollapsed] = useState(false)

  return (
    <div className="min-h-svh bg-app-shell text-foreground">
      <AppHeader
        showSidebarTrigger={false}
      />
      <Sheet>
        <SheetTrigger asChild>
          <Button
            variant="outline"
            size="icon-sm"
            className="fixed left-3 top-16 z-50 bg-card/95 shadow-sm lg:hidden"
            aria-label="Open workspace navigation"
          >
            <Menu className="size-4" />
          </Button>
        </SheetTrigger>
        <SheetContent side="left" className="w-[88vw] max-w-[320px] p-0">
          <SheetHeader className="sr-only"><SheetTitle>Workspace navigation</SheetTitle></SheetHeader>
          <WorkspaceSidebar workspaceId={workspaceId} collapsed={false} onCollapse={() => undefined} inSheet />
        </SheetContent>
      </Sheet>
      <div className="flex h-[calc(100svh-3.5rem)] overflow-hidden">
        <WorkspaceSidebar
          workspaceId={workspaceId}
          collapsed={collapsed}
          onCollapse={() => setCollapsed(true)}
          onExpand={() => setCollapsed(false)}
        />
        <div className="min-w-0 flex-1">
          <div className="h-full overflow-auto">{children}</div>
        </div>
      </div>
    </div>
  )
}
