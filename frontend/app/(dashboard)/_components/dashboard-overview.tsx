"use client"

import Link from "next/link"
import { useRouter } from "next/navigation"
import {
  Plus,
  FileText,
  Clock,
  Star,
  ArrowRight,
  Sparkles,
  Layout,
  ListChecks,
  BookOpen,
  Import,
} from "lucide-react"
import { Button } from "@/components/ui/button"
import { routes } from "@/lib/routes"
import { useEditorStore } from "@/features/docs/store/editor-store"
import type { Page } from "@/features/docs/types/document.types"

const quickActions = [
  { icon: FileText, label: "Empty Page", description: "Start with a blank page" },
  { icon: ListChecks, label: "Task List", description: "Track your to-dos" },
  { icon: Layout, label: "Meeting Notes", description: "Structured meeting template" },
  { icon: BookOpen, label: "Wiki", description: "Team knowledge base" },
]

export function DashboardOverview() {
  const router = useRouter()
  const { workspaces, addPage, getFavoritePages } = useEditorStore()
  const favorites = getFavoritePages()

  const allPages: (Page & { wsName: string; wsIcon: string })[] = []
  for (const ws of workspaces) {
    const flatPages = flattenPages(ws.pages)
    for (const p of flatPages) {
      if (!p.isDeleted) {
        allPages.push({ ...p, wsName: ws.name, wsIcon: ws.icon })
      }
    }
  }

  const recentPages = allPages
    .sort((a, b) => new Date(b.updatedAt).getTime() - new Date(a.updatedAt).getTime())
    .slice(0, 8)

  const handleQuickCreate = () => {
    if (workspaces.length > 0) {
      const newId = addPage(workspaces[0].id)
      router.push(routes.workspace.page(workspaces[0].id, newId) as never)
    }
  }

  return (
    <div className="max-w-5xl mx-auto px-6 py-10">
      <div className="mb-10">
        <h1 className="text-3xl font-bold tracking-tight">Good morning 👋</h1>
        <p className="text-muted-foreground mt-1">
          Pick up where you left off or start something new.
        </p>
      </div>

      <section className="mb-10">
        <h2 className="text-sm font-semibold text-muted-foreground uppercase tracking-wider mb-4">
          Quick Start
        </h2>
        <div className="grid grid-cols-2 md:grid-cols-4 gap-3">
          {quickActions.map((action) => (
            <button
              key={action.label}
              onClick={handleQuickCreate}
              className="group flex flex-col items-start gap-3 rounded-xl border bg-card p-4 hover:border-primary/30 hover:shadow-sm transition-all text-left"
            >
              <div className="flex size-10 items-center justify-center rounded-lg bg-primary/5 group-hover:bg-primary/10 transition-colors">
                <action.icon className="size-5 text-primary" />
              </div>
              <div>
                <div className="text-sm font-medium">{action.label}</div>
                <div className="text-xs text-muted-foreground mt-0.5">
                  {action.description}
                </div>
              </div>
            </button>
          ))}
        </div>
      </section>

      {favorites.length > 0 && (
        <section className="mb-10">
          <div className="flex items-center justify-between mb-4">
            <h2 className="text-sm font-semibold text-muted-foreground uppercase tracking-wider flex items-center gap-2">
              <Star className="size-3.5 fill-yellow-400 text-yellow-400" />
              Favorites
            </h2>
          </div>
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-3">
            {favorites.map((page) => (
              <Link
                key={page.id}
                href={routes.workspace.page(page.workspaceId, page.id) as never}
                className="group flex items-center gap-3 rounded-xl border bg-card p-4 hover:border-primary/30 hover:shadow-sm transition-all"
              >
                <span className="text-2xl">{page.icon}</span>
                <div className="min-w-0">
                  <div className="text-sm font-medium truncate">{page.title}</div>
                  <div className="text-xs text-muted-foreground">
                    {workspaces.find((w) => w.id === page.workspaceId)?.name}
                  </div>
                </div>
              </Link>
            ))}
          </div>
        </section>
      )}

      <section>
        <div className="flex items-center justify-between mb-4">
          <h2 className="text-sm font-semibold text-muted-foreground uppercase tracking-wider flex items-center gap-2">
            <Clock className="size-3.5" />
            Recent Pages
          </h2>
        </div>
        <div className="rounded-xl border bg-card divide-y">
          {recentPages.map((page) => (
            <Link
              key={page.id}
              href={routes.workspace.page(page.workspaceId, page.id) as never}
              className="flex items-center gap-4 p-3 hover:bg-accent/50 transition-colors first:rounded-t-xl last:rounded-b-xl group"
            >
              <span className="text-xl shrink-0">{page.icon}</span>
              <div className="flex-1 min-w-0">
                <div className="text-sm font-medium truncate group-hover:text-primary transition-colors">
                  {page.title}
                </div>
                <div className="text-xs text-muted-foreground flex items-center gap-2">
                  <span>{page.wsIcon} {page.wsName}</span>
                  <span className="text-muted-foreground/50">·</span>
                  <span>Edited {formatRelativeTime(page.updatedAt)}</span>
                </div>
              </div>
              <ArrowRight className="size-4 text-muted-foreground opacity-0 group-hover:opacity-100 transition-opacity" />
            </Link>
          ))}
        </div>
      </section>

      {workspaces.map((ws) => (
        <section key={ws.id} className="mt-10">
          <div className="flex items-center justify-between mb-4">
            <h2 className="text-sm font-semibold text-muted-foreground uppercase tracking-wider flex items-center gap-2">
              <span>{ws.icon}</span>
              {ws.name}
            </h2>
            <Button
              variant="ghost"
              size="sm"
              className="gap-1.5 text-xs"
              onClick={() => {
                const newId = addPage(ws.id)
                router.push(routes.workspace.page(ws.id, newId) as never)
              }}
            >
              <Plus className="size-3.5" />
              New Page
            </Button>
          </div>
          <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-4 gap-3">
            {ws.pages
              .filter((p) => !p.isDeleted && !p.parentId)
              .map((page) => (
                <Link
                  key={page.id}
                  href={routes.workspace.page(ws.id, page.id) as never}
                  className="group rounded-xl border bg-card overflow-hidden hover:border-primary/30 hover:shadow-sm transition-all"
                >
                  <div className="h-20 bg-gradient-to-br from-muted/50 to-muted flex items-center justify-center">
                    <span className="text-3xl opacity-60 group-hover:opacity-100 group-hover:scale-110 transition-all">
                      {page.icon}
                    </span>
                  </div>
                  <div className="p-3">
                    <div className="text-sm font-medium truncate">{page.title}</div>
                    {page.children && page.children.length > 0 && (
                      <div className="text-xs text-muted-foreground mt-0.5">
                        {page.children.length} sub-page{page.children.length > 1 ? "s" : ""}
                      </div>
                    )}
                  </div>
                </Link>
              ))}
          </div>
        </section>
      ))}
    </div>
  )
}

function flattenPages(pages: Page[]): Page[] {
  const result: Page[] = []
  for (const page of pages) {
    result.push(page)
    if (page.children) {
      result.push(...flattenPages(page.children))
    }
  }
  return result
}

function formatRelativeTime(dateStr: string): string {
  const date = new Date(dateStr)
  const now = new Date()
  const diffMs = now.getTime() - date.getTime()
  const diffMin = Math.floor(diffMs / 60000)
  const diffHr = Math.floor(diffMin / 60)
  const diffDay = Math.floor(diffHr / 24)

  if (diffMin < 1) return "just now"
  if (diffMin < 60) return `${diffMin}m ago`
  if (diffHr < 24) return `${diffHr}h ago`
  if (diffDay < 7) return `${diffDay}d ago`
  return date.toLocaleDateString()
}
