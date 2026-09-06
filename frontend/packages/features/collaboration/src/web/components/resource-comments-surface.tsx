import { useState } from "react";
import { MessageSquare, Send, Trash2 } from "lucide-react";
import { Avatar, Button, Input } from "@notrelix/ui-web";
import type { Comment } from "../../core/types/collaboration";

export interface ResourceCommentsSurfaceProps {
  comments: readonly Comment[];
  currentUserId?: string;
  status?: "idle" | "loading";
  onCreateComment?: (body: string) => void;
  onDeleteComment?: (commentId: string) => void;
}

function formatDate(isoString: string): string {
  return isoString.slice(0, 10);
}

export function ResourceCommentsSurface({
  comments,
  currentUserId = "current-user",
  status = "idle",
  onCreateComment,
  onDeleteComment,
}: ResourceCommentsSurfaceProps) {
  const [draft, setDraft] = useState("");
  const isLoading = status === "loading";

  function submit() {
    const body = draft.trim();
    if (!body) return;
    onCreateComment?.(body);
    setDraft("");
  }

  return (
    <div className="flex flex-col gap-3">
      <div className="flex items-center gap-2 text-sm font-medium text-muted-foreground">
        <MessageSquare className="size-4" />
        Comments ({comments.length})
      </div>

      {isLoading ? (
        <div className="space-y-3">
          {[1, 2].map((i) => (
            <div key={i} className="h-16 bg-muted rounded animate-pulse" />
          ))}
        </div>
      ) : (
        <div className="space-y-3">
          {comments.map((comment) => (
            <div
              key={comment.id}
              className="group flex gap-3 p-3 rounded-lg bg-muted/30"
            >
              <Avatar className="size-8 shrink-0">
                <div className="flex items-center justify-center size-full text-xs font-medium bg-primary/10 text-primary rounded-full">
                  {comment.authorName.charAt(0).toUpperCase()}
                </div>
              </Avatar>
              <div className="flex-1 min-w-0">
                <div className="flex items-center gap-2 mb-1">
                  <span className="text-sm font-medium">
                    {comment.authorName}
                  </span>
                  <span className="text-xs text-muted-foreground">
                    {formatDate(comment.createdAt)}
                  </span>
                </div>
                <p className="text-sm text-foreground whitespace-pre-wrap">
                  {comment.body}
                </p>
              </div>
              {comment.authorId === currentUserId ? (
                <button
                  type="button"
                  aria-label={`Delete comment ${comment.id}`}
                  onClick={() => onDeleteComment?.(comment.id)}
                  className="p-1 hover:bg-muted rounded opacity-0 group-hover:opacity-100 transition-opacity"
                >
                  <Trash2 className="size-3.5 text-muted-foreground hover:text-destructive" />
                </button>
              ) : null}
            </div>
          ))}
          {comments.length === 0 ? (
            <p className="text-sm text-muted-foreground py-4 text-center">
              No comments yet.
            </p>
          ) : null}
        </div>
      )}

      <form
        className="flex gap-2"
        onSubmit={(event) => {
          event.preventDefault();
          submit();
        }}
      >
        <Input
          value={draft}
          onChange={(event) => setDraft(event.target.value)}
          placeholder="Add a comment..."
          aria-label="New comment"
          className="flex-1"
        />
        <Button
          type="submit"
          size="icon"
          aria-label="Send comment"
          disabled={!draft.trim()}
        >
          <Send className="size-4" />
        </Button>
      </form>
    </div>
  );
}
