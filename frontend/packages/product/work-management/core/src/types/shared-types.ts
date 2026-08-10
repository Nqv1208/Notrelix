export interface DragItem {
  type: "card" | "column" | "group";
  id: string;
  sourceGroupId: string;
  sourceIndex: number;
}

export interface BoardTableGroupState {
  id: string;
  isCollapsed: boolean;
}

export interface BoardTableSelectionState {
  selectedCardIds: string[];
  isAllSelected: boolean;
}

export interface BoardTableDraftCard {
  listId: string;
  title: string;
}
