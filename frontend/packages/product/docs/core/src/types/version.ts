import type { ID } from "./ids";

/** Why a history entry exists: taken by the system, saved with a name, or written by a restore. */
export type PageVersionKind = "automatic" | "named" | "restore";

/**
 * One entry of a page's history. `versionNumber` is its place in this page's
 * history; it is not the page's or a block's concurrency version.
 */
export interface PageVersion {
  id: ID;
  pageId: ID;
  versionNumber: number;
  kind: PageVersionKind;
  /** The person's name for a named version; "Restored from version N" for a restore. */
  name: string | null;
  createdBy: ID | null;
  createdAt: string;
}

/** A workspace template: a copy of a page's content that new pages are made from. */
export interface PageTemplate {
  id: ID;
  name: string;
  description: string | null;
  /** The title a page made from it starts with. */
  title: string;
  icon: string;
  blockCount: number;
  createdBy: ID | null;
  createdAt: string;
}

export interface CreatePageTemplatePayload {
  pageId: ID;
  name: string;
  description?: string;
}

export interface CreatePageFromTemplatePayload {
  templateId: ID;
  title?: string;
  parentId?: ID | null;
  spaceId?: ID | null;
  /** One per user action; the same key only to retry that action. */
  idempotencyKey?: string;
}

/** A page made from a template; `droppedBlockCount` blocks (images, files, videos) were not copied. */
export interface PageFromTemplate {
  pageId: ID;
  droppedBlockCount: number;
}
