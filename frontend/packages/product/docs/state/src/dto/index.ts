/** `Documents.Pages.GetPage` and the page lists (`PageDto`). */
export type PageDtoApi = {
  id: string;
  workspaceId: string;
  parentId?: string | null;
  title: string;
  /** The page's icon (an emoji). */
  icon?: string | null;
  coverImage?: string | null;
  createdAt: string;
  updatedAt?: string | null;
  /** The Page aggregate version, sent back as `expectedVersion` by a versioned write. */
  version?: number;
  spaceId?: string | null;
  /**
   * Beyond the producer's `PageDto` (gap `CTR-GAP-DOCS-PAGE-EXT`): the review
   * state, who is on the page and the short tree name. A producer that does
   * not send them leaves the page a draft with no presence.
   */
  status?: "draft" | "review" | "published";
  navTitle?: string;
  isArchived?: boolean;
  publishedAt?: string | null;
  deadline?: string | null;
  activeUserIds?: string[];
  people?: { id: string; name: string }[];
};

export type BreadcrumbDtoApi = {
  id: string;
  title: string;
  icon?: string | null;
};

export type CommentDtoApi = {
  id: string;
  userId: string;
  contentMd: string;
  createdAt: string;
  isEdited: boolean;
};

export type HistoryDtoApi = {
  id: string;
  actorId: string;
  action: string;
  resourceTitle?: string | null;
  /** Gap `CTR-GAP-DOCS-PAGE-EXT`: a version the member named. */
  versionName?: string | null;
  createdAt: string;
};

/**
 * `Documents.Blocks.ListPageBlocks` (`BlockDto`). `content` and `properties`
 * are JSON objects: rich text v1 (`{ v: 1, runs }`) for a text-like block, `{}`
 * otherwise; `properties` holds the type's own values. The generated REST
 * schema types both as `unknown`, so the shapes are named here.
 */
export type BlockDtoApi = {
  id: string;
  pageId: string;
  parentId?: string | null;
  /** The producer's block type name (`Text`, `Heading1`, `ToDo`, ...). */
  type: string;
  content: Record<string, unknown>;
  properties: Record<string, unknown>;
  /** The block's fractional ordering key. */
  position: string;
  /** Sent back as `expectedVersion` by an edit or a delete. */
  version: number;
  createdAt: string;
  updatedAt?: string | null;
};

/** One operation of `Documents.Blocks.BatchUpdateBlocks`. */
export type BatchBlockItemApi = {
  id: string;
  op?: "upsert" | "delete";
  type?: string;
  content?: Record<string, unknown>;
  properties?: Record<string, unknown>;
  afterBlockId?: string;
  parentId?: string;
  expectedVersion?: number;
};
