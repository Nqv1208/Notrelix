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

/** `Documents.Blocks.ListPageBlocks` (`BlockDto`); `content` is the block's JSON body. */
export type BlockDtoApi = {
  id: string;
  pageId: string;
  parentId?: string | null;
  /** The producer's block type name (`Text`, `Heading1`, `ToDo`, ...). */
  type: string;
  /** The block's JSON body, as an object or JSON text. */
  content: string | Record<string, unknown>;
  /** Present once the producer sends the block's properties apart from its text. */
  properties?: string | Record<string, unknown>;
  position: string;
  version: number;
  createdAt: string;
  updatedAt?: string | null;
};
