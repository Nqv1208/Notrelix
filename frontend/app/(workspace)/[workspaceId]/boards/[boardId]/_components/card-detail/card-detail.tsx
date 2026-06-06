"use client"

import Link from "next/link"
import { AlertCircle, ArrowLeft, CalendarDays, FileText, MessageSquareText } from "lucide-react"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Skeleton } from "@/components/ui/skeleton"
import { Textarea } from "@/components/ui/textarea"
import { useCard } from "@/features/boards/hooks"
import { cn } from "@/lib/utils"
import { CardActivity } from "./card-activity"
import { CardChecklists } from "./card-checklists"
import { CardComments } from "./card-comments"
import { CardLinkedPagePreview } from "./card-linked-page-preview"
import { CardSidebar } from "./card-sidebar"

export function CardDetail({
  workspaceId,
  boardId,
  cardId,
  mode,
}: {
  workspaceId: string
  boardId: string
  cardId: string
  mode: "page" | "modal"
}) {
  const { card, isLoading, error } = useCard(cardId)

  if (isLoading) return <CardDetailSkeleton mode={mode} />

  if (error || !card) {
    return (
      <main className={cn("p-6", mode === "page" && "mx-auto max-w-[1180px]")}>
        <div className="rounded-2xl border border-border bg-card p-8 text-center">
          <AlertCircle className="mx-auto mb-3 size-8 text-destructive" />
          <h1 className="text-lg font-semibold text-foreground">Card unavailable</h1>
          <p className="mt-2 text-sm text-muted-foreground">This card may have been archived, deleted, or moved.</p>
        </div>
      </main>
    )
  }

  return (
    <main className={cn(mode === "page" ? "mx-auto max-w-[1180px] px-4 py-6 sm:px-6 lg:px-8" : "h-[92vh] overflow-auto p-5")}>
      <div className="mb-5 flex items-center justify-between gap-3 rounded-2xl border border-border bg-card p-3">
        <div className="flex min-w-0 items-center gap-2">
          <Button variant="ghost" size="icon-sm" asChild>
            <Link href={`/${workspaceId}/boards/${boardId}` as never} aria-label="Back to board">
              <ArrowLeft className="size-4" />
            </Link>
          </Button>
          <div className="min-w-0">
            <p className="text-xs text-muted-foreground">Board card</p>
            <h1 className="truncate text-base font-semibold text-foreground">{card.title}</h1>
          </div>
        </div>
        <Badge variant="secondary" className="rounded-full">
          {card.isArchived ? "Archived" : "Active"}
        </Badge>
      </div>

      <div className="grid gap-5 lg:grid-cols-[minmax(0,1fr)_320px]">
        <section className="space-y-5">
          <div className="rounded-2xl border border-border bg-card p-5">
            <div className="mb-4 flex flex-wrap items-center gap-2">
              {card.priority ? <Badge className="rounded-full">{card.priority}</Badge> : null}
              {card.dueDate ? (
                <Badge variant="secondary" className="rounded-full">
                  <CalendarDays className="size-3.5" />
                  {new Date(card.dueDate).toLocaleDateString()}
                </Badge>
              ) : null}
              <Badge variant="secondary" className="rounded-full">
                <MessageSquareText className="size-3.5" />
                {card._count.comments} comments
              </Badge>
            </div>

            <label htmlFor="card-description-textarea" className="text-xs font-semibold uppercase tracking-[0.08em] text-muted-foreground">Description</label>
            <Textarea id="card-description-textarea" className="mt-2 min-h-32 bg-muted/40" defaultValue={card.descriptionMd ?? ""} aria-label="Card description" />
          </div>

          {card.linkedPageId ? <CardLinkedPagePreview workspaceId={workspaceId} boardId={boardId} pageId={card.linkedPageId} /> : (
            <div className="rounded-2xl border border-dashed border-border bg-card p-5 text-sm text-muted-foreground">
              <FileText className="mb-2 size-5 text-primary" />
              No linked doc yet.
            </div>
          )}

          <CardChecklists card={card} />
          <CardComments cardId={card.id} />
          <CardActivity cardId={card.id} />
        </section>

        <CardSidebar card={card} workspaceId={workspaceId} boardId={boardId} />
      </div>
    </main>
  )
}

function CardDetailSkeleton({ mode }: { mode: "page" | "modal" }) {
  return (
    <main className={cn(mode === "page" ? "mx-auto max-w-[1180px] px-4 py-6 sm:px-6 lg:px-8" : "p-5")}>
      <Skeleton className="mb-5 h-16 rounded-2xl" />
      <div className="grid gap-5 lg:grid-cols-[minmax(0,1fr)_320px]">
        <Skeleton className="h-[560px] rounded-2xl" />
        <Skeleton className="h-[560px] rounded-2xl" />
      </div>
    </main>
  )
}
