"use client"

import { Bold, Italic, Strikethrough, Underline } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Separator } from "@/components/ui/separator"
import { useEditorSelection } from "@/features/docs/hooks/use-editor-selection"
import { useDocToolbar } from "@/features/docs/hooks/use-doc-toolbar"
import { useDocsEditorStore } from "@/features/docs/store/editor-store"
import type { Block } from "@/features/docs/types"

export function FloatingFormatToolbar({ pageId, blocks }: { pageId: string; blocks: Block[] }) {
  const focusedBlockId = useDocsEditorStore((state) => state.focusedBlockId)
  const selection = useEditorSelection(focusedBlockId)
  const toolbar = useDocToolbar(pageId, blocks)

  if (!selection.hasSelection || !selection.selectionRect) return null

  return (
    <div
      className="fixed z-50 flex items-center gap-1 rounded-xl border border-border bg-popover p-1 text-popover-foreground shadow-lg"
      style={{
        left: Math.max(12, selection.selectionRect.left + selection.selectionRect.width / 2 - 94),
        top: Math.max(68, selection.selectionRect.top - 46),
      }}
    >
      <Button variant="ghost" size="icon-xs" aria-label="Bold" onClick={() => toolbar.toggleProperty("bold")}>
        <Bold className="size-3.5" />
      </Button>
      <Button variant="ghost" size="icon-xs" aria-label="Italic" onClick={() => toolbar.toggleProperty("italic")}>
        <Italic className="size-3.5" />
      </Button>
      <Button variant="ghost" size="icon-xs" aria-label="Underline" onClick={() => toolbar.toggleProperty("underline")}>
        <Underline className="size-3.5" />
      </Button>
      <Button variant="ghost" size="icon-xs" aria-label="Strikethrough" onClick={() => toolbar.toggleProperty("strike")}>
        <Strikethrough className="size-3.5" />
      </Button>
      <Separator orientation="vertical" className="h-5" />
      <span className="max-w-28 truncate px-2 text-xs text-muted-foreground">{selection.selectionText}</span>
    </div>
  )
}
