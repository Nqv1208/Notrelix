import type { Comment } from "../types/collaboration";

export interface CollaborationApiClient {
  get<TResponse>(url: string, options?: unknown): Promise<TResponse>;
  post<TResponse, TBody = unknown>(
    url: string,
    body?: TBody,
    options?: unknown,
  ): Promise<TResponse>;
  delete<TResponse>(url: string, options?: unknown): Promise<TResponse>;
}

export interface CollaborationEndpoints {
  comments: {
    list: (resourceId: string) => string;
    create: (resourceId: string) => string;
    delete: (commentId: string) => string;
  };
}

interface CommentDtoApi {
  id: string;
  resourceId: string;
  authorId: string;
  authorName: string;
  body: string;
  mentionIds?: string[];
  resolved?: boolean;
  parentId?: string;
  createdAt: string;
  updatedAt: string;
}

function mapCommentDto(dto: CommentDtoApi): Comment {
  const comment: Comment = {
    id: dto.id,
    resourceId: dto.resourceId,
    resourceType: "page",
    authorId: dto.authorId,
    authorName: dto.authorName,
    body: dto.body,
    mentionIds: dto.mentionIds ?? [],
    resolved: dto.resolved ?? false,
    createdAt: dto.createdAt,
    updatedAt: dto.updatedAt,
  };
  if (dto.parentId !== undefined) {
    comment.parentId = dto.parentId;
  }
  return comment;
}

export function createCommentService(
  api: CollaborationApiClient,
  endpoints: CollaborationEndpoints,
) {
  return {
    async list(resourceId: string): Promise<Comment[]> {
      const dtos = await api.get<CommentDtoApi[]>(
        endpoints.comments.list(resourceId),
      );
      return dtos.map(mapCommentDto);
    },

    async create(
      resourceId: string,
      body: string,
      authorId: string,
      authorName: string,
    ): Promise<Comment> {
      const dto = await api.post<CommentDtoApi>(
        endpoints.comments.create(resourceId),
        {
          body,
          authorId,
          authorName,
        },
      );
      return mapCommentDto(dto);
    },

    async remove(commentId: string): Promise<void> {
      await api.delete(endpoints.comments.delete(commentId));
    },
  };
}
