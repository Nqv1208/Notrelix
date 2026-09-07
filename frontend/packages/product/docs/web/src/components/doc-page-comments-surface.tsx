import { useState } from "react";
import type { PageComment } from "@notrelix/docs-core";
import { Avatar, AvatarFallback, Button } from "@notrelix/ui-web";
import { MessageSquare, Send, Trash2 } from "lucide-react";
import {
  formatDate,
  type DocPageSurfaceCallbacks,
} from "./doc-page-surface-models";

export function DocCommentsSurface({
  comments,
  callbacks,
}: {
  comments: PageComment[];
  callbacks?: DocPageSurfaceCallbacks;
}) {
  const [draft, setDraft] = useState("");

  function submit() {
    const body = draft.trim();
    if (!body) return;
    callbacks?.onCreateComment?.(body);
    setDraft("");
  }

  return (
    <section className="space-y-4" aria-label="Document comments">
      <div className="flex items-center gap-2 text-sm font-medium text-muted-foreground">
        <MessageSquare className="h-4 w-4" />
        Comments ({comments.length})
      </div>
      {comments.length === 0 ? (
        <p className="py-3 text-sm italic text-muted-foreground">
          No comments yet
        </p>
      ) : (
        <div className="divide-y divide-border">
          {comments.map((comment) => (
            <div key={comment.id} className="group flex gap-3 py-3">
              <Avatar className="h-8 w-8 shrink-0">
                <AvatarFallback className="bg-muted text-xs">
                  {comment.authorId.slice(0, 2).toUpperCase()}
                </AvatarFallback>
              </Avatar>
              <div className="min-w-0 flex-1">
                <div className="mb-1 flex items-center gap-2">
                  <span className="text-sm font-medium">
                    {comment.authorId}
                  </span>
                  <span className="text-xs text-muted-foreground">
                    {formatDate(comment.createdAt)}
                  </span>
                  {comment.resolved ? (
                    <span className="rounded border border-border bg-muted px-1.5 py-0.5 text-xs text-foreground">
                      Resolved
                    </span>
                  ) : null}
                </div>
                <p className="whitespace-pre-wrap text-sm text-foreground">
                  {comment.body}
                </p>
              </div>
              <Button
                type="button"
                variant="ghost"
                size="icon"
                className="h-7 w-7 text-muted-foreground"
                aria-label={`Delete comment ${comment.id}`}
                onClick={() => callbacks?.onDeleteComment?.(comment.id)}
              >
                <Trash2 className="h-3.5 w-3.5" />
              </Button>
            </div>
          ))}
        </div>
      )}
      <div className="flex gap-2">
        <input
          type="text"
          value={draft}
          onChange={(event) => setDraft(event.currentTarget.value)}
          onKeyDown={(event) => {
            if (event.key === "Enter") submit();
          }}
          placeholder="Add a comment..."
          aria-label="New comment"
          className="h-9 min-w-0 flex-1 rounded-md border border-input bg-transparent px-3 py-1 text-sm outline-none focus-visible:ring-1 focus-visible:ring-ring"
        />
        <Button
          type="button"
          size="icon"
          className="h-9 w-9 shrink-0"
          aria-label="Send comment"
          disabled={!draft.trim()}
          onClick={submit}
        >
          <Send className="h-4 w-4" />
        </Button>
      </div>
    </section>
  );
}