"use client"

import { memo, useState } from "react"
import Link from "next/link"
import { ChevronRight, FileText, GripVertical, MoreHorizontal, Plus } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Collapsible, CollapsibleContent, CollapsibleTrigger } from "@/components/ui/collapsible"
import {
  ContextMenu,
  ContextMenuContent,
  ContextMenuItem,
  ContextMenuTrigger,
} from "@/components/ui/context-menu"
import { cn } from "@/lib/utils"
import type { PageTreeNode } from "@/features/docs/types"

interface PageTreeProps {
  tree: PageTreeNode[]
  workspaceId: string
  density?: "default" | "compact"
}

export const PageTree = memo(function PageTree({ tree, workspaceId, density = "default" }: PageTreeProps) {
  return (
    <nav aria-label="Page tree" className="space-y-0.5">
      {tree.map((node) => (
        <PageTreeItem key={node.id} node={node} workspaceId={workspaceId} density={density} />
      ))}
    </nav>
  )
})

function PageTreeItem({
  node,
  workspaceId,
  density,
}: {
  node: PageTreeNode
  workspaceId: string
  density: "default" | "compact"
}) {
  const [open, setOpen] = useState(node.depth < 1)
  const hasChildren = node.children.length > 0

  return (
    <Collapsible open={open} onOpenChange={setOpen}>
      <ContextMenu>
        <ContextMenuTrigger asChild>
          <div
            className={cn(
              "group flex items-center gap-1 rounded-lg text-sm text-muted-foreground transition hover:bg-muted hover:text-foreground",
              density === "compact" ? "h-8 px-1.5" : "h-10 px-2"
            )}
            style={{ paddingLeft: density === "compact" ? 6 + node.depth * 14 : 8 + node.depth * 18 }}
          >
            <CollapsibleTrigger asChild disabled={!hasChildren}>
              <Button
                variant="ghost"
                size="icon-xs"
                className={cn("size-5 shrink-0", !hasChildren && "opacity-0")}
                aria-label={open ? "Collapse page" : "Expand page"}
              >
                <ChevronRight className={cn("size-3 transition-transform", open && "rotate-90")} />
              </Button>
            </CollapsibleTrigger>
            <GripVertical className="size-3.5 shrink-0 text-muted-foreground opacity-0 transition group-hover:opacity-100" />
            <Link href={`/${workspaceId}/docs/${node.id}`} className="flex min-w-0 flex-1 items-center gap-2">
              <span className="w-5 shrink-0 text-center text-xs">{node.icon ?? <FileText className="size-3.5" />}</span>
              <span className="truncate">{node.title}</span>
            </Link>
            <div className="flex shrink-0 opacity-0 transition group-hover:opacity-100">
              <Button variant="ghost" size="icon-xs" aria-label="Add nested page">
                <Plus className="size-3" />
              </Button>
              <Button variant="ghost" size="icon-xs" aria-label="Page actions">
                <MoreHorizontal className="size-3" />
              </Button>
            </div>
          </div>
        </ContextMenuTrigger>
        <ContextMenuContent>
          <ContextMenuItem>Open</ContextMenuItem>
          <ContextMenuItem>Add subpage</ContextMenuItem>
          <ContextMenuItem>Copy link</ContextMenuItem>
          <ContextMenuItem>Move</ContextMenuItem>
        </ContextMenuContent>
      </ContextMenu>
      {hasChildren ? (
        <CollapsibleContent>
          <PageTree tree={node.children} workspaceId={workspaceId} density={density} />
        </CollapsibleContent>
      ) : null}
    </Collapsible>
  )
}
