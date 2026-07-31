import React, { useState } from 'react';
import {
  createUsePageComments,
  createUseCreateComment,
  createUseDeleteComment,
  type DocsApiClient,
  type PageApiEndpoints,
  type PageComment,
} from '@notrelix/docs-state';
import { Button, Avatar, AvatarFallback } from '@notrelix/ui-web';
import { MessageSquare, Trash2, Send } from 'lucide-react';

interface DocCommentsProps {
  api: DocsApiClient;
  endpoints: PageApiEndpoints;
  pageId: string;
}

function CommentItem({
  comment,
  onDelete,
}: {
  comment: PageComment;
  onDelete: (id: string) => void;
}) {
  return (
    <div className="group flex gap-3 py-3">
      <Avatar className="size-8 shrink-0">
        <AvatarFallback className="text-xs bg-muted">
          {comment.authorId.substring(0, 2).toUpperCase()}
        </AvatarFallback>
      </Avatar>
      <div className="flex-1 min-w-0">
        <div className="flex items-center gap-2 mb-1">
          <span className="text-sm font-medium">{comment.authorId}</span>
          <span className="text-xs text-muted-foreground">
            {new Date(comment.createdAt).toLocaleDateString()}
          </span>
          {comment.resolved && (
            <span className="text-xs text-primary bg-primary/10 px-1.5 py-0.5 rounded">
              Resolved
            </span>
          )}
        </div>
        <p className="text-sm text-foreground whitespace-pre-wrap">{comment.body}</p>
      </div>
      <Button
        variant="ghost"
        size="icon"
        className="h-6 w-6 opacity-0 group-hover:opacity-100 transition-opacity text-muted-foreground hover:text-destructive"
        onClick={() => onDelete(comment.id)}
      >
        <Trash2 className="h-3 w-3" />
      </Button>
    </div>
  );
}

export function DocComments({ api, endpoints, pageId }: DocCommentsProps) {
  const usePageComments = createUsePageComments(api, endpoints);
  const useCreateComment = createUseCreateComment(api, endpoints);
  const useDeleteComment = createUseDeleteComment(api, endpoints);

  const { data: comments = [], isLoading } = usePageComments(pageId);
  const createMutation = useCreateComment(pageId);
  const deleteMutation = useDeleteComment(pageId);

  const [newComment, setNewComment] = useState('');

  const handleSubmit = () => {
    if (!newComment.trim()) return;
    createMutation.mutate(
      { pageId, body: newComment.trim() },
      { onSuccess: () => setNewComment('') }
    );
  };

  return (
    <div className="space-y-4">
      <div className="flex items-center gap-2 text-sm font-medium text-muted-foreground">
        <MessageSquare className="h-4 w-4" />
        Comments ({comments.length})
      </div>

      {/* Comment list */}
      <div className="divide-y divide-border">
        {isLoading ? (
          <div className="space-y-3 py-3">
            {[1, 2].map((i) => (
              <div key={i} className="h-16 bg-muted rounded animate-pulse" />
            ))}
          </div>
        ) : comments.length === 0 ? (
          <p className="text-sm text-muted-foreground py-3 italic">No comments yet</p>
        ) : (
          comments.map((comment: PageComment) => (
            <CommentItem
              key={comment.id}
              comment={comment}
              onDelete={(id) => deleteMutation.mutate(id as never)}
            />
          ))
        )}
      </div>

      {/* Comment composer */}
      <div className="flex gap-2">
        <input
          type="text"
          value={newComment}
          onChange={(e) => setNewComment(e.target.value)}
          onKeyDown={(e) => e.key === 'Enter' && !e.shiftKey && handleSubmit()}
          placeholder="Add a comment..."
          className="flex-1 h-9 rounded-md border border-input bg-transparent px-3 py-1 text-sm placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring"
        />
        <Button
          size="icon"
          className="h-9 w-9 shrink-0"
          onClick={handleSubmit}
          disabled={!newComment.trim() || createMutation.isPending}
        >
          <Send className="h-4 w-4" />
        </Button>
      </div>
    </div>
  );
}
