"use client"

import { MessageSquareText, Send } from "lucide-react"
import { Avatar, AvatarFallback } from "@/components/ui/avatar"
import { Button } from "@/components/ui/button"
import { Skeleton } from "@/components/ui/skeleton"
import { Textarea } from "@/components/ui/textarea"
import { useCardComments } from "@/features/boards/hooks"

export function CardComments({ cardId }: { cardId: string }) {
  const { data = [], isLoading } = useCardComments(cardId)

  return (
    <section className="rounded-2xl border border-border bg-card p-5">
      <div className="mb-4 flex items-center gap-2">
        <MessageSquareText className="size-4 text-primary" />
        <h2 className="text-sm font-semibold text-foreground">Comments</h2>
      </div>

      {isLoading ? (
        <div className="space-y-2">
          <Skeleton className="h-16 rounded-xl" />
          <Skeleton className="h-16 rounded-xl" />
        </div>
      ) : (
        <div className="space-y-3">
          {data.map((comment) => (
            <article key={comment.id} className="flex gap-3 rounded-xl border border-border bg-muted/40 p-3">
              <Avatar className="size-8">
                <AvatarFallback className="bg-primary text-[10px] font-semibold text-primary-foreground">
                  {comment.author.initials}
                </AvatarFallback>
              </Avatar>
              <div className="min-w-0 flex-1">
                <div className="flex items-center gap-2">
                  <p className="text-sm font-medium text-foreground">{comment.author.name}</p>
                  <span className="text-xs text-muted-foreground">{new Date(comment.createdAt).toLocaleString()}</span>
                </div>
                <p className="mt-1 text-sm leading-6 text-muted-foreground">{comment.body}</p>
              </div>
            </article>
          ))}
        </div>
      )}

      <div className="mt-4 flex gap-2">
        <Textarea className="min-h-12 bg-muted/40" placeholder="Write a comment..." />
        <Button size="icon-sm" className="mt-1 rounded-full" aria-label="Send comment">
          <Send className="size-4" />
        </Button>
      </div>
    </section>
  )
}
