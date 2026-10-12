/**
 * Kanban-specific types
 */
import type { WorkStatusCategory } from "./field-types";
import type { Card } from "./item-types";

export interface KanbanFiltersState {
  status: string[];
  priority: string[];
  assigneeId: string[];
  labelId: string[];
}

export type KanbanSortOption = "title" | "position" | "priority" | "dueDate";

/**
 * One Kanban lane: the cards whose Status option is `optionId`, in board
 * order. `optionId: null` is the lane for items without a known status; it
 * exists only when such items exist, comes last and has an empty `name` (the
 * view supplies its label). `total` counts every top-level card of the lane
 * before any view filter.
 */
export interface KanbanStatusLane {
  readonly optionId: string | null;
  readonly name: string;
  readonly category: WorkStatusCategory | null;
  readonly color: string | null;
  readonly cards: Card[];
  readonly total: number;
}
