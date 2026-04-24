"use client"

import * as React from "react"
import { useSortable } from "@dnd-kit/sortable"
import { CSS } from "@dnd-kit/utilities"
import { cn } from "@/lib/utils"
import { GripVertical, Plus, Trash2, Copy, ArrowUpDown } from "lucide-react"
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuSub,
  DropdownMenuSubContent,
  DropdownMenuSubTrigger,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"
import type { Block, BlockType } from "../types/document.types"
import { BlockRenderer } from "./block-renderer"
import { useEditorStore } from "../store/editor-store"

type SortableBlockProps = {
  block: Block
}

const TURN_INTO_OPTIONS: { type: BlockType; label: string }[] = [
  { type: "paragraph", label: "Text" },
  { type: "heading_1", label: "Heading 1" },
  { type: "heading_2", label: "Heading 2" },
  { type: "heading_3", label: "Heading 3" },
  { type: "bulleted_list", label: "Bulleted List" },
  { type: "numbered_list", label: "Numbered List" },
  { type: "to_do", label: "To-do" },
  { type: "quote", label: "Quote" },
  { type: "code", label: "Code" },
  { type: "callout", label: "Callout" },
]

export function SortableBlock({ block }: SortableBlockProps) {
  const { addBlock, deleteBlock, duplicateBlock, turnInto, isDragging: globalDragging } =
    useEditorStore()

  const {
    attributes,
    listeners,
    setNodeRef,
    transform,
    transition,
    isDragging,
  } = useSortable({ id: block.id })

  const style: React.CSSProperties = {
    transform: CSS.Translate.toString(transform),
    transition,
  }

  return (
    <div
      ref={setNodeRef}
      style={style}
      className={cn(
        "group/block relative flex items-stretch",
        isDragging && "opacity-50 z-50",
        globalDragging && !isDragging && "transition-transform"
      )}
    >
      <div
        className={cn(
          "flex items-center gap-0.5 opacity-0 group-hover/block:opacity-100 transition-opacity shrink-0 pr-1 pt-0.5",
          isDragging && "opacity-0"
        )}
      >
        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <button className="flex size-6 items-center justify-center rounded hover:bg-accent text-muted-foreground/60 hover:text-muted-foreground">
              <Plus className="size-3.5" />
            </button>
          </DropdownMenuTrigger>
          <DropdownMenuContent align="start" className="w-48">
            {TURN_INTO_OPTIONS.slice(0, 7).map((opt) => (
              <DropdownMenuItem
                key={opt.type}
                onSelect={() => addBlock(opt.type, block.id)}
              >
                {opt.label}
              </DropdownMenuItem>
            ))}
          </DropdownMenuContent>
        </DropdownMenu>

        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <button
              className="flex size-6 items-center justify-center rounded hover:bg-accent text-muted-foreground/60 hover:text-muted-foreground cursor-grab active:cursor-grabbing"
              {...attributes}
              {...listeners}
            >
              <GripVertical className="size-3.5" />
            </button>
          </DropdownMenuTrigger>
          <DropdownMenuContent align="start" className="w-52">
            <DropdownMenuItem onSelect={() => deleteBlock(block.id)}>
              <Trash2 className="size-4 mr-2" />
              Delete
            </DropdownMenuItem>
            <DropdownMenuItem onSelect={() => duplicateBlock(block.id)}>
              <Copy className="size-4 mr-2" />
              Duplicate
            </DropdownMenuItem>
            <DropdownMenuSeparator />
            <DropdownMenuSub>
              <DropdownMenuSubTrigger>
                <ArrowUpDown className="size-4 mr-2" />
                Turn into
              </DropdownMenuSubTrigger>
              <DropdownMenuSubContent className="w-44">
                {TURN_INTO_OPTIONS.map((opt) => (
                  <DropdownMenuItem
                    key={opt.type}
                    onSelect={() => turnInto(block.id, opt.type)}
                    disabled={block.type === opt.type}
                  >
                    {opt.label}
                  </DropdownMenuItem>
                ))}
              </DropdownMenuSubContent>
            </DropdownMenuSub>
          </DropdownMenuContent>
        </DropdownMenu>
      </div>

      <div className="flex-1 min-w-0">
        <BlockRenderer block={block} />
      </div>
    </div>
  )
}
