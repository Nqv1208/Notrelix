"use client"

import { MessageSquareText, Plus, Sparkles } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Tooltip, TooltipContent, TooltipTrigger } from "@/components/ui/tooltip"
import { useCreateBlock } from "@/features/docs/hooks/use-create-block"
import { useDocsEditorStore } from "@/features/docs/store/editor-store"

interface FloatingActionsProps {
  pageId: string
  workspaceSlug: string
}

export function FloatingActions({ pageId }: FloatingActionsProps) {
  const createBlock = useCreateBlock(pageId)
  const setCommentsOpen = useDocsEditorStore((state) => state.setCommentsOpen)

  return (
    <div className="fixed bottom-5 left-1/2 z-40 hidden -translate-x-1/2 items-center gap-2 rounded-full border border-border bg-card p-1.5 shadow-[rgba(0,0,0,0.15)_0px_10px_40px] md:flex">
      <Tooltip>
        <TooltipTrigger asChild>
          <Button size="icon-sm" className="rounded-full bg-primary text-primary-foreground hover:bg-primary/90" onClick={() => createBlock.mutate({ type: "paragraph", properties: { text: "" } })}>
            <Plus className="size-4" />
          </Button>
        </TooltipTrigger>
        <TooltipContent>Add block</TooltipContent>
      </Tooltip>
      <Tooltip>
        <TooltipTrigger asChild>
          <Button variant="ghost" size="icon-sm" className="rounded-full" onClick={() => setCommentsOpen(true)}>
            <MessageSquareText className="size-4" />
          </Button>
        </TooltipTrigger>
        <TooltipContent>Comments</TooltipContent>
      </Tooltip>
      <Tooltip>
        <TooltipTrigger asChild>
          <Button variant="ghost" size="icon-sm" className="rounded-full">
            <Sparkles className="size-4" />
          </Button>
        </TooltipTrigger>
        <TooltipContent>AI assist</TooltipContent>
      </Tooltip>
    </div>
  )
}
