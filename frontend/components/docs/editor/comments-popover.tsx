"use client"

import { MessageSquareText } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Popover, PopoverContent, PopoverTrigger } from "@/components/ui/popover"
import { ScrollArea } from "@/components/ui/scroll-area"
import { Textarea } from "@/components/ui/textarea"
import { useCreatePageComment, usePageComments } from "@/features/docs/hooks/use-page-comments"
import { mockDocsWorkspace } from "@/features/docs/mock/mock-data"

export function CommentsPopover({ pageId, blockId }: { pageId: string; blockId?: string }) {
  const comments = usePageComments(pageId)
  const createComment = useCreatePageComment(pageId)
  const visibleComments = (comments.data ?? []).filter((comment) => !blockId || comment.blockId === blockId)

  return (
    <Popover>
      <PopoverTrigger asChild>
        <Button variant="ghost" size="sm" className="rounded-full">
          <MessageSquareText className="size-4" />
          Comments
        </Button>
      </PopoverTrigger>
      <PopoverContent align="end" className="w-80 p-0">
        <div className="border-b border-border px-4 py-3">
          <p className="text-sm font-semibold text-foreground">Comments</p>
          <p className="text-xs text-muted-foreground">{visibleComments.length} threads in this context</p>
        </div>
        <ScrollArea className="max-h-72">
          <div className="space-y-3 p-3">
            {visibleComments.length ? (
              visibleComments.map((comment) => {
                const user = mockDocsWorkspace.users.find((item) => item.id === comment.authorId)
                return (
                  <div key={comment.id} className="rounded-xl border border-border bg-muted p-3">
                    <p className="text-xs font-semibold text-foreground">{user?.name ?? "Teammate"}</p>
                    <p className="mt-1 text-sm leading-6 text-muted-foreground">{comment.body}</p>
                  </div>
                )
              })
            ) : (
              <p className="rounded-xl border border-dashed border-border p-4 text-center text-sm text-muted-foreground">
                No comments yet.
              </p>
            )}
          </div>
        </ScrollArea>
        <form
          className="border-t border-border p-3"
          onSubmit={(event) => {
            event.preventDefault()
            const formData = new FormData(event.currentTarget)
            const body = String(formData.get("comment") ?? "").trim()
            if (!body) return
            createComment.mutate({ body, blockId: blockId ?? null })
            event.currentTarget.reset()
          }}
        >
          <Textarea name="comment" className="min-h-20 resize-none bg-card" placeholder="Add a comment..." />
          <Button className="mt-2 w-full rounded-full" size="sm" disabled={createComment.isPending}>
            Add comment
          </Button>
        </form>
      </PopoverContent>
    </Popover>
  )
}
