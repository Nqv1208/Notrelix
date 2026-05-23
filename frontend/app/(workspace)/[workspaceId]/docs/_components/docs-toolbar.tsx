"use client"

import { Download, Filter, ListFilter, Share2 } from "lucide-react"
import { Button } from "@/components/ui/button"
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuTrigger } from "@/components/ui/dropdown-menu"
import { NewPageButton } from "./new-page-button"

interface DocsToolbarProps {
  workspaceId: string
}

export function DocsToolbar({ workspaceId }: DocsToolbarProps) {
  return (
    <div className="flex min-w-0 flex-1 items-center justify-between gap-3">
      <div className="min-w-0">
        <p className="truncate text-sm font-semibold text-foreground">Workspace docs</p>
        <p className="hidden text-xs text-muted-foreground sm:block">Pages, specs, decisions, and shared context</p>
      </div>
      <div className="flex items-center gap-2">
        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <Button variant="outline" size="sm" className="hidden bg-card md:inline-flex">
              <ListFilter className="size-4" />
              View
            </Button>
          </DropdownMenuTrigger>
          <DropdownMenuContent align="end">
            <DropdownMenuItem><Filter className="size-4" /> Recent first</DropdownMenuItem>
            <DropdownMenuItem><Share2 className="size-4" /> Shared docs</DropdownMenuItem>
            <DropdownMenuItem><Download className="size-4" /> Export index</DropdownMenuItem>
          </DropdownMenuContent>
        </DropdownMenu>
        <NewPageButton workspaceId={workspaceId} />
      </div>
    </div>
  )
}
