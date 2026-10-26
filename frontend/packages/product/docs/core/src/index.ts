/**
 * @notrelix/docs-core — Docs product core types and pure query keys.
 *
 * Framework-neutral: no React, no DOM.
 * Consumed by docs/web and docs/mobile.
 */

export {
  convertBlockProperties,
  emptyTable,
  TABLE_MAX_COL_WIDTH,
  TABLE_MIN_COL_WIDTH,
  TEXT_BLOCK_TYPES,
} from "./types/block";

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
  TableCell,
  TableData,
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
  CommentAnchor,
  PageComment,
  CreateCommentPayload,
  PageTreeNode,
  SearchResult,
} from "./types";

export {
  COMMENT_ANCHOR_MAX_QUOTE,
  resolveCommentAnchor,
} from "./types/comment";

export {
  BLOCKED_PAGE_FILE_EXTENSIONS,
  IMAGE_MAX_ALT_LENGTH,
  IMAGE_MAX_WIDTH,
  IMAGE_MIN_WIDTH,
  MAX_PAGE_FILE_BYTES,
  clampImageWidth,
  fileExtension,
  formatFileSize,
  isPageImage,
  refusePageFile,
} from "./types/attachment-rules";
export type { PageAttachment, PageFileRefusal } from "./types/attachment-rules";
export { canRetryUpload } from "./types/attachment-uploads";
export type {
  AddUploadResult,
  AttachmentUploadsView,
  PendingUpload,
  UploadFailure,
} from "./types/attachment-uploads";

export {
  COLOR_TOKENS,
  HIGHLIGHT_TOKENS,
  MAX_TEXT_LENGTH,
  isRichTextV1,
  isSafeHref,
  normalizeMarks,
  normalizeRuns,
  parseRichText,
  plainTextOf,
  plainTextOfRuns,
  richText,
  richTextEquals,
  runsFromPlainText,
} from "./types/rich-text";
export type {
  ColorToken,
  HighlightToken,
  Mark,
  MarkType,
  RichTextV1,
  Run,
  SimpleMarkType,
} from "./types/rich-text";
export {
  blockPlainText,
  diffBlockDrafts,
  draftEquals,
  draftOfBlock,
  isEmptyDiff,
  plainDraftContent,
  upgradeLegacyProperties,
} from "./types/block-content";
export type {
  BlockDiff,
  BlockDraft,
  BlockDraftPatch,
} from "./types/block-content";
