"use client"

import Link from "next/link"
import { Clock3 } from "lucide-react"
import type { Page } from "@/features/docs/types"

interface RecentPagesProps {
  pages: Page[]
  workspaceId: string
}

export function RecentPages({ pages, workspaceId }: RecentPagesProps) {
  return (
    <section className="rounded-2xl border border-border bg-card p-5 shadow-[rgba(205,208,223,0.2)_0px_2px_22px]">
      <div className="mb-4 flex items-center gap-2">
        <Clock3 className="size-4 text-primary" />
        <h2 className="text-sm font-semibold text-foreground">Recent pages</h2>
      </div>
      <div className="space-y-1">
        {pages.map((page) => (
          <Link
            key={page.id}
            href={`/${workspaceId}/docs/${page.id}`}
            className="flex items-center gap-3 rounded-lg px-2 py-2 text-sm transition hover:bg-muted"
          >
            <span className="w-6 text-center text-xs">{page.icon}</span>
            <span className="min-w-0 flex-1 truncate text-foreground">{page.title}</span>
            <span className="hidden text-xs text-muted-foreground sm:inline">{new Date(page.lastEditedAt).toLocaleDateString()}</span>
          </Link>
        ))}
      </div>
    </section>
  )
}
