import type { Checklist, ChecklistDtoApi } from "./checklist-types";
import type { CardLabel } from "./label-types";

export interface CardMember {
  id: string;
  userId: string;
  name: string;
  initials: string;
  avatarUrl?: string;
  color: string;
}

export interface Card {
  id: string;
  listId: string;
  boardId: string;
  workspaceId: string;
  title: string;
  descriptionMd?: string;
  linkedPageId?: string;
  position: number;
  priority?: "urgent" | "high" | "medium" | "low";
  status: string;
  dueDate?: string;
  startDate?: string;
  completedAt?: string;
  isArchived: boolean;
  isDeleted: boolean;
  members: CardMember[];
  labels: CardLabel[];
  checklists: Checklist[];
  fieldValues: Record<string, unknown>;
  _count: { comments: number; attachments: number; checklistItems: number };
  createdAt: string;
  updatedAt?: string;
}

export type CardDetailTab =
  "updates" | "files" | "activity" | "linked-docs" | "subtasks";

export interface CardComment {
  id: string;
  cardId: string;
  author: string;
  body: string;
  createdAt: string;
}

export interface CardUpdate {
  id: string;
  cardId: string;
  author: CardMember;
  body: string;
  mentionUserIds: string[];
  attachmentIds: string[];
  createdAt: string;
  updatedAt?: string;
}

export interface CardActivity {
  id: string;
  cardId: string;
  actor: string;
  action: string;
  type?: "created" | "updated" | "commented" | "file" | "automation" | "system";
  metadata?: Record<string, unknown>;
  createdAt: string;
}

export interface CardFile {
  id: string;
  cardId: string;
  name: string;
  size: number;
  contentType: string;
  url: string;
  source: "upload" | "r2" | "s3" | "link";
  createdBy: CardMember;
  createdAt: string;
}

export interface CardDetail extends Card {
  boardTitle: string;
  watchers: CardMember[];
  isWatched: boolean;
  updates: CardUpdate[];
  files: CardFile[];
  activity: CardActivity[];
}

// DTOs
export interface CardSummaryDtoApi {
  id: string;
  title: string;
  priority?: string | null;
  status: string;
  dueDate?: string | null;
  cover?: string | null;
  memberCount: number;
  members: CardMemberDtoApi[];
  labels: CardLabelDtoApi[];
  checklistProgress: number;
  checklistTotal: number;
  commentCount: number;
  attachmentCount: number;
  position: number;
  fieldValues?: Record<string, unknown> | string | null;
}

export interface CardDtoApi {
  id: string;
  boardId: string;
  workspaceId: string;
  listId: string;
  title: string;
  descriptionMd?: string | null;
  linkedPageId?: string | null;
  priority?: string | null;
  status: string;
  dueDate?: string | null;
  startDate?: string | null;
  completedAt?: string | null;
  cover?: string | null;
  position: number;
  members: CardMemberDtoApi[];
  labels: CardLabelDtoApi[];
  checklists: ChecklistDtoApi[];
  commentCount: number;
  attachmentCount: number;
  fieldValues?: Record<string, unknown> | string | null;
  createdAt: string;
  updatedAt?: string | null;
}

export interface CardMemberDtoApi {
  userId: string;
  name: string;
  avatar?: string | null;
  assignedAt: string;
}

export interface CardLabelDtoApi {
  labelId: string;
  name?: string | null;
  color: string;
}

export interface CommentDtoApi {
  id: string;
  userId: string;
  userName: string;
  userAvatar?: string | null;
  contentMd: string;
  parentCommentId?: string | null;
  isEdited: boolean;
  resolvedAt?: string | null;
  createdAt: string;
}

export interface ActivityLogDtoApi {
  id: string;
  actorId: string;
  actorName?: string | null;
  action: string;
  resourceType: string;
  resourceId: string;
  resourceTitle?: string | null;
  createdAt: string;
}

export interface ActivityLogResponseApi {
  data: ActivityLogDtoApi[];
  total: number;
  page: number;
  pageSize: number;
}

export interface AttachmentDtoApi {
  id: string;
  resourceId: string;
  filename: string;
  url: string;
  sizeBytes: number;
  contentType: string;
  source: string;
  uploadedBy: string;
  uploadedByName?: string | null;
  createdAt: string;
}
