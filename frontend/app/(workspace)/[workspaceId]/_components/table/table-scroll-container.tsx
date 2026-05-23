"use client"

import type { ReactNode } from "react"

export function TableScrollContainer({ children }: { children: ReactNode }) {
  return (
    <div data-slot="table-frame" className="h-full min-h-0 overflow-hidden bg-background px-6 pb-6 pt-2">
      <div data-slot="table-scroll-viewport" className="h-full min-h-0 overflow-auto bg-background rounded-t-sm">
        <div className="min-h-full min-w-full">{children}</div>
      </div>
    </div>
  )
}
