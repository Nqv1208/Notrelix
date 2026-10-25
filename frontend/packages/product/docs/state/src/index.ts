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
  MovePageToSpacePayload,
  UpdatePagePayload,
  PageComment,
  CreateCommentPayload,
  PageTreeNode,
  SearchResult,
} from "@notrelix/docs-core";

export type {
  PageDtoApi,
  BreadcrumbDtoApi,
  CommentDtoApi,
  HistoryDtoApi,
  BlockDtoApi,
} from "./dto";

export { mapPage, mapBreadcrumb, mapHistory } from "./model/page.mapper";
export {
  CREATABLE_BLOCK_TYPES,
  mapBlock,
  parseProperties,
} from "./model/block.mapper";
export { mapComment } from "./model/comment.mapper";

export { createPageApi } from "./api/page.api";
export type {
  DocsApiClient,
  DocsRequestOptions,
  PageApiEndpoints,
} from "./api/page.api";
export { createBreadcrumbApi } from "./api/breadcrumb.api";
export { createSearchApi } from "./api/search.api";
export { createFavoriteApi } from "./api/favorite.api";
export { createBlockApi } from "./api/block.api";
export { createCommentApi } from "./api/comment.api";
export { createHistoryApi } from "./api/history.api";
export { createPageExtrasApi } from "./api/page-extras.api";
export type {
  ConvertedTaskDto,
  PageBacklinksDto,
  PagePermissionLevel,
  PagePermissionsDto,
} from "./api/page-extras.api";

export * from "./query";
