import type {
  CardActivity,
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

/** A child item listed under "Việc con" (WM-041); `done` = done category. */
export interface ItemSheetChildItem {
  readonly id: string;
  readonly title: string;
  readonly done: boolean;
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
}

export interface TaskDetailCallbacks {
  readonly onClose: () => void;
  readonly onRenameTitle: (cardId: string, patch: UpdateCardInput) => void;
  readonly onToggleWatch: (watched: boolean) => void;
  readonly onDuplicate: (cardId: string) => void;
  readonly onArchive: (cardId: string) => void;
  readonly onUpdateFieldValue: (payload: UpdateFieldValueInput) => void;
  readonly onRefreshActivity: () => void;
  readonly onCreateUpdate: (
    input: CreateCardUpdateInput,
    options?: { readonly onSuccess?: () => void },
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
  /** Moves the item to another board group ("Chuyển nhóm"). */
  readonly onMoveToGroup?: (cardId: string, groupId: string) => void;
}

export const defaultTaskDetailCapabilities: TaskDetailCapabilities = {
  canCreateCard: true,
  canMoveCard: true,
  canRenameGroup: true,
  canDelete: true,
  canEditFields: true,
  canComment: true,
};
