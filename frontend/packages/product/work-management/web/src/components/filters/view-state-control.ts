import type {
  CompoundFilter,
  FieldRef,
  SavedFilterCriteria,
  SortLevel,
  ViewState,
} from "@notrelix/work-management-core";

/**
 * What the view bar needs of a member's own view state. `useViewState` returns
 * it; the pure bar, its stories and tests take it as a plain value.
 */
export interface ViewStateControl {
  /** The state with every condition, also incomplete rows (what the builder edits). */
  readonly state: ViewState;
  /** The state that applies: incomplete or invalid conditions left out. */
  readonly applied: ViewState;
  /** True once the saved criteria were read (or there is no saved view to read them from). */
  readonly loaded: boolean;
  /** The member has a view of their own: any filter, sort, grouping or swimlane. */
  readonly personal: boolean;
  setFilter(filter: CompoundFilter): void;
  setSorts(sorts: SortLevel[]): void;
  setGroup(group: FieldRef | undefined): void;
  setLane(lane: FieldRef | undefined): void;
  applyCriteria(criteria: SavedFilterCriteria): void;
  reset(): void;
  readonly persistent: boolean;
  readonly saveFailed: boolean;
  /** Why the save was not sent or was refused for good: the view is too large, or the server rejected it. No retry helps. */
  readonly saveIssue?: "too_large" | "rejected" | null;
  retrySave(): void;
}
