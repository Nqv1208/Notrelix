"use client"

import { useEffect } from "react"
import Link from "next/link"
import { ArrowUpRight, Clock3, FileText, PanelRight } from "lucide-react"
import { Avatar, AvatarFallback } from "@/components/ui/avatar"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Sheet, SheetContent, SheetHeader, SheetTitle } from "@/components/ui/sheet"
import { Skeleton } from "@/components/ui/skeleton"
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs"
import { usePageComments } from "../../comments/hooks/queries/use-page-comments"
import { usePageHistory } from "../../pages/hooks/queries/use-page-history"
import { mockDocsWorkspace } from "../../shared/mock/mock-data"
import { isMockModeEnabled } from "@/lib/config/mock-mode"
import { useDocsEditorStore } from "../store/editor-store"
import type { PageActivity, PageDetail } from "../../pages/types/page.types"
import type { PageComment } from "../../comments/types/comment.types"
import type { Block } from "../../blocks/types/block.types"
import { cn } from "@/lib/utils"
import { DocBlockRenderer } from "../../blocks/components/block-renderer"
import { DocumentToolbar } from "./document-toolbar"
import { EditablePageTitle } from "../../pages/components/editable-page-title"
import { FloatingFormatToolbar } from "./floating-format-toolbar"

export interface DocumentEditorProps {
  pageId: string
  workspaceId: string
  detail: PageDetail
  pageBlocks: Block[]
  embedded?: boolean
  showToolbar?: boolean
  showOpenFullDoc?: boolean
}

export function DocumentEditor({
  pageId,
  workspaceId,
  detail,
  pageBlocks,
  embedded,
  showToolbar = true,
  showOpenFullDoc,
}: DocumentEditorProps) {
  const setActivePageId = useDocsEditorStore((state) => state.setActivePageId)
  const commentsOpen = useDocsEditorStore((state) => state.commentsOpen)
  const setCommentsOpen = useDocsEditorStore((state) => state.setCommentsOpen)
  const contained = embedded || !showToolbar

  useEffect(() => {
    setActivePageId(pageId)
  }, [pageId, setActivePageId])

  return (
    <div className={cn("min-h-0 bg-card text-foreground", contained ? "h-full" : "min-h-svh")}>
      {showToolbar ? <DocumentToolbar pageId={pageId} blocks={pageBlocks} /> : null}
      <div className={cn("flex min-h-0", contained ? "h-full" : "min-h-[calc(100svh-49px)]")}>
        <main className="min-w-0 flex-1 overflow-auto">
          <div className="mx-auto max-w-[880px] px-4 pb-24 pt-5 sm:px-8">
            <div className="mb-4 flex flex-wrap items-center justify-between gap-3">
              <div className="flex min-w-0 items-center gap-2 text-xs text-muted-foreground">
                <FileText className="size-4 text-primary" />
                {detail.breadcrumb.map((item: { id: string; title: string }, index: number) => (
                  <span key={item.id} className="flex min-w-0 items-center gap-2">
                    {index > 0 ? <span>/</span> : null}
                    <span className="truncate">{item.title}</span>
                  </span>
                ))}
              </div>
              {showOpenFullDoc ? (
                <Button asChild variant="outline" size="sm" className="bg-card">
                  <Link href={`/${workspaceId}/docs/${pageId}` as never}>
                    Open full doc
                    <ArrowUpRight className="size-4" />
                  </Link>
                </Button>
              ) : null}
            </div>

            <section className="rounded-2xl border border-border bg-card shadow-sm">
              {detail.coverColor ? <div className="h-20 rounded-t-2xl border-b border-border bg-muted" /> : null}
              <div className="px-5 py-5 sm:px-8 sm:py-7">
                <div className="mb-4 flex items-center gap-3">
                  <Button variant="ghost" size="icon-lg" className="text-xl" aria-label="Page icon">
                    {detail.icon ?? "□"}
                  </Button>
                  <div className="min-w-0 flex-1">
                    <EditablePageTitle page={detail} />
                    <div className="mt-2 flex flex-wrap items-center gap-2 text-xs text-muted-foreground">
                      <Badge variant="secondary" className="rounded-full">{detail.status}</Badge>
                      <span className="inline-flex items-center gap-1"><Clock3 className="size-3.5" /> Edited {new Date(detail.lastEditedAt).toLocaleDateString()}</span>
                      <span>{detail.linkedBoards.length} linked boards</span>
                      <span>{detail.metadata.aiSummaryStatus} AI summary</span>
                    </div>
                  </div>
                </div>

                <div className="mb-6 flex items-center justify-between gap-3 rounded-xl border border-border bg-muted px-3 py-2">
                  <div className="flex items-center gap-2">
                    <div className="flex -space-x-2">
                      {detail.collaborators.slice(0, 4).map((user: { id: string; name: string; color?: string }) => (
                        <Avatar key={user.id} className="size-7 border-2 border-card">
                          <AvatarFallback className="text-[10px] text-primary-foreground" style={{ backgroundColor: user.color }}>
                            {user.name.split(" ").map((part: string) => part[0]).join("").slice(0, 2)}
                          </AvatarFallback>
                        </Avatar>
                      ))}
                    </div>
                    <span className="text-xs text-muted-foreground">{detail.collaborators.length} collaborators</span>
                  </div>
                  <Button variant="ghost" size="sm" className="rounded-full" onClick={() => setCommentsOpen(true)}>
                    <PanelRight className="size-4" />
                    Comments
                  </Button>
                </div>

                <DocBlockRenderer blocks={pageBlocks} pageId={pageId} />
              </div>
            </section>
          </div>
        </main>

        {!embedded ? (
          <aside className="hidden w-[340px] shrink-0 border-l border-border bg-card/70 p-3 lg:block">
            <CollaborationTabs pageId={pageId} />
          </aside>
        ) : null}
      </div>

      <Sheet open={commentsOpen} onOpenChange={setCommentsOpen}>
        <SheetContent side="right" className="w-[92vw] max-w-[380px]">
          <SheetHeader>
            <SheetTitle>Page collaboration</SheetTitle>
          </SheetHeader>
          <CollaborationTabs pageId={pageId} compact />
        </SheetContent>
      </Sheet>

      <FloatingFormatToolbar pageId={pageId} blocks={pageBlocks} />
    </div>
  )
}


function CollaborationTabs({ pageId, compact }: { pageId: string; compact?: boolean }) {
  const { data: comments = [], isLoading: isCommentsLoading } = usePageComments(pageId)
  const { data: activity = [], isLoading: isActivityLoading } = usePageHistory(pageId)

  if (isCommentsLoading || isActivityLoading) {
    return (
      <div className="space-y-3 p-2">
        <Skeleton className="h-10 rounded-xl" />
        <Skeleton className="h-10 rounded-xl" />
        <Skeleton className="h-10 rounded-xl" />
      </div>
    )
  }

  return (
    <Tabs defaultValue="comments" className={compact ? "mt-4" : "h-[calc(100svh-88px)]"}>
      <TabsList className="grid w-full grid-cols-2">
        <TabsTrigger value="comments">Comments</TabsTrigger>
        <TabsTrigger value="activity">Activity</TabsTrigger>
      </TabsList>
      <TabsContent value="comments" className="mt-3">
        <div className="space-y-3">
          {comments.map((comment: PageComment) => {
            const user = isMockModeEnabled("docs")
              ? mockDocsWorkspace.users.find((item) => item.id === comment.authorId)
              : null
            return (
              <div key={comment.id} className="rounded-xl border border-border bg-muted p-3">
                <p className="text-xs font-semibold text-foreground">{user?.name ?? "Teammate"}</p>
                <p className="mt-1 text-sm leading-6 text-muted-foreground">{comment.body}</p>
              </div>
            )
          })}
          {comments.length === 0 && (
            <p className="text-center text-xs text-muted-foreground py-4">No comments yet</p>
          )}
        </div>
      </TabsContent>
      <TabsContent value="activity" className="mt-3">
        <div className="space-y-3">
          {activity.map((item: PageActivity) => {
            const user = isMockModeEnabled("docs")
              ? mockDocsWorkspace.users.find((candidate) => candidate.id === item.actorId)
              : null
            return (
              <div key={item.id} className="rounded-xl border border-border bg-muted p-3">
                <p className="text-sm text-foreground"><span className="font-medium">{user?.name ?? "Teammate"}</span> {item.action} {item.targetLabel}</p>
                <p className="mt-1 text-xs text-muted-foreground">{new Date(item.createdAt).toLocaleString()}</p>
              </div>
            )
          })}
          {activity.length === 0 && (
            <p className="text-center text-xs text-muted-foreground py-4">No activity logged yet</p>
          )}
        </div>
      </TabsContent>
    </Tabs>
  )
}


export function EditorSkeleton({ embedded }: { embedded?: boolean }) {
  return (
    <div className={cn("mx-auto max-w-[880px] space-y-5 p-8", embedded && "max-w-full")}>
      <Skeleton className="h-12 w-2/3 rounded-xl" />
      <Skeleton className="h-32 rounded-2xl" />
      {Array.from({ length: 7 }).map((_, index) => (
        <Skeleton key={index} className="h-8 w-full rounded-lg" />
      ))}
    </div>
  )
}
