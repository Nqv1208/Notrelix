"use client"

import { FileText } from "lucide-react"
import { Button } from "@/components/ui/button"
import { useBoardDocsPanel } from "@/features/boards/hooks"

export function CellLinkedPage({ pageId }: { pageId?: string }) {
  const { openDoc } = useBoardDocsPanel()
  if (!pageId) return <span className="text-sm text-muted-foreground">No doc</span>

  return (
    <Button variant="ghost" size="sm" className="h-8 justify-start px-2 text-muted-foreground" onClick={() => openDoc(pageId)}>
      <FileText className="size-4 text-primary" />
      <span className="truncate">{pageId}</span>
    </Button>
  )
}
