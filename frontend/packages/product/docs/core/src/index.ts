/**
 * @notrelix/docs-core — Docs product core types and pure query keys.
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
} from "./types";

export { docsQueryKeys } from "./query/keys";
