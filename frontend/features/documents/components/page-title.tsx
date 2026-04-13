"use client"

import * as React from "react"
import { cn } from "@/lib/utils"
import { Smile } from "lucide-react"
import { useEditorStore } from "../store/editor-store"

type PageTitleProps = {
  pageId: string
}

const PAGE_ICONS = ["📄", "📝", "📒", "📓", "📔", "📕", "📗", "📘", "📙", "🗒️", "🚀", "🎯", "🎨", "💡", "🔥", "⭐", "🏠", "📅", "👥", "🗺️", "📚", "✅", "🎵", "🖼️", "📣", "🏃", "💻", "🔧", "🧪", "📊"]

export function PageTitle({ pageId }: PageTitleProps) {
  const { getPage, updatePage, addBlock, blocks } = useEditorStore()
  const page = getPage(pageId)
  const inputRef = React.useRef<HTMLDivElement>(null)
  const [showIconPicker, setShowIconPicker] = React.useState(false)

  if (!page) return null

  const handleInput = (e: React.FormEvent<HTMLDivElement>) => {
    const text = e.currentTarget.textContent ?? ""
    updatePage(pageId, { title: text })
  }

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === "Enter") {
      e.preventDefault()
      if (blocks.length > 0) {
        const { setFocusedBlock } = useEditorStore.getState()
        setFocusedBlock(blocks[0].id)
      } else {
        addBlock("paragraph")
      }
    }
  }

  return (
    <div className="mb-4">
      <div className="group/icon relative inline-block mb-2">
        <button
          onClick={() => setShowIconPicker(!showIconPicker)}
          className="text-5xl hover:bg-accent rounded-lg p-1 transition-colors"
        >
          {page.icon}
        </button>
        <button
          onClick={() => setShowIconPicker(!showIconPicker)}
          className="absolute -bottom-1 -right-1 opacity-0 group-hover/icon:opacity-100 transition-opacity bg-background border rounded-md p-0.5 shadow-sm"
        >
          <Smile className="size-3.5 text-muted-foreground" />
        </button>

        {showIconPicker && (
          <div className="absolute top-full left-0 mt-1 z-50 bg-popover border rounded-lg shadow-xl p-3 w-72">
            <div className="text-xs font-medium text-muted-foreground mb-2">Choose an icon</div>
            <div className="grid grid-cols-8 gap-1">
              {PAGE_ICONS.map((icon) => (
                <button
                  key={icon}
                  onClick={() => {
                    updatePage(pageId, { icon })
                    setShowIconPicker(false)
                  }}
                  className={cn(
                    "size-8 flex items-center justify-center rounded hover:bg-accent text-lg transition-colors",
                    page.icon === icon && "bg-accent ring-1 ring-primary"
                  )}
                >
                  {icon}
                </button>
              ))}
            </div>
          </div>
        )}
      </div>

      <div
        ref={inputRef}
        contentEditable
        suppressContentEditableWarning
        onInput={handleInput}
        onKeyDown={handleKeyDown}
        data-placeholder="Untitled"
        className="text-4xl font-bold tracking-tight outline-none empty:before:content-[attr(data-placeholder)] empty:before:text-muted-foreground/30"
      >
        {page.title}
      </div>
    </div>
  )
}
