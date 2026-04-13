"use client"

import * as React from "react"
import { cn } from "@/lib/utils"
import {
  Type,
  Heading1,
  Heading2,
  Heading3,
  List,
  ListOrdered,
  CheckSquare,
  Quote,
  Minus,
  Code,
  Lightbulb,
  Image,
  ChevronRight,
} from "lucide-react"
import type { BlockType } from "../types/document.types"

type SlashCommandItem = {
  type: BlockType
  label: string
  description: string
  icon: React.ReactNode
  group: string
}

const SLASH_COMMANDS: SlashCommandItem[] = [
  {
    type: "paragraph",
    label: "Text",
    description: "Plain text block",
    icon: <Type className="size-5" />,
    group: "Basic",
  },
  {
    type: "heading_1",
    label: "Heading 1",
    description: "Large section heading",
    icon: <Heading1 className="size-5" />,
    group: "Basic",
  },
  {
    type: "heading_2",
    label: "Heading 2",
    description: "Medium section heading",
    icon: <Heading2 className="size-5" />,
    group: "Basic",
  },
  {
    type: "heading_3",
    label: "Heading 3",
    description: "Small section heading",
    icon: <Heading3 className="size-5" />,
    group: "Basic",
  },
  {
    type: "bulleted_list",
    label: "Bulleted List",
    description: "Unordered list with bullets",
    icon: <List className="size-5" />,
    group: "Lists",
  },
  {
    type: "numbered_list",
    label: "Numbered List",
    description: "Ordered list with numbers",
    icon: <ListOrdered className="size-5" />,
    group: "Lists",
  },
  {
    type: "to_do",
    label: "To-do List",
    description: "Checklist with checkboxes",
    icon: <CheckSquare className="size-5" />,
    group: "Lists",
  },
  {
    type: "quote",
    label: "Quote",
    description: "Blockquote for citations",
    icon: <Quote className="size-5" />,
    group: "Advanced",
  },
  {
    type: "divider",
    label: "Divider",
    description: "Horizontal separator line",
    icon: <Minus className="size-5" />,
    group: "Advanced",
  },
  {
    type: "code",
    label: "Code",
    description: "Code block with syntax",
    icon: <Code className="size-5" />,
    group: "Advanced",
  },
  {
    type: "callout",
    label: "Callout",
    description: "Highlighted note or tip",
    icon: <Lightbulb className="size-5" />,
    group: "Advanced",
  },
  {
    type: "toggle",
    label: "Toggle",
    description: "Collapsible content block",
    icon: <ChevronRight className="size-5" />,
    group: "Advanced",
  },
  {
    type: "image",
    label: "Image",
    description: "Upload or embed an image",
    icon: <Image className="size-5" />,
    group: "Media",
  },
]

type SlashCommandMenuProps = {
  position: { top: number; left: number }
  query: string
  onSelect: (type: BlockType) => void
  onClose: () => void
}

export function SlashCommandMenu({
  position,
  query,
  onSelect,
  onClose,
}: SlashCommandMenuProps) {
  const [selectedIndex, setSelectedIndex] = React.useState(0)
  const menuRef = React.useRef<HTMLDivElement>(null)

  const filteredCommands = React.useMemo(() => {
    if (!query) return SLASH_COMMANDS
    const lower = query.toLowerCase()
    return SLASH_COMMANDS.filter(
      (cmd) =>
        cmd.label.toLowerCase().includes(lower) ||
        cmd.description.toLowerCase().includes(lower) ||
        cmd.type.toLowerCase().includes(lower)
    )
  }, [query])

  const groups = React.useMemo(() => {
    const map = new Map<string, SlashCommandItem[]>()
    for (const cmd of filteredCommands) {
      const group = map.get(cmd.group) ?? []
      group.push(cmd)
      map.set(cmd.group, group)
    }
    return map
  }, [filteredCommands])

  React.useEffect(() => {
    setSelectedIndex(0)
  }, [query])

  React.useEffect(() => {
    function handleKeyDown(e: KeyboardEvent) {
      if (e.key === "ArrowDown") {
        e.preventDefault()
        setSelectedIndex((i) => Math.min(i + 1, filteredCommands.length - 1))
      } else if (e.key === "ArrowUp") {
        e.preventDefault()
        setSelectedIndex((i) => Math.max(i - 1, 0))
      } else if (e.key === "Enter") {
        e.preventDefault()
        if (filteredCommands[selectedIndex]) {
          onSelect(filteredCommands[selectedIndex].type)
        }
      } else if (e.key === "Escape") {
        e.preventDefault()
        onClose()
      }
    }

    document.addEventListener("keydown", handleKeyDown)
    return () => document.removeEventListener("keydown", handleKeyDown)
  }, [selectedIndex, filteredCommands, onSelect, onClose])

  React.useEffect(() => {
    function handleClickOutside(e: MouseEvent) {
      if (menuRef.current && !menuRef.current.contains(e.target as Node)) {
        onClose()
      }
    }
    document.addEventListener("mousedown", handleClickOutside)
    return () => document.removeEventListener("mousedown", handleClickOutside)
  }, [onClose])

  if (filteredCommands.length === 0) {
    return (
      <div
        ref={menuRef}
        className="fixed z-50 w-72 rounded-lg border bg-popover shadow-xl p-3"
        style={{ top: position.top, left: position.left }}
      >
        <p className="text-sm text-muted-foreground text-center py-4">No results found</p>
      </div>
    )
  }

  let flatIndex = 0

  return (
    <div
      ref={menuRef}
      className="fixed z-50 w-72 max-h-80 overflow-y-auto rounded-lg border bg-popover shadow-xl py-1"
      style={{ top: position.top, left: position.left }}
    >
      <div className="px-3 py-2 text-xs font-medium text-muted-foreground">
        Insert block
      </div>
      {Array.from(groups.entries()).map(([group, items]) => (
        <div key={group}>
          <div className="px-3 py-1.5 text-[11px] font-semibold text-muted-foreground/70 uppercase tracking-wider">
            {group}
          </div>
          {items.map((item) => {
            const currentIndex = flatIndex++
            return (
              <button
                key={item.type}
                onClick={() => onSelect(item.type)}
                onMouseEnter={() => setSelectedIndex(currentIndex)}
                className={cn(
                  "w-full flex items-center gap-3 px-3 py-2 text-left transition-colors",
                  currentIndex === selectedIndex
                    ? "bg-accent text-accent-foreground"
                    : "hover:bg-accent/50"
                )}
              >
                <div className="flex size-9 items-center justify-center rounded-md border bg-background shrink-0">
                  {item.icon}
                </div>
                <div className="min-w-0">
                  <div className="text-sm font-medium truncate">{item.label}</div>
                  <div className="text-xs text-muted-foreground truncate">
                    {item.description}
                  </div>
                </div>
              </button>
            )
          })}
        </div>
      ))}
    </div>
  )
}
