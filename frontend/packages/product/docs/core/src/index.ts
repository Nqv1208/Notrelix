/**
 * @notrelix/docs-core — Docs product core types, DTOs, mappers, and API contracts.
 *
 * Framework-neutral: no React, no DOM.
 * Consumed by docs/web and docs/mobile.
 */

// Types
export type {
  ID,
  DocsUser,
  DocsRole,
  PresenceStatus,
  Mention,
  LinkedTask,
  LinkedBoard,
  BlockType,
  BlockProperties,
  Block,
  CreateBlockPayload,
  UpdateBlockPayload,
  ReorderBlocksInput,
  PageStatus,
  CollaborativeMetadata,
  Page,
  BreadcrumbItem,
  PageDetail,
  PageActivity,
  CreatePagePayload,
  UpdatePagePayload,
  PageComment,
  CreateCommentPayload,
  PageTreeNode,
  SearchResult,
} from './types';

// DTOs
export type {
  PageDtoApi,
  BreadcrumbDtoApi,
  CommentDtoApi,
  HistoryDtoApi,
  BlockDtoApi,
} from './dto';

// Mappers
export { mapPage, mapBreadcrumb, mapHistory } from './model/page.mapper';
export { mapBlock, parseProperties } from './model/block.mapper';
export { mapComment } from './model/comment.mapper';

// API
export { createPageApi } from './api/page.api';
export type { DocsApiClient, PageApiEndpoints } from './api/page.api';
export { createBreadcrumbApi } from './api/breadcrumb.api';
export { createSearchApi } from './api/search.api';
export { createFavoriteApi } from './api/favorite.api';
export { createBlockApi } from './api/block.api';
export { createCommentApi } from './api/comment.api';
export { createHistoryApi } from './api/history.api';

export * from './query';
