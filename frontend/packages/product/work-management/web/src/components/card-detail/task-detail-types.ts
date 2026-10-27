import type { ItemPageCopy } from "../item-page/item-page.copy";
import type {
  CardActivity,
  CardLabel,
  CardDetailTab,
  CardFile,
  CardUpdate,
  CreateCardUpdateInput,
  UpdateCardInput,
  UpdateFieldValueInput,
} from "@notrelix/work-management-core";

export interface TaskDetailCapabilities {
  readonly canCreateCard: boolean;
  readonly canMoveCard: boolean;
  readonly canRenameGroup: boolean;
  readonly canDelete: boolean;
  readonly canEditFields: boolean;
  /**
   * Writing comments. A commenter has it without `canEditFields`: every
   * property is read-only while the composer stays. When omitted, commenting
   * follows `canEditFields`.
   */
  readonly canComment?: boolean;
}

/** Whether the composer is available for these capabilities. */
export function canWriteComment(capabilities: TaskDetailCapabilities): boolean {
  return capabilities.canComment ?? capabilities.canEditFields;
}

/** A child item listed under "Việc con"; `done` = done category. */
export interface ItemSheetChildItem {
  readonly id: string;
  readonly title: string;
  readonly done: boolean;
  /** Item key such as "ERP-142.1"; the item page shows it when known. */
  readonly itemKey?: string | null;
  /** Due date and time (ISO 8601); the item page shows it when known. */
  readonly dueAt?: string | null;
  /** Assignee; the item page shows their mark when known. */
  readonly assignee?: {
    readonly id: string;
    readonly userId: string;
    readonly name: string;
    readonly initials: string;
  } | null;
}

/** A link between this item and another, as the item page lists it. */
export interface ItemSheetLink {
  readonly id: string;
  /** "blocked-by": this item waits for it; "blocks": it waits for this one. */
  readonly relation: "blocked-by" | "blocks" | "related";
  readonly item: {
    readonly id: string;
    readonly key: string | null;
    readonly title: string;
    readonly statusName: string | null;
    readonly statusCategory: "todo" | "in_progress" | "review" | "done" | null;
  };
}

/** An item the page can link to (the other items of the board). */
export interface ItemSheetLinkCandidate {
  readonly id: string;
  readonly key: string | null;
  readonly title: string;
  readonly statusCategory?: "todo" | "in_progress" | "review" | "done" | null;
}

/** A person who follows the item. */
export interface ItemSheetFollower {
  readonly userId: string;
  readonly name: string;
  readonly initials: string;
}

/** An approval asked about the item, with the name of the person asked. */
export interface ItemSheetApproval {
  readonly id: string;
  readonly status: "pending" | "approved" | "rejected" | "cancelled";
  readonly approverUserId: string | null;
  readonly approverName: string | null;
  /** The approver's comment with the decision. */
  readonly decisionNote?: string | null;
}

/** Board group offered by "Chuyển nhóm"; the item's own group is excluded. */
export interface ItemSheetGroup {
  readonly id: string;
  readonly title: string;
}

export type ItemSheetMarkTone =
  "blue" | "gold" | "coral" | "sage" | "crimson" | "violet";

/** Department (Space) mark shown before the board name in the crumb. */
export interface ItemSheetMark {
  readonly initials: string;
  readonly tone: ItemSheetMarkTone;
}

export interface TaskDetailData {
  readonly updates: readonly CardUpdate[];
  readonly updatesLoading: boolean;
  readonly files: readonly CardFile[];
  readonly filesLoading: boolean;
  readonly activity: readonly CardActivity[];
  readonly activityLoading: boolean;
  readonly activityFetching: boolean;
  /** Child items when the item detail carries them; the count falls back to `childProgress`. */
  readonly childItems?: readonly ItemSheetChildItem[];
  /** Whether the member follows the item; `null`/absent hides the follow chip. */
  readonly following?: boolean | null;
  /** Everyone who follows the item (the page's stack of faces). */
  readonly followers?: readonly ItemSheetFollower[];
  /** Approvals asked about the item; absent: the page offers none. */
  readonly approvals?: readonly ItemSheetApproval[];
  /** Why the last approval request was refused, when it was. */
  readonly approvalError?: "alreadyAsked" | "failed" | null;
  /** Links to other items; absent: the page says links are not available. */
  readonly links?: readonly ItemSheetLink[];
  /** Items a new link can point at. */
  readonly linkCandidates?: readonly ItemSheetLinkCandidate[];
  /** The item page's words for the links section; without it the sheet shows none. */
  readonly linksCopy?: ItemPageCopy;
  /** The labels the board offers; absent: labels cannot be edited here. */
  readonly boardLabels?: readonly CardLabel[];
}

export interface TaskDetailCallbacks {
  readonly onClose: () => void;
  readonly onRenameTitle: (cardId: string, patch: UpdateCardInput) => void;
  /**
   * Patches the item itself (title, start and due date). The item page's
   * "Ngày bắt đầu" uses it; the control stays disabled without it.
   */
  readonly onUpdateItem?: (cardId: string, patch: UpdateCardInput) => void;
  readonly onToggleWatch: (watched: boolean) => void;
  /** Asks a board member to approve the item; "Request approval" is absent without it. */
  readonly onRequestApproval?: (approverUserId: string) => void;
  /** Ticks or unticks a child item; the checkboxes stay inert without it. */
  readonly onToggleChildItem?: (childId: string, done: boolean) => void;
  /** Adds a child item with this title; the input stays inert without it. */
  readonly onAddChildItem?: (title: string) => void;
  /** Links this item to another; "Thêm" in "Liên kết" stays inert without it. */
  readonly onAddLink?: (
    itemId: string,
    relation: ItemSheetLink["relation"],
  ) => void;
  readonly onRemoveLink?: (linkId: string) => void;
  /** Registers a file chosen or dropped on the page; the drop zone stays inert without it. */
  readonly onAddFile?: (file: File) => void;
  /** Saves a stored file; a file without a public address has no download without it. */
  readonly onDownloadFile?: (file: CardFile) => void;
  readonly onDuplicate: (cardId: string) => void;
  readonly onArchive: (cardId: string) => void;
  readonly onUpdateFieldValue: (payload: UpdateFieldValueInput) => void;
  /** Puts a board label on the item or takes it off; labels stay read-only without it. */
  readonly onToggleLabel?: (labelId: string, on: boolean) => void;
  readonly onRefreshActivity: () => void;
  /**
   * Posts a comment. `onSuccess` clears the composer; `onSettled` runs after
   * success or failure so the composer can be used again (a failed post keeps
   * the text for a retry).
   */
  readonly onCreateUpdate: (
    input: CreateCardUpdateInput,
    options?: {
      readonly onSuccess?: () => void;
      readonly onSettled?: () => void;
    },
  ) => void;
  readonly onUpdateUpdate: (updateId: string, body: string) => void;
  readonly onDeleteUpdate: (updateId: string) => void;
  readonly onSelectTab: (tab: CardDetailTab) => void;
  /** Previous item in lane order ("K"); omitted at the first item. */
  readonly onPrevious?: () => void;
  /** Next item in lane order ("J"); omitted at the last item. */
  readonly onNext?: () => void;
  /** Copies the item link; the button is disabled without it. */
  readonly onCopyLink?: (cardId: string) => void;
  /** Opens the item's own page ("Mở toàn trang"); the button is disabled without it. */
  readonly onOpenFullPage?: (cardId: string) => void;
  /** Moves the item to another board group ("Chuyển nhóm"). */
  readonly onMoveToGroup?: (cardId: string, groupId: string) => void;
}
