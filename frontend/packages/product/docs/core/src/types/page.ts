import type { ID } from "./ids";
import type { DocsUser } from "./user";
import type { LinkedTask, LinkedBoard } from "./integration";
import type { Block } from "./block";

export type PageStatus = "draft" | "review" | "published" | "archived";

export interface CollaborativeMetadata {
  version: number;
  lockOwnerId: ID | null;
  activeUserIds: ID[];
  lastSyncedAt: string;
  realtimeChannel: string;
  aiSummaryStatus: "idle" | "queued" | "ready";
}

export interface Page {
  id: ID;
  workspaceId: ID;
  workspaceSlug: string;
  title: string;
  icon: string | null;
  coverUrl: string | null;
  coverColor: string;
  parentId: ID | null;
  position: number;
  status: PageStatus;
  isPublished: boolean;
  isFavorited: boolean;
  isShared: boolean;
  tags: string[];
  authorId: ID;
  lastEditedById: ID;
  lastEditedAt: string;
  createdAt: string;
  updatedAt: string;
  collaboratorIds: ID[];
  metadata: CollaborativeMetadata;
  linkedTaskIds: ID[];
  linkedBoardIds: ID[];
  /**
   * The Page aggregate version (`PageDto.version`), sent back as the
   * `expectedVersion` of a versioned write such as a move to a Space; `null`
   * when the producer did not report it, in which case no versioned write is
   * offered.
   */
  version: number | null;
  /**
   * The Workspaces Space the page is placed in; `null` at the workspace root
   * and when the caller cannot see that Space (GOV-029).
   */
  spaceId?: ID | null;
}

/** `Documents.Pages.MoveToSpace`: a root page moves with its subtree. */
export interface MovePageToSpacePayload {
  pageId: ID;
  /** `null` moves the page back to the workspace level. */
  spaceId: ID | null;
  expectedVersion: number;
}

export interface BreadcrumbItem {
  id: ID;
  title: string;
  icon: string | null;
}

export interface PageDetail extends Page {
  blocks: Block[];
  breadcrumb: BreadcrumbItem[];
  collaborators: DocsUser[];
  linkedTasks: LinkedTask[];
  linkedBoards: LinkedBoard[];
}

export interface PageActivity {
  id: ID;
  pageId: ID;
  actorId: ID;
  action: "created" | "edited" | "commented" | "shared" | "moved" | "published";
  targetLabel: string;
  createdAt: string;
}

export interface CreatePagePayload {
  title: string;
  workspaceId: ID;
  workspaceSlug?: string;
  parentId?: ID | null;
  templateId?: ID;
  /** Create a root page in this Space (a nested page follows its parent). */
  spaceId?: ID | null;
}

export interface UpdatePagePayload {
  title?: string;
  icon?: string | null;
  coverUrl?: string | null;
  coverColor?: string;
  status?: PageStatus;
  isPublished?: boolean;
  isFavorited?: boolean;
  tags?: string[];
}
