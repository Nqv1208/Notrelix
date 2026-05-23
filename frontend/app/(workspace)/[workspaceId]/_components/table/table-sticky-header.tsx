"use client"

import type { ReactNode } from "react"

export function TableStickyHeader({ children }: { children: ReactNode }) {
  return (
    <div data-slot="sticky-header" className="sticky top-0 z-20 overflow-hidden border-b border-border bg-card rounded-t-sm">
      {children}
    </div>
  )
}
