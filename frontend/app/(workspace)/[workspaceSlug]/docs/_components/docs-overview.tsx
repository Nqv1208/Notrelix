"use client"

import type { ComponentType } from "react"
import Link from "next/link"
import { ArrowUpRight, FileText, Globe2, MessageSquareText, Workflow } from "lucide-react"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import type { Page } from "@/features/docs/types"

interface DocsOverviewProps {
  overview: { total: number; published: number; shared: number; review: number }
  pages: Page[]
  workspaceSlug: string
}

export function DocsOverview({ overview, pages, workspaceSlug }: DocsOverviewProps) {
  const heroPages = pages.slice(0, 4)

  return (
    <section className="rounded-2xl border border-border bg-card p-5 shadow-[rgba(205,208,223,0.28)_0px_2px_28px]">
      <div className="mb-5 grid gap-3 sm:grid-cols-4">
        <Metric icon={FileText} label="Pages" value={overview.total} />
        <Metric icon={Globe2} label="Published" value={overview.published} />
        <Metric icon={Workflow} label="Review" value={overview.review} />
        <Metric icon={MessageSquareText} label="Shared" value={overview.shared} />
      </div>
      <div className="grid gap-3 md:grid-cols-2">
        {heroPages.map((page) => (
          <Link
            key={page.id}
            href={`/${workspaceSlug}/docs/${page.id}`}
            className="group rounded-xl border border-border bg-muted p-4 transition hover:-translate-y-0.5 hover:bg-card hover:shadow-[rgba(205,208,223,0.35)_0px_2px_24px]"
          >
            <div className="mb-4 flex items-start justify-between gap-3">
              <div className="flex size-10 items-center justify-center rounded-xl text-lg" style={{ backgroundColor: page.coverColor }}>
                {page.icon}
              </div>
              <Button variant="ghost" size="icon-xs" className="opacity-0 transition group-hover:opacity-100">
                <ArrowUpRight className="size-3.5" />
              </Button>
            </div>
            <h3 className="line-clamp-1 text-sm font-semibold text-foreground">{page.title}</h3>
            <div className="mt-3 flex flex-wrap gap-1.5">
              {page.tags.slice(0, 2).map((tag) => (
                <Badge key={tag} variant="secondary" className="rounded-full bg-card text-[11px] text-muted-foreground">
                  {tag}
                </Badge>
              ))}
            </div>
          </Link>
        ))}
      </div>
    </section>
  )
}

function Metric({ icon: Icon, label, value }: { icon: ComponentType<{ className?: string }>; label: string; value: number }) {
  return (
    <div className="rounded-xl border border-border bg-muted p-3">
      <div className="mb-2 flex items-center gap-2 text-xs font-medium text-muted-foreground">
        <Icon className="size-3.5 text-primary" />
        {label}
      </div>
      <p className="text-2xl font-semibold tracking-[-0.015em] text-foreground">{value}</p>
    </div>
  )
}
