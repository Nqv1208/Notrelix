"use client"

import Link from "next/link"
import { Star } from "lucide-react"
import { Empty, EmptyDescription, EmptyHeader, EmptyTitle } from "@/components/ui/empty"
import type { Page } from "@/features/docs/types"

interface FavoritesSectionProps {
  pages: Page[]
  workspaceId: string
}

export function FavoritesSection({ pages, workspaceId }: FavoritesSectionProps) {
  return (
    <section className="rounded-2xl border border-border bg-card p-5 shadow-[rgba(205,208,223,0.2)_0px_2px_22px]">
      <div className="mb-4 flex items-center gap-2">
        <Star className="size-4 fill-amber-500 text-amber-500" />
        <h2 className="text-sm font-semibold text-foreground">Favorites</h2>
      </div>
      {pages.length ? (
        <div className="space-y-2">
          {pages.slice(0, 5).map((page) => (
            <Link
              key={page.id}
              href={`/${workspaceId}/docs/${page.id}`}
              className="flex items-center gap-3 rounded-xl border border-transparent p-2 transition hover:border-border hover:bg-muted"
            >
              <span className="flex size-9 items-center justify-center rounded-lg text-sm" style={{ backgroundColor: page.coverColor }}>
                {page.icon}
              </span>
              <span className="min-w-0 flex-1">
                <span className="block truncate text-sm font-medium text-foreground">{page.title}</span>
                <span className="block truncate text-xs text-muted-foreground">{page.tags.join(" · ") || "Workspace page"}</span>
              </span>
            </Link>
          ))}
        </div>
      ) : (
        <Empty className="border border-dashed border-border p-8">
          <EmptyHeader>
            <EmptyTitle className="text-base">No favorites</EmptyTitle>
            <EmptyDescription>Star pages you use often and they will appear here.</EmptyDescription>
          </EmptyHeader>
        </Empty>
      )}
    </section>
  )
}
