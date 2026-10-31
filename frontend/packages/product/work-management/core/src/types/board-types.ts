import type { FieldDefinition, BoardColumnDtoApi } from "./field-types";
import type { BoardGroup, ListDtoApi } from "./group-types";

export interface BoardMember {
  id: string;
  userId: string;
  name: string;
  initials: string;
  role: "owner" | "editor" | "viewer";
  avatarUrl?: string;
  color: string;
}

export interface Board {
  id: string;
  workspaceId: string;
  title: string;
  description?: string;
  background: { type: "color" | "image"; value: string };
  visibility: "private" | "workspace" | "public";
  isArchived: boolean;
  linkedPageId?: string;
  fieldDefinitions: FieldDefinition[];
  members: BoardMember[];
  createdAt: string;
  updatedAt?: string;
  /** The board version of the read: a lifecycle write sends it back (a stale one changes nothing). */
  version?: number;
  /**
   * Whether the caller may add items to this board (workspace board list only;
   * gap `CTR-GAP-WM-TABLE-BULK`). Absent = not known.
   */
  canAddItems?: boolean;
}

export type ViewMode = "table" | "kanban" | "calendar" | "timeline";

/** Row height of the table: "comfortable" 40 px (the default), "compact" 32 px. */
export type TableDensity = "compact" | "comfortable";

export interface ViewConfig {
  groupBy: "list" | "status" | "priority" | "assignee" | string;
  hiddenFields: string[];
  columnOrder: string[];
  columnWidths: Record<string, number>;
  collapsedGroups: Record<string, boolean>;
  filters: FilterConfig[];
  sortBy: SortConfig[];
  density: TableDensity;
  searchQuery?: string;
  /** Kanban: cards a lane shows before "Xem thêm N việc" (1..20, default 4). */
  laneCardLimit?: number;
  /** Kanban: board levels that see the status controls (manager is always in). */
  statusManageRoles?: Array<"manager" | "editor">;
  /** Kanban: Alt+Arrow moves the focused card (default on). */
  keyboardMove?: boolean;
  /** Kanban: lane value id to its WIP limit (1..999); a warning, never a write rule. */
  wipLimits?: Record<string, number>;
  /** Kanban: field ids shown on the card face, in order; absent or empty: none. */
  cardFieldIds?: string[];
  /** Kanban: field ids that are hidden on the card face while the card has no value. */
  cardHideEmpty?: string[];
}

export interface FilterConfig {
  fieldId: string;
  operator: "is" | "is_not" | "contains" | "is_empty" | "is_not_empty";
  value: unknown;
}

export interface SortConfig {
  fieldId: string;
  direction: "asc" | "desc";
}

/** Where one Status lane stands in a paged board read. */
export interface LanePaging {
  /** Live items of the lane before any filter, as the server counted them. */
  readonly total: number;
  /** Items of the lane that pass the server filter. */
  readonly filteredTotal: number;
  /** Cursor of the lane's next page; null when every item that passes is loaded. */
  readonly nextCursor: string | null;
  /**
   * Items of the lane the cache held when the server counted. The lane's size follows a later write
   * (a drop, a create, an archive) by how far the loaded count has moved from this.
   */
  readonly baseLoaded: number;
}

/**
 * A board read in pages by Status lane: the groups carry only the first pages of each lane. Absent on a read
 * of the whole board.
 */
export interface BoardPaging {
  /** By Status option id; the lane of items without a status is `none`. */
  readonly lanes: Readonly<Record<string, LanePaging>>;
  /** Live root items of the board before the filter. */
  readonly totalItems: number;
  /** Items of the board that pass the server filter. */
  readonly filteredTotal: number;
  /** The filter and sort the pages were read under; later pages are read under the same. */
  readonly criteria: { readonly filter: string; readonly sort: string };
}

export interface FullBoardResponse {
  board: Board;
  /** Present when the groups hold only the first pages of each lane. */
  paging?: BoardPaging;
  groups: BoardGroup[];
  fieldDefinitions: FieldDefinition[];
  /**
   * The archived groups. They are not in `groups` (they leave every view), but an
   * item inside one is still opened by its link and is read-only.
   */
  archivedGroupIds?: readonly string[];
}

/** A saved view of a board: a tab with its layout and config. */
export interface SavedBoardView {
  readonly id: string;
  readonly name: string;
  readonly viewMode: ViewMode;
  readonly viewConfig: ViewConfig;
  readonly isDefault: boolean;
  /** "board": shared with the board's members. "me": its creator's alone. */
  readonly visibility: "board" | "me";
  readonly createdBy: string | null;
  readonly version: number;
}

export interface BoardViewDtoApi {
  id?: string;
  name?: string;
  /** The layout of a saved view: Table, Kanban, Calendar or Timeline. */
  type?: string | null;
  isDefault?: boolean;
  /** "Board" (shared with the board's members) or "Private" (its creator's). */
  visibility?: string;
  createdBy?: string | null;
  version?: number;
  viewMode?: string | null;
  filters?: string | Record<string, unknown> | null;
  config?: string | Record<string, unknown> | null;
}

export interface BoardDtoApi {
  id: string;
  workspaceId: string;
  title: string;
  description?: string | null;
  background: string;
  visibility: string;
  isArchived: boolean;
  memberCount: number;
  listCount: number;
  createdAt: string;
  /** Gap field (`CTR-GAP-WM-TABLE-BULK`): the caller may add items. */
  canAddItems?: boolean;
}

export interface FullBoardDtoApi {
  id: string;
  title: string;
  description?: string | null;
  background: string;
  visibility: string;
  columns?: BoardColumnDtoApi[];
  lists: ListDtoApi[];
  members: BoardMemberDtoApi[];
  /** The board is archived: it reads, every write is refused until a Manager unarchives it. */
  isArchived?: boolean;
  version?: number;
}

export interface BoardMemberDtoApi {
  userId: string;
  name: string;
  avatar?: string | null;
  role: string;
  joinedAt: string;
}
