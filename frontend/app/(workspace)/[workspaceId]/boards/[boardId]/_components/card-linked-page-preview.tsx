import Link from "next/link"
import { ArrowUpRight, FileText } from "lucide-react"
import { Button } from "@/components/ui/button"

export function CardLinkedPagePreview({ workspaceId, boardId, pageId }: { workspaceId: string; boardId: string; pageId: string }) {
  return (
    <section className="rounded-2xl border border-border bg-card p-5">
      <div className="mb-3 flex items-center gap-2">
        <FileText className="size-4 text-primary" />
        <h2 className="text-sm font-semibold text-foreground">Linked doc</h2>
      </div>
      <div className="rounded-xl border border-border bg-muted/40 p-4">
        <p className="text-sm font-medium text-foreground">{pageId}</p>
        <p className="mt-1 text-sm leading-6 text-muted-foreground">Open this page in the board docs panel to keep task execution and project context together.</p>
        <Button variant="outline" size="sm" className="mt-3 bg-card" asChild>
          <Link href={`/${workspaceId}/boards/${boardId}?doc=${pageId}` as never}>
            Open in docs panel
            <ArrowUpRight className="size-4" />
          </Link>
        </Button>
      </div>
    </section>
  )
}
