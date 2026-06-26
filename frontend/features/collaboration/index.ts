// Public API for the collaboration feature slice.
// Explicit exports only.

export { commentsApi } from "./comments/api/comments.api"
export type { CollaborationComment, ResourceType } from "./comments/api/comments.api"
export {
  useComments,
  useCreateComment,
  useUpdateComment,
  useDeleteComment
} from "./comments/hooks/use-comments"

export type { ResourceRef } from "./shared/resource-ref"
