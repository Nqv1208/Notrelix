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

/** `Documents.Pages.ListTrashedPages`: one cursor page of the trash. */
export type TrashPageDtoApi = {
  items?: PageDtoApi[] | null;
  /** Opaque; sent back as `cursor` for the next page, absent on the last one. */
  nextCursor?: string | null;
};

export type BreadcrumbDtoApi = {
  /** Absent on a crumb the viewer may not see (`unavailable`). */
  id?: string | null;
  title?: string | null;
  icon?: string | null;
  unavailable?: boolean;
};

export type CommentDtoApi = {
  id: string;
  userId: string;
  contentMd: string;
  createdAt: string;
  isEdited: boolean;
  /** When the thread was marked resolved; absent while it is open. */
  resolvedAt?: string | null;
  /** `Collaboration.Comments.GetPageComments`: the quoted range of a block, when the comment has one. */
  anchor?: {
    blockId: string;
    quote: string;
    start: number;
    end: number;
  } | null;
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

/** `Documents.Versions.ListPageVersions` (`PageVersionDto`). */
export type PageVersionDtoApi = {
  id: string;
  pageId: string;
  versionNumber: number;
  /** `automatic`, `named` or `restore`. */
  kind: string;
  name?: string | null;
  createdBy?: string | null;
  createdAt: string;
};

/** A block as a version captured it: no concurrency version, since a snapshot is never edited. */
export type VersionBlockDtoApi = {
  id: string;
  parentId?: string | null;
  type: string;
  content: Record<string, unknown>;
  properties: Record<string, unknown>;
  position: string;
};

/** `Documents.Versions.GetPageVersion`. */
export type PageVersionDetailDtoApi = {
  version: PageVersionDtoApi;
  title: string;
  blocks: VersionBlockDtoApi[];
};

/** `Documents.Versions.RestorePageVersion`. */
export type RestorePageVersionResultDtoApi = {
  version: PageVersionDtoApi;
  blocks: BlockDtoApi[];
};

/** `Documents.Templates.ListPageTemplates` (`PageTemplateDto`). */
export type PageTemplateDtoApi = {
  id: string;
  name: string;
  description?: string | null;
  title: string;
  icon: string;
  blockCount: number;
  createdBy?: string | null;
  createdAt: string;
};

/** `Documents.Templates.CreatePageFromTemplate`. */
export type PageFromTemplateDtoApi = {
  pageId: string;
  droppedBlockCount: number;
};
