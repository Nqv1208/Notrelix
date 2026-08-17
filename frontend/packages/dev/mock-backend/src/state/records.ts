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
