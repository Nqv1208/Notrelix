"use client"

import type { ComponentType, ReactNode } from "react"
import { Clock3, FileText, Layers3, Plus, Share2, Star } from "lucide-react"
import { ScrollArea } from "@/components/ui/scroll-area"
import { Button } from "@/components/ui/button"
import { Skeleton } from "@/components/ui/skeleton"
import type { DocsWorkspaceSnapshot } from "../../shared/types/snapshot.types"
import type { Page } from "../../pages/types/page.types"
import type { PageTreeNode } from "../types/tree.types"
import { PageTree } from "./page-tree"
import { NewPageButton } from "./new-page-button"
import { DocsSearch } from "./docs-search"

interface DocsSidebarProps {
  workspace: DocsWorkspaceSnapshot
  workspaceId: string
  pageTree: PageTreeNode[]
  favorites: Page[]
  recentPages: Page[]
  sharedPages: Page[]
  isLoading?: boolean
}

export function DocsSidebar({
  workspace,
  workspaceId,
  pageTree,
  favorites,
  recentPages,
  sharedPages,
  isLoading,
}: DocsSidebarProps) {
  return (
    <div className="flex h-svh flex-col bg-card">
      <div className="border-b border-border p-4">
        <div className="mb-4 flex items-center justify-between gap-3">
          <div className="flex min-w-0 items-center gap-2">
            <div className="flex size-9 shrink-0 items-center justify-center rounded-xl bg-[#02093a] text-sm font-semibold text-primary-foreground">
              {workspace.icon}
            </div>
            <div className="min-w-0">
              <p className="truncate text-sm font-semibold text-foreground">{workspace.name}</p>
              <p className="text-xs text-muted-foreground">Docs workspace</p>
            </div>
          </div>
          <NewPageButton workspaceId={workspace.id} compact />
        </div>
        <DocsSearch workspaceId={workspace.id} mode="inline" />
      </div>

      <ScrollArea className="min-h-0 flex-1">
        <div className="space-y-5 p-3">
          {isLoading ? (
            <SidebarSkeleton />
          ) : (
            <>
              <SidebarSection title="Favorites" icon={Star}>
                <CompactPageList pages={favorites} workspaceId={workspaceId} empty="No favorites yet" />
              </SidebarSection>
              <SidebarSection title="Recent" icon={Clock3}>
                <CompactPageList pages={recentPages} workspaceId={workspaceId} empty="No recent pages" />
              </SidebarSection>
              <SidebarSection title="Workspace tree" icon={Layers3} action={<Button variant="ghost" size="icon-xs"><Plus className="size-3" /></Button>}>
                {pageTree.length ? (
                  <PageTree tree={pageTree} workspaceId={workspaceId} density="compact" />
                ) : (
                  <p className="px-2 py-3 text-xs text-muted-foreground">No pages yet</p>
                )}
              </SidebarSection>
              <SidebarSection title="Shared" icon={Share2}>
                <CompactPageList pages={sharedPages} workspaceId={workspaceId} empty="No shared pages" />
              </SidebarSection>
            </>
          )}
        </div>
      </ScrollArea>

      <div className="border-t border-border p-3 text-xs text-muted-foreground">
        {workspace.pages.length} pages · {workspace.users.length} collaborators
      </div>
    </div>
  )
}

function SidebarSection({
  title,
  icon: Icon,
  action,
  children,
}: {
  title: string
  icon: ComponentType<{ className?: string }>
  action?: ReactNode
  children: ReactNode
}) {
  return (
    <section>
      <div className="mb-2 flex items-center justify-between px-2">
        <div className="flex items-center gap-2 text-[11px] font-semibold uppercase tracking-[0.08em] text-muted-foreground">
          <Icon className="size-3.5" />
          {title}
        </div>
        {action}
      </div>
      {children}
    </section>
  )
}

function CompactPageList({ pages, workspaceId, empty }: { pages: Page[]; workspaceId: string; empty: string }) {
  if (!pages.length) return <p className="px-2 py-2 text-xs text-muted-foreground">{empty}</p>

  return (
    <div className="space-y-0.5">
      {pages.map((page) => (
        <a
          key={page.id}
          href={`/${workspaceId}/docs/${page.id}`}
          className="flex items-center gap-2 rounded-lg px-2 py-1.5 text-sm text-muted-foreground transition hover:bg-muted hover:text-foreground"
        >
          <span className="w-5 text-center text-xs">{page.icon ?? <FileText className="size-3.5" />}</span>
          <span className="min-w-0 flex-1 truncate">{page.title}</span>
        </a>
      ))}
    </div>
  )
}

function SidebarSkeleton() {
  return (
    <div className="space-y-4">
      {Array.from({ length: 4 }).map((_, index) => (
        <div key={index} className="space-y-2">
          <Skeleton className="h-4 w-24" />
          <Skeleton className="h-8 w-full" />
          <Skeleton className="h-8 w-10/12" />
        </div>
      ))}
    </div>
  )
}
