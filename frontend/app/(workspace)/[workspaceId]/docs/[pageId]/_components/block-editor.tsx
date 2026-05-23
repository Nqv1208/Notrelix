"use client"

import type { CSSProperties, KeyboardEvent, ReactNode } from "react"
import { useMemo, useState } from "react"
import { ChevronRight, GripVertical, ImageIcon, MoreHorizontal, Plus, SquareKanban } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Checkbox } from "@/components/ui/checkbox"
import { Textarea } from "@/components/ui/textarea"
import { useCreateBlock } from "@/features/docs/hooks/use-create-block"
import { useUpdateBlock } from "@/features/docs/hooks/use-update-block"
import { useDocsEditorStore } from "@/features/docs/store/editor-store"
import type { Block } from "@/features/docs/types"
import { cn } from "@/lib/utils"
import { SlashCommand } from "./slash-command"

interface BlockEditorProps {
  block: Block
  pageId: string
}

export function BlockEditor({ block, pageId }: BlockEditorProps) {
  const [text, setText] = useState(block.properties.text ?? "")
  const [slashOpen, setSlashOpen] = useState(false)
  const updateBlock = useUpdateBlock(pageId)
  const createBlock = useCreateBlock(pageId)
  const setFocusedBlockId = useDocsEditorStore((state) => state.setFocusedBlockId)

  function persistText(next = text) {
    if (next !== (block.properties.text ?? "")) {
      updateBlock.mutate({ blockId: block.id, payload: { properties: { text: next } } })
    }
  }

  function onKeyDown(event: KeyboardEvent<HTMLTextAreaElement>) {
    if (event.key === "/" && !text) setSlashOpen(true)
    if (event.key === "Enter" && !event.shiftKey) {
      event.preventDefault()
      persistText()
      createBlock.mutate({ type: "paragraph", position: block.position + 0.5, properties: { text: "" } })
    }
  }

  const chrome = (
    <div className="flex w-12 shrink-0 items-start justify-end gap-0.5 pt-1 opacity-0 transition group-hover:opacity-100">
      <Button variant="ghost" size="icon-xs" aria-label="Drag block">
        <GripVertical className="size-3.5 text-muted-foreground" />
      </Button>
      <Button variant="ghost" size="icon-xs" aria-label="Add block" onClick={() => createBlock.mutate({ type: "paragraph", position: block.position + 0.5, properties: { text: "" } })}>
        <Plus className="size-3.5" />
      </Button>
    </div>
  )

  return (
    <div className="group relative flex gap-2 rounded-lg py-0.5 hover:bg-muted/70" onFocus={() => setFocusedBlockId(block.id)}>
      {chrome}
      <div className="min-w-0 flex-1">
        <BlockContent block={block} text={text} setText={setText} onKeyDown={onKeyDown} onBlur={() => persistText()} />
      </div>
      <Button variant="ghost" size="icon-xs" className="mr-1 mt-1 opacity-0 transition group-hover:opacity-100" aria-label="Block actions">
        <MoreHorizontal className="size-3.5" />
      </Button>
      <SlashCommand
        open={slashOpen}
        onOpenChange={setSlashOpen}
        onSelect={(type) => {
          updateBlock.mutate({ blockId: block.id, payload: { type, properties: type === "todo" ? { text, checked: false } : { text } } })
          setSlashOpen(false)
        }}
      />
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
  const textStyle = useMemo(() => getTextStyle(block), [block])

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
      <pre className="my-2 overflow-x-auto rounded-xl bg-muted p-4 text-sm leading-6 text-foreground">
        <code>{text}</code>
      </pre>
    )
  }
  if (block.type === "todo") {
    return (
      <div className="flex items-start gap-2 py-1.5">
        <Checkbox
          checked={Boolean(block.properties.checked)}
          onCheckedChange={(checked) => updateBlock.mutate({ blockId: block.id, payload: { properties: { checked: checked === true } } })}
          className="mt-1"
        />
        <EditableTextarea text={text} setText={setText} onKeyDown={onKeyDown} onBlur={onBlur} style={textStyle} className={block.properties.checked ? "text-muted-foreground line-through" : ""} />
      </div>
    )
  }
  if (block.type === "callout") {
    return (
      <div className="my-2 flex gap-3 rounded-xl border border-border bg-muted p-3">
        <span>{block.properties.icon ?? "💡"}</span>
        <EditableTextarea text={text} setText={setText} onKeyDown={onKeyDown} onBlur={onBlur} style={textStyle} />
      </div>
    )
  }
  if (block.type === "quote") {
    return (
      <blockquote className="my-2 border-l-4 border-primary pl-4 text-lg leading-8 text-foreground">
        <EditableTextarea text={text} setText={setText} onKeyDown={onKeyDown} onBlur={onBlur} style={textStyle} />
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
        block.type === "heading_1" && "text-3xl font-semibold tracking-[-0.018em]",
        block.type === "heading_2" && "text-2xl font-semibold tracking-[-0.015em]",
        block.type === "heading_3" && "text-xl font-semibold",
        block.type === "bulleted_list" && "pl-5 before:absolute before:left-1 before:top-4 before:size-1.5 before:rounded-full before:bg-muted-foreground",
        block.type === "numbered_list" && "pl-5",
        block.type === "toggle" && "pl-5"
      )}
      prefix={block.type === "numbered_list" ? <span className="absolute left-1 top-2.5 text-sm text-muted-foreground">{block.position}.</span> : block.type === "toggle" ? <ChevronRight className="absolute left-0 top-2.5 size-4 text-muted-foreground" /> : null}
      style={textStyle}
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
  style,
}: {
  text: string
  setText: (value: string) => void
  onKeyDown: (event: KeyboardEvent<HTMLTextAreaElement>) => void
  onBlur: () => void
  className?: string
  prefix?: ReactNode
  style?: CSSProperties
}) {
  return (
    <div className="relative min-w-0 flex-1">
      {prefix}
      <Textarea
        value={text}
        onChange={(event) => setText(event.target.value)}
        onKeyDown={onKeyDown}
        onBlur={onBlur}
        placeholder="Type / for commands"
        className={cn("min-h-8 resize-none border-0 bg-transparent px-0 py-1.5 leading-7 shadow-none focus-visible:ring-0", className)}
        style={style}
        rows={1}
      />
    </div>
  )
}

function getTextStyle(block: Block): CSSProperties {
  const family = {
    inter: "var(--font-body)",
    poppins: "var(--font-display)",
    serif: "var(--font-editorial), Georgia, serif",
    mono: "var(--font-mono)",
  }[block.properties.fontFamily ?? "inter"]

  const size = {
    sm: "0.875rem",
    base: "1rem",
    lg: "1.125rem",
    xl: "1.25rem",
  }[block.properties.fontSize ?? "base"]

  return {
    fontFamily: family,
    fontSize: size,
    textAlign: block.properties.align ?? "left",
  }
}
