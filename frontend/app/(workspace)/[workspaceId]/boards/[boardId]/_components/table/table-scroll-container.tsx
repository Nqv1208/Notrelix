"use client"

import type { ReactNode } from "react"

export function TableScrollContainer({ children }: { children: ReactNode }) {
  return (
    <div data-slot="table-frame" className="h-full min-h-0 overflow-hidden bg-background px-4 pb-4 pt-2">
      <div data-slot="table-scroll-viewport" className="h-full min-h-0 overflow-auto rounded-sm bg-table-bg shadow-sm ring-1 ring-border/50">
        <div className="min-h-full min-w-full">{children}</div>
      </div>
    </div>
  )
}
