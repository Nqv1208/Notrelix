/**
 * @notrelix/docs-core — Docs product core types and pure query keys.
 *
 * Framework-neutral: no React, no DOM.
 * Consumed by docs/web and docs/mobile.
 */

export { convertBlockProperties, TEXT_BLOCK_TYPES } from "./types/block";

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
  MovePageToSpacePayload,
  UpdatePagePayload,
  PageComment,
  CreateCommentPayload,
  PageTreeNode,
  SearchResult,
} from "./types";

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
