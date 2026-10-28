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
  /**
   * The item aggregate version from the board read; a move sends it as
   * `expectedVersion`. Absent on reads that do not carry it.
   */
  version?: number | null;
  /** Board item key such as "ERP-142", or a plain number on a board without a prefix. */
  itemKey?: string | null;
  /** Parent item id when this item is a child item; child items are not top-level cards. */
  parentItemId?: string | null;
  /** Selected option of the board's Status field. */
  statusOptionId?: string | null;
  /** Selected option of the board's priority field, when the read carries it. */
  priorityOptionId?: string | null;
  /** Work status category of the selected Status option. */
  statusCategory?: "todo" | "in_progress" | "review" | "done" | null;
  /** Due date and time as an ISO 8601 string. */
  dueAt?: string | null;
  /** Child item progress; a card shows it only when `total > 0`. */
  childProgress?: { done: number; total: number } | null;
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

/**
 * What a history line changed beyond its sentence (gap
 * `CTR-GAP-WM-ITEM-DETAIL`): a before and after, a link, a file or an edited
 * description.
 */
export type ActivityDetail =
  | {
      readonly kind: "diff";
      readonly label?: string;
      readonly from: string;
      readonly to: string;
      readonly note?: string;
    }
  | {
      readonly kind: "link";
      readonly relation: string;
      readonly key: string;
      readonly title: string;
    }
  | { readonly kind: "file"; readonly name: string }
  | {
      readonly kind: "description";
      readonly label: string;
      readonly added: string;
    };

/** How the "Tất cả" tab words a history line when it differs from the history tab's. */
export interface ActivityCompact {
  readonly verb: string;
  readonly target?: string;
  /** The line shows no before/after on that tab. */
  readonly hideDetail?: boolean;
}

/**
 * What a history line changed, without sentences (the backend's `ActivityChangeDto`): names and short values
 * only. The client words it in the viewer's language from the line's type.
 */
export interface ActivityChange {
  readonly fieldName?: string;
  readonly from?: string;
  readonly to?: string;
  readonly personId?: string;
  readonly personName?: string;
  readonly itemKey?: string;
}

export interface CardActivity {
  id: string;
  cardId: string;
  actor: string;
  action: string;
  type?: "created" | "updated" | "commented" | "file" | "automation" | "system";
  metadata?: Record<string, unknown>;
  createdAt: string;
  /** The rule's name when an automation made the change. */
  automationName?: string;
  /** The short verb ("đổi Hạn chót"); absent: `action` is the whole sentence. */
  verb?: string;
  /** What the verb names, drawn in the accent ("ERP-142.3"). */
  target?: string;
  /** Text after the verb on the "Tất cả" tab only ("và nhắc Trần Minh Quân"). */
  notified?: string;
  detail?: ActivityDetail;
  compact?: ActivityCompact;
  /** The stable type of the fact ("work.item-status-changed"); the client words it in its language. */
  code?: string;
  change?: ActivityChange;
}

export interface CardFile {
  id: string;
  cardId: string;
  name: string;
  size: number;
  contentType: string;
  url: string;
  source: "upload" | "r2" | "s3" | "link";
  /**
   * "file": bytes held by the API, downloaded through the content route (`url`
   * is empty); "link": opens its `url`. Absent on records that predate it.
   */
  kind?: "file" | "link";
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
  /** Files that came with the comment (gap `CTR-GAP-WM-ITEM-DETAIL`). */
  attachmentIds?: string[];
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
  /** Gap fields (`CTR-GAP-WM-ITEM-DETAIL`). */
  kind?: "created" | null;
  automationName?: string | null;
  verb?: string | null;
  target?: string | null;
  notified?: string | null;
  detail?: ActivityDetail | null;
  compact?: ActivityCompact | null;
  /** The stable type of the fact and its language-neutral detail (`GET /board-items/{id}/activity`). */
  type?: string | null;
  change?: ActivityChange | null;
  actorAvatarUrl?: string | null;
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
  /** "File" or "Link". */
  kind?: string | null;
  uploadedBy: string;
  uploadedByName?: string | null;
  createdAt: string;
}
