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
}

export interface TaskDetailData {
  readonly updates: readonly CardUpdate[];
  readonly updatesLoading: boolean;
  readonly files: readonly CardFile[];
  readonly filesLoading: boolean;
  readonly activity: readonly CardActivity[];
  readonly activityLoading: boolean;
  readonly activityFetching: boolean;
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
}

export const defaultTaskDetailCapabilities: TaskDetailCapabilities = {
  canCreateCard: true,
  canMoveCard: true,
  canRenameGroup: true,
  canDelete: true,
  canEditFields: true,
};
