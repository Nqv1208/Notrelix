export interface MockUserRecord {
  id: string;
  email: string;
  name: string;
  avatarUrl: string | null;
}

export interface MockWorkspaceRecord {
  id: string;
  name: string;
  slug: string;
  plan: "free" | "pro" | "business" | "enterprise";
  icon: string;
  isPersonal: boolean;
}

export interface MockMembershipRecord {
  id: string;
  workspaceId: string;
  userId: string;
  role: "owner" | "admin" | "member" | "guest";
  status: "active" | "idle" | "offline" | "in-call";
  workload: number;
  color: string;
  joinedAt: string;
}

export interface MockWorkspaceViewRecord {
  id: string;
  workspaceId: string;
  name: string;
  type: "kanban" | "table" | "doc" | "calendar" | "timeline";
  icon: string;
  description: string;
  visibility: "private" | "workspace" | "public";
  isDefault: boolean;
  position: number;
  createdAt: string;
}

export interface MockBoardRecord {
  id: string;
  workspaceId: string;
  title: string;
  description?: string;
  background: { type: "color" | "image"; value: string };
  visibility: "private" | "workspace" | "public";
  isArchived: boolean;
  createdAt: string;
  updatedAt: string;
  /** The Space the Board is placed in (OT-4); absent = workspace level. */
  spaceId?: string | null;
  /** Board aggregate version (move-to-space `expectedVersion`). */
  version?: number;
  /** Item key prefix ("ERP" gives "ERP-142"); absent = plain numbers. */
  itemKeyPrefix?: string | null;
}

export interface MockListRecord {
  id: string;
  boardId: string;
  title: string;
  color?: string;
  position: number;
  isCollapsed: boolean;
}

export interface MockCardRecord {
  id: string;
  boardId: string;
  listId: string;
  title: string;
  description?: string;
  position: number;
  createdAt: string;
  updatedAt: string;
  /** Board item key ("ERP-142"); absent on cards seeded without one. */
  itemKey?: string | null;
  /** Parent item; a child item is never a top-level card of a group. */
  parentItemId?: string | null;
  dueAt?: string | null;
  startedAt?: string | null;
  completedAt?: string | null;
  /** Assigned people (user ids), shown as the item's members. */
  assigneeUserIds?: string[];
  /** Child progress of an item whose children are not seeded as cards. */
  childProgress?: { done: number; total: number } | null;
  /** Item aggregate version (a move's `expectedVersion`); absent = 1. */
  version?: number;
  /** Archived items leave the board read; they are not deleted. */
  isArchived?: boolean;
}

export interface MockPageRecord {
  id: string;
  workspaceId: string;
  title: string;
  icon?: string;
  parentId?: string;
  /** The Space the page is placed in (OT-4); absent = workspace root. */
  spaceId?: string | null;
  /** Page aggregate version (move-to-space `expectedVersion`); absent = 1. */
  version?: number;
  /** Sibling order among pages of one parent; absent = 0. */
  position?: number;
  /** Date the document takes effect; absent = none. */
  deadline?: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface MockNotificationRecord {
  id: string;
  userId: string;
  title: string;
  message: string;
  isRead: boolean;
  createdAt: string;
}

export interface MockBoardViewRecord {
  boardId: string;
  viewMode: string;
  viewConfig: string;
  filters?: string;
}

/** An option of a Status or Select field; Status options carry a category. */
export interface MockFieldOptionRecord {
  id: string;
  name: string;
  color: string;
  /** Fractional-index ordering key. */
  position: string;
  category?: "todo" | "in_progress" | "review" | "done";
}

export interface MockColumnRecord {
  id: string;
  boardId: string;
  name: string;
  fieldType: string;
  settings?: string;
  position: number;
  isHidden?: boolean;
  /** Options of a Status or Select field, in position order. */
  options?: MockFieldOptionRecord[];
}

/** A board member (`WorkManagement.Boards.GetMembers` / `/full` members). */
export interface MockBoardMemberRecord {
  boardId: string;
  userId: string;
  role: "Owner" | "Admin" | "Member" | "Observer" | "Guest";
  joinedAt: string;
}

/** An explicit board access entry for one user (Governance grant). */
export interface MockBoardGrantRecord {
  boardId: string;
  userId: string;
  level: "viewer" | "commenter" | "editor" | "manager";
  grantedByUserId: string;
}

/**
 * The board's audience as the access read reports it. The mock cannot
 * enumerate who reaches a board, so the summary is seeded data.
 */
export interface MockBoardAudienceRecord {
  boardId: string;
  sources: {
    kind: "space" | "team" | "user" | "guest";
    id: string | null;
    name: string | null;
    count: number;
  }[];
  guestCount: number;
}

export interface MockLabelRecord {
  id: string;
  boardId: string;
  name: string;
  color: string;
}

export interface MockCardLabelRecord {
  id: string;
  cardId: string;
  labelId: string;
}

export interface MockChecklistRecord {
  id: string;
  cardId: string;
  title: string;
  position: number;
}

export interface MockChecklistItemRecord {
  id: string;
  checklistId: string;
  title: string;
  isChecked: boolean;
  dueDate?: string | null;
  assigneeId?: string | null;
  position?: number;
}

export interface MockCommentRecord {
  id: string;
  cardId: string;
  userId: string;
  contentMd: string;
  createdAt: string;
  updatedAt?: string;
}

export interface MockCardFieldValueRecord {
  id: string;
  cardId: string;
  fieldDefinitionId: string;
  value: unknown;
}

export interface MockUserPreferencesRecord {
  userId: string;
  theme: "system" | "light" | "dark";
  colorTheme: string;
  sidebarCollapsed: boolean;
  defaultView: string;
}

export interface MockBlockRecord {
  id: string;
  pageId: string;
  parentBlockId?: string | null;
  type: string;
  properties: string;
  position: number;
  version: number;
  createdByUserId: string;
  createdAt: string;
  updatedAt?: string | null;
}

export interface MockPageCommentRecord {
  id: string;
  pageId: string;
  userId: string;
  contentMd: string;
  createdAt: string;
  updatedAt?: string | null;
}

export interface MockPageHistoryRecord {
  id: string;
  pageId: string;
  actorId: string;
  action: string;
  resourceTitle?: string | null;
  createdAt: string;
}
