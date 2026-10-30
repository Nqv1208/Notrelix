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
  MovePagePayload,
  MovePageToSpacePayload,
  UpdatePagePayload,
  PageComment,
  CreateCommentPayload,
  PageTreeNode,
  SearchResult,
  PageVersionKind,
  PageVersion,
  PageTemplate,
  CreatePageTemplatePayload,
  CreatePageFromTemplatePayload,
  PageFromTemplate,
} from "@notrelix/docs-core";

export type {
  PageDtoApi,
  BreadcrumbDtoApi,
  CommentDtoApi,
  HistoryDtoApi,
  BlockDtoApi,
  PageVersionDtoApi,
  PageVersionDetailDtoApi,
  RestorePageVersionResultDtoApi,
  PageTemplateDtoApi,
  PageFromTemplateDtoApi,
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
  DocsFileClient,
  DocsRequestOptions,
  DocsUploadOptions,
  DocsUploadProgress,
  PageApiEndpoints,
  TrashPage,
} from "./api/page.api";
export { createBreadcrumbApi } from "./api/breadcrumb.api";
export { createSearchApi } from "./api/search.api";
export { createFavoriteApi } from "./api/favorite.api";
export { createAttachmentApi, mapPageAttachment } from "./api/attachment.api";
export type { PageAttachmentDtoApi } from "./api/attachment.api";
export { createBlockApi } from "./api/block.api";
export { createCommentApi } from "./api/comment.api";
export { createHistoryApi } from "./api/history.api";
export { createVersionApi } from "./api/version.api";
export type { RestoredPageVersion, SeenBlockVersion } from "./api/version.api";
export { createTemplateApi } from "./api/template.api";
export { createImportApi } from "./api/import.api";
export type { ImportPageInput } from "./api/import.api";
export { mapVersion, mapVersionContent } from "./model/version.mapper";
export type { PageVersionContent } from "./model/version.mapper";
export {
  convertTodoFailedStep,
  createPageExtrasApi,
} from "./api/page-extras.api";
export type {
  ConvertedTaskDto,
  ConvertTodoTarget,
  ConvertTodoStep,
  PageBacklinksDto,
  PagePermissionLevel,
  PagePermissionsDto,
} from "./api/page-extras.api";

export * from "./query";

export {
  BlockVersionConflict,
  DocBlockSaver,
  planBlockWrites,
} from "./editor/doc-block-saver";
export type {
  BlockConflict,
  BlockWrite,
  BlockWriter,
  DocBlockSaverOptions,
  SaverStatus,
  SavedBlock,
} from "./editor/doc-block-saver";
export {
  createBatchBlockWriter,
  toBatchItem,
} from "./editor/batch-block-writer";
export {
  decodeBlockBody,
  encodeContent,
  encodeProperties,
} from "./model/block-wire";

export {
  AttachmentUploads,
  canRetryUpload,
  classifyUploadFailure,
} from "./editor/attachment-uploads";
export type {
  AddUploadResult,
  AttachmentUploadsOptions,
  PendingUpload,
  UploadFailure,
} from "./editor/attachment-uploads";
