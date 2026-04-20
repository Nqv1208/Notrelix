"use client"

import * as React from "react"
import { Button } from "@/components/ui/button"
import {
  Star,
  Share2,
  MoreHorizontal,
  Clock,
  MessageSquare,
} from "lucide-react"
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"
import { PageEditor } from "@/features/documents/components/page-editor"
import { useEditorStore } from "@/features/documents/store/editor-store"
import { cn } from "@/lib/utils"

type DocumentPageViewProps = {
  workspaceId: string
  pageId: string
}

export function DocumentPageView({ workspaceId, pageId }: DocumentPageViewProps) {
  const { getPage, toggleFavorite } = useEditorStore()
  const page = getPage(pageId)

  if (!page) {
    return (
      <div className="flex-1 flex items-center justify-center text-muted-foreground">
        <div className="text-center">
          <p className="text-lg font-medium">Page not found</p>
          <p className="text-sm mt-1">This page may have been deleted or moved.</p>
        </div>
      </div>
    )
  }

  return (
    <div className="flex flex-col h-full">
      <div className="flex items-center justify-between px-4 h-10 border-b shrink-0">
        <div className="flex items-center gap-2 text-sm text-muted-foreground min-w-0">
          <span className="shrink-0">{page.icon}</span>
          <span className="truncate">{page.title}</span>
          <span className="text-xs text-muted-foreground/50 shrink-0">
            Edited just now
          </span>
        </div>

        <div className="flex items-center gap-0.5">
          <Button variant="ghost" size="icon-sm" onClick={() => toggleFavorite(pageId)}>
            <Star
              className={cn(
                "size-4",
                page.isFavorite && "fill-yellow-400 text-yellow-400"
              )}
            />
          </Button>
          <Button variant="ghost" size="icon-sm">
            <Clock className="size-4" />
          </Button>
          <Button variant="ghost" size="icon-sm">
            <MessageSquare className="size-4" />
          </Button>
          <Button variant="ghost" size="icon-sm">
            <Share2 className="size-4" />
          </Button>
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button variant="ghost" size="icon-sm">
                <MoreHorizontal className="size-4" />
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end" className="w-48">
              <DropdownMenuItem>Export as Markdown</DropdownMenuItem>
              <DropdownMenuItem>Duplicate page</DropdownMenuItem>
              <DropdownMenuItem>Move to...</DropdownMenuItem>
              <DropdownMenuSeparator />
              <DropdownMenuItem className="text-destructive">
                Delete page
              </DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>
        </div>
      </div>

      <PageEditor workspaceId={workspaceId} pageId={pageId} />
    </div>
  )
}
