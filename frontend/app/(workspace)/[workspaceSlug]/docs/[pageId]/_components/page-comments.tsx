"use client"

import { useState } from "react"
import { MessageSquareText, Send } from "lucide-react"
import { Avatar, AvatarFallback } from "@/components/ui/avatar"
import { Button } from "@/components/ui/button"
import { ScrollArea } from "@/components/ui/scroll-area"
import { Textarea } from "@/components/ui/textarea"
import { Empty, EmptyDescription, EmptyHeader, EmptyTitle } from "@/components/ui/empty"
import { useCreatePageComment, usePageComments } from "@/features/docs/hooks/use-page-comments"
import type { DocsUser } from "@/features/docs/types"

export function PageComments({ pageId, users }: { pageId: string; users: DocsUser[] }) {
  const [body, setBody] = useState("")
  const { data: comments = [], isLoading } = usePageComments(pageId)
  const createComment = useCreatePageComment(pageId)
  const byId = new Map(users.map((user) => [user.id, user]))

  function submit() {
    if (!body.trim()) return
    createComment.mutate({ body: body.trim() })
    setBody("")
  }

  return (
    <div className="flex h-full min-h-[420px] flex-col">
      <ScrollArea className="min-h-0 flex-1 pr-2">
        {isLoading ? (
          <p className="p-4 text-sm text-muted-foreground">Loading comments...</p>
        ) : comments.length ? (
          <div className="space-y-3">
            {comments.map((comment) => {
              const user = byId.get(comment.authorId)
              return (
                <article key={comment.id} className="rounded-xl border border-border bg-card p-3">
                  <div className="mb-2 flex items-center gap-2">
                    <Avatar className="size-7">
                      <AvatarFallback style={{ backgroundColor: user?.color ?? "#6161ff", color: "white" }}>
                        {(user?.name ?? "NA").slice(0, 2).toUpperCase()}
                      </AvatarFallback>
                    </Avatar>
                    <div className="min-w-0">
                      <p className="truncate text-sm font-medium text-foreground">{user?.name ?? "Unknown"}</p>
                      <p className="text-xs text-muted-foreground">{new Date(comment.createdAt).toLocaleString()}</p>
                    </div>
                  </div>
                  <p className="text-sm leading-6 text-muted-foreground">{comment.body}</p>
                </article>
              )
            })}
          </div>
        ) : (
          <Empty className="border border-dashed border-border p-8">
            <EmptyHeader>
              <MessageSquareText className="size-8 text-primary" />
              <EmptyTitle className="text-base">No comments</EmptyTitle>
              <EmptyDescription>Start a focused discussion on this page.</EmptyDescription>
            </EmptyHeader>
          </Empty>
        )}
      </ScrollArea>
      <div className="mt-3 rounded-xl border border-border bg-card p-2">
        <Textarea value={body} onChange={(event) => setBody(event.target.value)} placeholder="Add a comment..." className="min-h-20 border-0 shadow-none focus-visible:ring-0" />
        <div className="flex justify-end">
          <Button size="sm" onClick={submit} disabled={createComment.isPending || !body.trim()}>
            <Send className="size-4" />
            Send
          </Button>
        </div>
      </div>
    </div>
  )
}
