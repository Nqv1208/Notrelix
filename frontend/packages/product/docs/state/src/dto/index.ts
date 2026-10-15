import type { Block, BlockProperties } from "@notrelix/docs-core";

export type PageDtoApi = {
  id: string;
  workspaceId: string;
  parentId?: string | null;
  title: string;
  iconType?: string | null;
  iconValue?: string | null;
  coverUrl?: string | null;
  position: number;
  depth: number;
  isTemplate: boolean;
  isArchived: boolean;
  publishedAt?: string | null;
  deadline?: string | null;
  createdAt: string;
  updatedAt?: string | null;
  /** The Page aggregate version (`PageDto.version`). */
  version?: number;
  spaceId?: string | null;
  /** Gap `CTR-GAP-DOCS-PAGE-EXT`: the review state and who is on the page now. */
  status?: "draft" | "review" | "published";
  activeUserIds?: string[];
  people?: { id: string; name: string }[];
};

export type BreadcrumbDtoApi = {
  id: string;
  title: string;
  iconType?: string | null;
  iconValue?: string | null;
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

export type BlockDtoApi = {
  id: string;
  pageId: string;
  parentBlockId?: string | null;
  type: Block["type"];
  properties: string | BlockProperties;
  position: number;
  version: number;
  createdByUserId: string;
  createdAt: string;
  updatedAt?: string | null;
};
