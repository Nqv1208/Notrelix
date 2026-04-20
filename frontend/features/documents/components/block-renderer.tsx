"use client"

import * as React from "react"
import { cn } from "@/lib/utils"
import { Checkbox } from "@/components/ui/checkbox"
import type { Block, BlockType } from "../types/document.types"
import { useEditorStore } from "../store/editor-store"

type BlockRendererProps = {
  block: Block
  isSelected?: boolean
  onFocus?: () => void
}

export function BlockRenderer({ block, isSelected, onFocus }: BlockRendererProps) {
  const { updateBlock, toggleTodo, addBlock, deleteBlock, focusedBlockId, setFocusedBlock } =
    useEditorStore()
  const contentRef = React.useRef<HTMLDivElement>(null)
  const isFocused = focusedBlockId === block.id

  React.useEffect(() => {
    if (isFocused && contentRef.current && block.type !== "divider") {
      contentRef.current.focus()
      const selection = window.getSelection()
      if (selection && contentRef.current.childNodes.length > 0) {
        const range = document.createRange()
        range.selectNodeContents(contentRef.current)
        range.collapse(false)
        selection.removeAllRanges()
        selection.addRange(range)
      }
    }
  }, [isFocused, block.type])

  const handleInput = React.useCallback(
    (e: React.FormEvent<HTMLDivElement>) => {
      const text = e.currentTarget.textContent ?? ""
      updateBlock(block.id, { content: text })
    },
    [block.id, updateBlock]
  )

  const handleKeyDown = React.useCallback(
    (e: React.KeyboardEvent<HTMLDivElement>) => {
      if (e.key === "Enter" && !e.shiftKey) {
        e.preventDefault()
        const continuedType: BlockType =
          block.type === "bulleted_list" ||
          block.type === "numbered_list" ||
          block.type === "to_do"
            ? block.type
            : "paragraph"

        if (
          (block.type === "bulleted_list" ||
            block.type === "numbered_list" ||
            block.type === "to_do") &&
          block.content === ""
        ) {
          updateBlock(block.id, { type: "paragraph" })
          return
        }

        addBlock(continuedType, block.id)
      }

      if (e.key === "Backspace" && block.content === "" && block.type !== "paragraph") {
        e.preventDefault()
        updateBlock(block.id, { type: "paragraph" })
        return
      }

      if (e.key === "Backspace" && block.content === "" && block.type === "paragraph") {
        e.preventDefault()
        deleteBlock(block.id)
      }
    },
    [block, addBlock, deleteBlock, updateBlock]
  )

  const handleFocus = React.useCallback(() => {
    setFocusedBlock(block.id)
    onFocus?.()
  }, [block.id, setFocusedBlock, onFocus])

  const editableProps = {
    ref: contentRef,
    contentEditable: true,
    suppressContentEditableWarning: true,
    onInput: handleInput,
    onKeyDown: handleKeyDown,
    onFocus: handleFocus,
    "data-placeholder": getPlaceholder(block.type),
    className: "outline-none empty:before:content-[attr(data-placeholder)] empty:before:text-muted-foreground/50",
  }

  switch (block.type) {
    case "heading_1":
      return (
        <div
          {...editableProps}
          className={cn(
            editableProps.className,
            "text-3xl font-bold tracking-tight mt-8 mb-1 first:mt-0"
          )}
        >
          {block.content}
        </div>
      )

    case "heading_2":
      return (
        <div
          {...editableProps}
          className={cn(
            editableProps.className,
            "text-2xl font-semibold tracking-tight mt-6 mb-1"
          )}
        >
          {block.content}
        </div>
      )

    case "heading_3":
      return (
        <div
          {...editableProps}
          className={cn(
            editableProps.className,
            "text-xl font-semibold mt-4 mb-1"
          )}
        >
          {block.content}
        </div>
      )

    case "paragraph":
      return (
        <div
          {...editableProps}
          className={cn(
            editableProps.className,
            "text-base leading-relaxed min-h-[1.5em]"
          )}
        >
          {block.content}
        </div>
      )

    case "bulleted_list":
      return (
        <div className="flex gap-2 items-start">
          <span className="mt-[0.4em] text-muted-foreground select-none shrink-0">•</span>
          <div
            {...editableProps}
            className={cn(editableProps.className, "flex-1 min-h-[1.5em]")}
          >
            {block.content}
          </div>
        </div>
      )

    case "numbered_list":
      return (
        <div className="flex gap-2 items-start">
          <span className="mt-[0.05em] text-muted-foreground select-none shrink-0 min-w-[1.2em] text-right">
            {block.position}.
          </span>
          <div
            {...editableProps}
            className={cn(editableProps.className, "flex-1 min-h-[1.5em]")}
          >
            {block.content}
          </div>
        </div>
      )

    case "to_do":
      return (
        <div className="flex gap-2 items-start group/todo">
          <Checkbox
            checked={block.properties?.checked ?? false}
            onCheckedChange={() => toggleTodo(block.id)}
            className="mt-1 shrink-0"
          />
          <div
            {...editableProps}
            className={cn(
              editableProps.className,
              "flex-1 min-h-[1.5em]",
              block.properties?.checked && "line-through text-muted-foreground"
            )}
          >
            {block.content}
          </div>
        </div>
      )

    case "quote":
      return (
        <div className="border-l-[3px] border-foreground/20 pl-4 py-0.5">
          <div
            {...editableProps}
            className={cn(
              editableProps.className,
              "text-base italic text-muted-foreground min-h-[1.5em]"
            )}
          >
            {block.content}
          </div>
        </div>
      )

    case "divider":
      return (
        <div
          className="py-3 cursor-pointer"
          onClick={handleFocus}
          onKeyDown={(e) => {
            if (e.key === "Enter") {
              e.preventDefault()
              addBlock("paragraph", block.id)
            }
            if (e.key === "Backspace") {
              e.preventDefault()
              deleteBlock(block.id)
            }
          }}
          tabIndex={0}
        >
          <hr className={cn(
            "border-border",
            isSelected && "border-primary"
          )} />
        </div>
      )

    case "code":
      return (
        <div className="rounded-lg bg-muted/50 border overflow-hidden my-1">
          <div className="flex items-center justify-between px-4 py-2 border-b bg-muted/30">
            <span className="text-xs text-muted-foreground font-mono">
              {block.properties?.language ?? "plain text"}
            </span>
          </div>
          <div
            ref={contentRef}
            contentEditable
            suppressContentEditableWarning
            onInput={handleInput}
            onKeyDown={(e) => {
              if (e.key === "Enter") {
                e.preventDefault()
                document.execCommand("insertLineBreak")
              }
              if (e.key === "Tab") {
                e.preventDefault()
                document.execCommand("insertText", false, "  ")
              }
            }}
            onFocus={handleFocus}
            className="p-4 font-mono text-sm leading-relaxed whitespace-pre-wrap outline-none min-h-[2.5em] overflow-x-auto"
          >
            {block.content}
          </div>
        </div>
      )

    case "callout":
      return (
        <div className="flex gap-3 rounded-lg bg-muted/50 border p-4 my-1">
          <span className="text-xl shrink-0 select-none">{block.properties?.emoji ?? "💡"}</span>
          <div
            {...editableProps}
            className={cn(editableProps.className, "flex-1 min-h-[1.5em]")}
          >
            {block.content}
          </div>
        </div>
      )

    case "toggle":
      return (
        <details className="group" open={block.properties?.expanded}>
          <summary className="flex gap-2 items-center cursor-pointer list-none">
            <span className="transition-transform group-open:rotate-90 text-muted-foreground">▶</span>
            <div
              {...editableProps}
              className={cn(editableProps.className, "flex-1 min-h-[1.5em]")}
            >
              {block.content}
            </div>
          </summary>
          <div className="pl-6 mt-1 text-muted-foreground text-sm">
            Toggle content goes here...
          </div>
        </details>
      )

    case "image":
      return (
        <div className="my-2 rounded-lg overflow-hidden border">
          {block.properties?.url ? (
            <img
              src={block.properties.url}
              alt={block.properties?.caption ?? ""}
              className="w-full object-cover max-h-96"
            />
          ) : (
            <div
              className="flex items-center justify-center h-48 bg-muted/50 text-muted-foreground cursor-pointer hover:bg-muted/70 transition-colors"
              onClick={handleFocus}
            >
              Click to add an image
            </div>
          )}
          {block.properties?.caption && (
            <p className="text-sm text-muted-foreground text-center py-2">
              {block.properties.caption}
            </p>
          )}
        </div>
      )

    default:
      return (
        <div
          {...editableProps}
          className={cn(editableProps.className, "min-h-[1.5em]")}
        >
          {block.content}
        </div>
      )
  }
}

function getPlaceholder(type: BlockType): string {
  switch (type) {
    case "heading_1":
      return "Heading 1"
    case "heading_2":
      return "Heading 2"
    case "heading_3":
      return "Heading 3"
    case "paragraph":
      return "Type '/' for commands..."
    case "bulleted_list":
      return "List item"
    case "numbered_list":
      return "List item"
    case "to_do":
      return "To-do"
    case "quote":
      return "Quote"
    case "code":
      return "Code"
    case "callout":
      return "Callout"
    case "toggle":
      return "Toggle"
    default:
      return "Type something..."
  }
}
