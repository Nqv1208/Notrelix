"use client"

import { Badge } from "@/components/ui/badge"
import type { PageDetail } from "@/features/docs/types"

export function PageProperties({ page }: { page: PageDetail }) {
  return (
    <div className="mb-6 flex flex-wrap items-center gap-2 border-y border-border py-3">
      <Badge variant="secondary" className="rounded-full bg-muted text-muted-foreground">
        {page.status}
      </Badge>
      {page.tags.map((tag) => (
        <Badge key={tag} variant="outline" className="rounded-full">
          {tag}
        </Badge>
      ))}
      {page.isShared ? <Badge className="rounded-full bg-accent text-primary hover:bg-accent">Shared</Badge> : null}
    </div>
  )
}
