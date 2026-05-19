"use client"

import Link from "next/link"
import { ArrowUpRight, FileText, PanelRightClose, Sparkles } from "lucide-react"
import { Button } from "@/components/ui/button"
import { ScrollArea } from "@/components/ui/scroll-area"
import { useBoardDocsPanel } from "@/features/boards/hooks"

export function DocsPanel({ workspaceSlug, pageId }: { workspaceSlug: string; pageId: string }) {
  const { closeDoc } = useBoardDocsPanel()

  return (
    <aside className="flex h-full min-h-[620px] flex-col overflow-hidden rounded-2xl border border-border bg-card shadow-sm">
      <div className="flex shrink-0 items-center justify-between gap-3 border-b border-border p-4">
        <div className="min-w-0">
          <p className="text-xs font-medium text-muted-foreground">Board doc</p>
          <h2 className="truncate text-sm font-semibold text-foreground">{pageId}</h2>
        </div>
        <Button variant="ghost" size="icon-sm" onClick={closeDoc} aria-label="Close docs panel">
          <PanelRightClose className="size-4" />
        </Button>
      </div>

      <ScrollArea className="min-h-0 flex-1">
        <div className="space-y-4 p-4">
          <div className="rounded-2xl border border-border bg-muted/40 p-4">
            <div className="mb-3 flex size-11 items-center justify-center rounded-xl bg-primary text-primary-foreground">
              <FileText className="size-5" />
            </div>
            <h3 className="text-lg font-semibold text-foreground">Linked project context</h3>
            <p className="mt-2 text-sm leading-6 text-muted-foreground">
              This panel is wired to URL state through <span className="font-medium text-foreground">?doc={pageId}</span>. The real Docs editor can be lazy-loaded here in the next pass.
            </p>
            <Button variant="outline" size="sm" className="mt-4 bg-card" asChild>
              <Link href={`/${workspaceSlug}/docs/${pageId}` as never}>
                Open full doc
                <ArrowUpRight className="size-4" />
              </Link>
            </Button>
          </div>

          <div className="rounded-2xl border border-border bg-card p-4">
            <div className="mb-3 flex items-center gap-2">
              <Sparkles className="size-4 text-primary" />
              <h3 className="text-sm font-semibold text-foreground">Doc handoff</h3>
            </div>
            <ul className="space-y-2 text-sm leading-6 text-muted-foreground">
              <li>Summarize board decisions into the linked page.</li>
              <li>Keep card checklists and doc action items aligned.</li>
              <li>Use browser back/forward to open or close the panel.</li>
            </ul>
          </div>
        </div>
      </ScrollArea>
    </aside>
  )
}
