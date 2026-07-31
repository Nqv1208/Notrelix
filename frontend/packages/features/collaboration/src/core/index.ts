/**
 * @notrelix/feature-collaboration — Collaboration core types.
 *
 * Framework-neutral: no React, no DOM.
 */

export type {
  Comment,
  Reaction,
  Presence,
  Attachment,
} from './types/collaboration';

export { collaborationQueryKeys } from './query/keys';

export {
  createCommentService,
  type CollaborationApiClient,
  type CollaborationEndpoints,
} from './api/comment.service';
