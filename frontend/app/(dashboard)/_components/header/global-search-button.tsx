"use client"

import { Search } from "lucide-react"

interface GlobalSearchButtonProps {
  onOpen: () => void
}

export function GlobalSearchButton({ onOpen }: GlobalSearchButtonProps) {
  const modifierKey = "⌘"

  return (
    <button
      onClick={onOpen}
      className="flex h-9 w-full max-w-[400px] items-center justify-between rounded-lg border border-border bg-muted px-3 text-muted-foreground transition-colors hover:bg-accent hover:text-accent-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
      style={{ fontFamily: "var(--font-body)" }}
      aria-label="Search across workspace"
    >
      <div className="flex items-center gap-2">
        <Search size={14} />
        <span className="text-[14px]">Search...</span>
      </div>
      <kbd
        className="pointer-events-none hidden h-5 select-none items-center gap-1 rounded border border-border bg-card px-1.5 text-[10px] font-medium text-muted-foreground opacity-100 sm:flex"
        style={{ fontFamily: "var(--font-display)" }}
      >
        <span className="text-xs">{modifierKey}</span>K
      </kbd>
    </button>
  )
}
