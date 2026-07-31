import React, { useState, useMemo } from 'react';
import type { Comment } from '../../core';
import { createUseComments, createUseCreateComment, createUseDeleteComment } from '../query/hooks/use-comments';
import { Button, Input, Avatar } from '@notrelix/ui-web';
import { MessageSquare, Send, Trash2 } from 'lucide-react';

const collabEndpoints = {
  comments: {
    list: (resourceId: string) => `/api/v1/comments?resourceId=${resourceId}`,
    create: (resourceId: string) => `/api/v1/comments?resourceId=${resourceId}`,
    delete: (commentId: string) => `/api/v1/comments/${commentId}`,
  },
};

interface ResourceCommentsProps {
  resourceId: string;
  resourceType: 'page' | 'block' | 'card';
  currentUserId?: string;
  currentUserName?: string;
  api: any;
}

export function ResourceComments({
  resourceId,
  resourceType: _resourceType,
  currentUserId = 'current-user',
  currentUserName = 'You',
  api,
}: ResourceCommentsProps) {
  const [newComment, setNewComment] = useState('');

  const useComments = useMemo(
    () => createUseComments(api, collabEndpoints),
    [api],
  );
  const useCreateComment = useMemo(
    () => createUseCreateComment(api, collabEndpoints),
    [api],
  );
  const useDeleteComment = useMemo(
    () => createUseDeleteComment(api, collabEndpoints),
    [api],
  );

  const { data: comments = [], isLoading } = useComments(resourceId);
  const createComment = useCreateComment();
  const deleteComment = useDeleteComment(resourceId);

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!newComment.trim()) return;
    createComment.mutate({
      resourceId,
      body: newComment.trim(),
      authorId: currentUserId,
      authorName: currentUserName,
    });
    setNewComment('');
  };

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
          {comments.map((comment: Comment) => (
            <div key={comment.id} className="flex gap-3 p-3 rounded-lg bg-muted/30">
              <Avatar className="size-8 shrink-0">
                <div className="flex items-center justify-center size-full text-xs font-medium bg-primary/10 text-primary rounded-full">
                  {comment.authorName.charAt(0).toUpperCase()}
                </div>
              </Avatar>
              <div className="flex-1 min-w-0">
                <div className="flex items-center gap-2 mb-1">
                  <span className="text-sm font-medium">{comment.authorName}</span>
                  <span className="text-xs text-muted-foreground">
                    {new Date(comment.createdAt).toLocaleDateString()}
                  </span>
                </div>
                <p className="text-sm text-foreground whitespace-pre-wrap">{comment.body}</p>
              </div>
              {comment.authorId === currentUserId && (
                <button
                  onClick={() => deleteComment.mutate(comment.id)}
                  className="p-1 hover:bg-muted rounded opacity-0 group-hover:opacity-100 transition-opacity"
                >
                  <Trash2 className="size-3.5 text-muted-foreground hover:text-destructive" />
                </button>
              )}
            </div>
          ))}
          {comments.length === 0 && (
            <p className="text-sm text-muted-foreground py-4 text-center">No comments yet.</p>
          )}
        </div>
      )}

      <form onSubmit={handleSubmit} className="flex gap-2">
        <Input
          value={newComment}
          onChange={(e: React.ChangeEvent<HTMLInputElement>) => setNewComment(e.target.value)}
          placeholder="Add a comment..."
          className="flex-1"
        />
        <Button
          type="submit"
          size="icon"
          disabled={!newComment.trim() || createComment.isPending}
        >
          <Send className="size-4" />
        </Button>
      </form>
    </div>
  );
}
