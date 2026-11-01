export interface ChecklistItem {
  id: string;
  title: string;
  isDone: boolean;
  position: number;
  /** The person the item is given to ("Giao mục này"); absent: nobody. */
  assigneeId?: string | null;
  /** The item's own due time as an ISO instant; absent: none. */
  dueAt?: string | null;
}

export interface Checklist {
  id: string;
  title: string;
  /** Aggregate version of the list and its items; a write sends it as `expectedVersion`. */
  version?: number;
  items: ChecklistItem[];
  position: number;
}

export interface ChecklistDtoApi {
  id: string;
  title: string;
  position: number;
  items: ChecklistItemDtoApi[];
}

export interface ChecklistItemDtoApi {
  id: string;
  title: string;
  isChecked: boolean;
  dueDate?: string | null;
  assigneeId?: string | null;
  position: number;
}
