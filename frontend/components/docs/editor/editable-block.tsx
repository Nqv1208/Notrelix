"use client"

import type { KeyboardEvent, ReactNode } from "react"
import { useEffect, useMemo, useRef, useState } from "react"
import { useSortable } from "@dnd-kit/sortable"
import { CSS } from "@dnd-kit/utilities"
import { ChevronRight, Code2, ImageIcon, MoreHorizontal, SquareKanban } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Checkbox } from "@/components/ui/checkbox"
import { Textarea } from "@/components/ui/textarea"
import { useCreateBlock } from "@/features/docs/hooks/use-create-block"
import { useSlashCommand } from "@/features/docs/hooks/use-slash-command"
import { useUpdateBlock } from "@/features/docs/hooks/use-update-block"
import { useDocsEditorStore } from "@/features/docs/store/editor-store"
import type { Block, BlockType } from "@/features/docs/types"
import { cn } from "@/lib/utils"
import { BlockDragHandle } from "./block-drag-handle"
import { getBlockTextClass, getHighlightClass } from "./formatting"
import { SlashCommandMenu } from "./slash-command-menu"

interface EditableBlockProps {
  block: Block
  pageId: string
}

export function EditableBlock({ block, pageId }: EditableBlockProps) {
  const [text, setText] = useState(block.properties.text ?? "")
  const slashCommand = useSlashCommand()
  const updateBlock = useUpdateBlock(pageId)
  const createBlock = useCreateBlock(pageId)
  const setFocusedBlockId = useDocsEditorStore((state) => state.setFocusedBlockId)
  const { attributes, listeners, setNodeRef, transform, transition, isDragging } = useSortable({
    id: block.id,
    data: { type: "doc-block", block },
  })

  function persistText(next = text) {
    if (next !== (block.properties.text ?? "")) {
      updateBlock.mutate({ blockId: block.id, payload: { properties: { text: next } } })
    }
  }

  function createBelow(type: BlockType = "paragraph") {
    createBlock.mutate({ type, position: block.position + 0.5, properties: defaultBlockProperties(type) })
  }

  function convertTo(type: BlockType) {
    updateBlock.mutate({
      blockId: block.id,
      payload: {
        type,
        properties: { ...defaultBlockProperties(type), text: text.replace(/^\/\S*\s?/, "") },
      },
    })
    slashCommand.reset()
  }

  function handleKeyDown(event: KeyboardEvent<HTMLTextAreaElement>) {
    if (event.key === "/" && text.trim().length === 0) {
      slashCommand.setOpen(true)
      slashCommand.setQuery("")
      return
    }
    if (slashCommand.open) {
      if (event.key === "Escape") {
        slashCommand.reset()
        return
      }
      window.requestAnimationFrame(() => {
        const value = event.currentTarget.value
        const slashIndex = value.lastIndexOf("/")
        slashCommand.setQuery(slashIndex >= 0 ? value.slice(slashIndex + 1) : "")
      })
    }
    if (event.key === "Enter" && !event.shiftKey) {
      event.preventDefault()
      persistText()
      createBelow("paragraph")
    }
  }

  const style = {
    transform: CSS.Transform.toString(transform),
    transition,
    opacity: isDragging ? 0.55 : 1,
  }

  return (
    <div
      ref={setNodeRef}
      className={cn(
        "group/block relative flex gap-2 rounded-lg py-0.5 transition hover:bg-muted/70 focus-within:bg-muted/50",
        isDragging && "bg-accent"
      )}
      style={style}
      onFocus={() => setFocusedBlockId(block.id)}
    >
      <BlockDragHandle
        label={block.properties.text ?? block.type}
        attributes={attributes}
        listeners={listeners}
        onAdd={() => createBelow("paragraph")}
      />
      <div className="min-w-0 flex-1">
        <BlockContent
          block={block}
          text={text}
          setText={setText}
          onKeyDown={handleKeyDown}
          onBlur={() => persistText()}
        />
      </div>
      <Button variant="ghost" size="icon-xs" className="mr-1 mt-1 opacity-0 transition group-hover/block:opacity-100" aria-label="Block actions">
        <MoreHorizontal className="size-3.5" />
      </Button>
      <SlashCommandMenu open={slashCommand.open} items={slashCommand.items} onSelect={convertTo} />
    </div>
  )
}

function BlockContent({
  block,
  text,
  setText,
  onKeyDown,
  onBlur,
}: {
  block: Block
  text: string
  setText: (value: string) => void
  onKeyDown: (event: KeyboardEvent<HTMLTextAreaElement>) => void
  onBlur: () => void
}) {
  const updateBlock = useUpdateBlock(block.pageId)
  const textClass = useMemo(() => getBlockTextClass(block), [block])

  if (block.type === "divider") return <hr className="my-4 border-border" />
  if (block.type === "table") {
    return (
      <div className="my-3 overflow-hidden rounded-xl border border-border">
        {(block.properties.rows ?? []).map((row, rowIndex) => (
          <div key={rowIndex} className="grid grid-cols-3 border-b border-border last:border-b-0">
            {row.map((cell, cellIndex) => (
              <div key={cellIndex} className={cn("p-3 text-sm", rowIndex === 0 && "bg-muted font-semibold")}>
                {cell}
              </div>
            ))}
          </div>
        ))}
      </div>
    )
  }
  if (block.type === "board_reference") {
    return (
      <div className="my-2 flex items-center gap-3 rounded-xl border border-border bg-card p-3">
        <SquareKanban className="size-4 text-primary" />
        <span className="text-sm font-medium text-foreground">{block.properties.title ?? "Linked board"}</span>
      </div>
    )
  }
  if (block.type === "page_reference") {
    return (
      <div className="my-2 flex items-center gap-3 rounded-xl border border-border bg-muted p-3">
        <ChevronRight className="size-4 text-primary" />
        <span className="text-sm font-medium text-foreground">{block.properties.title ?? "Linked page"}</span>
      </div>
    )
  }
  if (block.type === "image" || block.type === "embed") {
    return (
      <div className="my-2 flex h-36 items-center justify-center rounded-xl border border-dashed border-border bg-muted text-sm text-muted-foreground">
        <ImageIcon className="mr-2 size-4" />
        {block.type === "image" ? "Image block" : "Embed block"}
      </div>
    )
  }
  if (block.type === "code") {
    return (
      <div className="my-2 rounded-xl bg-muted p-3">
        <div className="mb-2 flex items-center gap-2 text-xs text-muted-foreground">
          <Code2 className="size-3.5" />
          {block.properties.language ?? "code"}
        </div>
        <EditableTextarea text={text} setText={setText} onKeyDown={onKeyDown} onBlur={onBlur} className="font-mono text-sm" />
      </div>
    )
  }
  if (block.type === "todo") {
    return (
      <div className="flex items-start gap-2 py-1.5">
        <Checkbox
          checked={Boolean(block.properties.checked)}
          onCheckedChange={(checked) => updateBlock.mutate({ blockId: block.id, payload: { properties: { checked: checked === true } } })}
          className="mt-2"
        />
        <EditableTextarea text={text} setText={setText} onKeyDown={onKeyDown} onBlur={onBlur} className={cn(textClass, block.properties.checked && "text-muted-foreground line-through")} />
      </div>
    )
  }
  if (block.type === "callout") {
    return (
      <div className={cn("my-2 flex gap-3 rounded-xl border border-border p-3", getHighlightClass(block.properties) || "bg-muted")}>
        <span className="text-sm">{block.properties.icon ?? "i"}</span>
        <EditableTextarea text={text} setText={setText} onKeyDown={onKeyDown} onBlur={onBlur} className={textClass} />
      </div>
    )
  }
  if (block.type === "quote") {
    return (
      <blockquote className="my-2 border-l-4 border-primary pl-4">
        <EditableTextarea text={text} setText={setText} onKeyDown={onKeyDown} onBlur={onBlur} className={cn("text-lg", textClass)} />
      </blockquote>
    )
  }

  return (
    <EditableTextarea
      text={text}
      setText={setText}
      onKeyDown={onKeyDown}
      onBlur={onBlur}
      className={cn(
        textClass,
        block.type === "heading_1" && "text-3xl font-semibold tracking-[-0.018em]",
        block.type === "heading_2" && "text-2xl font-semibold tracking-[-0.015em]",
        block.type === "heading_3" && "text-xl font-semibold",
        block.type === "bulleted_list" && "pl-5 before:absolute before:left-1 before:top-4 before:size-1.5 before:rounded-full before:bg-muted-foreground",
        block.type === "numbered_list" && "pl-5",
        block.type === "toggle" && "pl-5"
      )}
      prefix={getPrefix(block)}
    />
  )
}

function EditableTextarea({
  text,
  setText,
  onKeyDown,
  onBlur,
  className,
  prefix,
}: {
  text: string
  setText: (value: string) => void
  onKeyDown: (event: KeyboardEvent<HTMLTextAreaElement>) => void
  onBlur: () => void
  className?: string
  prefix?: ReactNode
}) {
  const ref = useRef<HTMLTextAreaElement | null>(null)

  useEffect(() => {
    const textarea = ref.current
    if (!textarea) return
    textarea.style.height = "auto"
    textarea.style.height = `${textarea.scrollHeight}px`
  }, [text])

  return (
    <div className="relative min-w-0 flex-1">
      {prefix}
      <Textarea
        ref={ref}
        value={text}
        onChange={(event) => setText(event.target.value)}
        onKeyDown={onKeyDown}
        onBlur={onBlur}
        placeholder="Type / for commands"
        className={cn("min-h-8 resize-none overflow-hidden border-0 bg-transparent px-0 py-1.5 shadow-none focus-visible:ring-0", className)}
        rows={1}
      />
    </div>
  )
}

function getPrefix(block: Block) {
  if (block.type === "numbered_list") {
    return <span className="absolute left-1 top-2.5 text-sm text-muted-foreground">{Math.max(1, Math.round(block.position))}.</span>
  }
  if (block.type === "toggle") {
    return <ChevronRight className="absolute left-0 top-2.5 size-4 text-muted-foreground" />
  }
  return null
}

function defaultBlockProperties(type: BlockType) {
  if (type === "todo") return { text: "", checked: false }
  if (type === "callout") return { text: "", icon: "i", highlight: "accent" as const }
  if (type === "code") return { text: "", language: "tsx", fontFamily: "mono" as const }
  return { text: "" }
}
