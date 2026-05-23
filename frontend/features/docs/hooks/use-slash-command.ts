"use client"

import { useMemo, useState } from "react"
import type { SlashCommandItem } from "../types"

export const slashCommandItems: SlashCommandItem[] = [
  { id: "text", type: "paragraph", label: "Text", description: "Plain paragraph block", keywords: ["paragraph", "text"] },
  { id: "h1", type: "heading_1", label: "Heading 1", description: "Large section heading", keywords: ["h1", "title"] },
  { id: "h2", type: "heading_2", label: "Heading 2", description: "Medium section heading", keywords: ["h2", "subtitle"] },
  { id: "h3", type: "heading_3", label: "Heading 3", description: "Small section heading", keywords: ["h3"] },
  { id: "todo", type: "todo", label: "Checklist", description: "Track a task inline", keywords: ["todo", "check", "task"] },
  { id: "bullet", type: "bulleted_list", label: "Bullet list", description: "Create a bulleted list item", keywords: ["list", "bullet"] },
  { id: "number", type: "numbered_list", label: "Numbered list", description: "Create a numbered list item", keywords: ["list", "number"] },
  { id: "quote", type: "quote", label: "Quote", description: "Highlight a note or decision", keywords: ["quote"] },
  { id: "code", type: "code", label: "Code", description: "Capture a code or API snippet", keywords: ["code", "snippet"] },
  { id: "divider", type: "divider", label: "Divider", description: "Separate document sections", keywords: ["divider", "line"] },
  { id: "callout", type: "callout", label: "Callout", description: "Emphasize context", keywords: ["callout", "note"] },
]

export function useSlashCommand() {
  const [open, setOpen] = useState(false)
  const [query, setQuery] = useState("")

  const items = useMemo(() => {
    const normalized = query.trim().toLowerCase()
    if (!normalized) return slashCommandItems
    return slashCommandItems.filter((item) =>
      [item.label, item.description, ...item.keywords].some((value) => value.toLowerCase().includes(normalized))
    )
  }, [query])

  return {
    open,
    setOpen,
    query,
    setQuery,
    items,
    reset: () => {
      setOpen(false)
      setQuery("")
    },
  }
}
