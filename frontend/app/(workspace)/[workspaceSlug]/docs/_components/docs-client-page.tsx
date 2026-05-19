"use client"

import { useMemo } from "react"
import { FileText, FolderOpen, Sparkles } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Skeleton } from "@/components/ui/skeleton"
import { useDocsOverview, usePageList, usePageTree } from "@/features/docs/hooks/use-page-tree"
import { useFavorites } from "@/features/docs/hooks/use-favorites"
import { mockDocsWorkspace } from "@/features/docs/mock/mock-data"
import { PageTree } from "./page-tree"
import { DocsToolbar } from "./docs-toolbar"
import { DocsOverview } from "./docs-overview"
import { TemplatesSection } from "./templates-section"
import { RecentPages } from "./recent-pages"
import { FavoritesSection } from "./favorites-section"
import { DocsSearch } from "./docs-search"

interface DocsClientPageProps {
  workspaceId: string
  workspaceSlug: string
}

export function DocsClientPage({ workspaceId, workspaceSlug }: DocsClientPageProps) {
  const { data: tree = [], isLoading: treeLoading } = usePageTree(workspaceId)
  const { data: pageList = [], isLoading: listLoading } = usePageList(workspaceId)
  const { data: favorites = [] } = useFavorites(workspaceId)
  const { overview } = useDocsOverview(workspaceId)

  const recentPages = useMemo(() => pageList.slice(0, 6), [pageList])
  const isLoading = treeLoading || listLoading

  return (
    <main className="mx-auto max-w-[1380px] px-4 py-5 sm:px-6 lg:px-8">
      <section className="mb-5 overflow-hidden rounded-2xl border border-border bg-card p-5 shadow-[rgba(205,208,223,0.25)_0px_2px_28px]">
        <div className="flex flex-col gap-4 lg:flex-row lg:items-end lg:justify-between">
          <div>
            <div className="mb-3 inline-flex items-center gap-2 rounded-full border border-border bg-muted px-3 py-1 text-xs font-medium text-muted-foreground">
              <Sparkles className="size-3.5 text-primary" />
              Workspace docs hub
            </div>
            <h1 className="text-2xl font-semibold tracking-[-0.015em] text-foreground sm:text-3xl">Docs</h1>
            <p className="mt-2 max-w-2xl text-sm leading-6 text-muted-foreground">
              Workdocs, specs, meeting notes, and decisions live beside boards in the same workspace.
            </p>
          </div>
          <div className="flex flex-wrap gap-2">
            <DocsSearch workspaceId={workspaceId} workspaceSlug={workspaceSlug} mode="button" />
            <DocsToolbar workspaceId={workspaceId} workspaceSlug={workspaceSlug} />
          </div>
        </div>
      </section>

      {isLoading ? (
        <DocsDashboardSkeleton />
      ) : (
        <div className="grid gap-5 xl:grid-cols-[minmax(0,1fr)_360px]">
          <section className="space-y-5">
            <DocsOverview overview={overview} pages={pageList} workspaceSlug={workspaceSlug} />
            <div className="rounded-2xl border border-border bg-card p-5 shadow-[rgba(205,208,223,0.2)_0px_2px_22px]">
              <div className="mb-4 flex items-center justify-between">
                <div className="flex items-center gap-2">
                  <FolderOpen className="size-4 text-primary" />
                  <h2 className="text-sm font-semibold text-foreground">Page tree</h2>
                </div>
                <Button variant="outline" size="sm" className="bg-card">Reorder</Button>
              </div>
              {tree.length ? (
                <PageTree tree={tree} workspaceSlug={workspaceSlug} />
              ) : (
                <div className="rounded-xl border border-dashed border-border p-8 text-center">
                  <FileText className="mx-auto mb-2 size-8 text-muted-foreground" />
                  <p className="text-sm font-medium text-foreground">No docs yet</p>
                  <p className="text-sm text-muted-foreground">Create a doc to start building shared context.</p>
                </div>
              )}
            </div>
            <TemplatesSection
              templates={mockDocsWorkspace.templates}
              workspaceId={workspaceId}
              workspaceSlug={workspaceSlug}
            />
          </section>
          <aside className="space-y-5">
            <FavoritesSection pages={favorites} workspaceSlug={workspaceSlug} />
            <RecentPages pages={recentPages} workspaceSlug={workspaceSlug} />
          </aside>
        </div>
      )}
    </main>
  )
}

function DocsDashboardSkeleton() {
  return (
    <div className="grid gap-5 xl:grid-cols-[minmax(0,1fr)_360px]">
      <div className="space-y-5">
        <Skeleton className="h-52 rounded-2xl" />
        <Skeleton className="h-80 rounded-2xl" />
      </div>
      <div className="space-y-5">
        <Skeleton className="h-60 rounded-2xl" />
        <Skeleton className="h-60 rounded-2xl" />
      </div>
    </div>
  )
}
