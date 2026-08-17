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
}

export interface MockPageRecord {
  id: string;
  workspaceId: string;
  title: string;
  icon?: string;
  parentId?: string;
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

export interface MockColumnRecord {
  id: string;
  boardId: string;
  name: string;
  fieldType: string;
  settings?: string;
  position: number;
  isHidden?: boolean;
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
