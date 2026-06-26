// Public API of features/work-management.
// Refactored and aligned under the final modular target architecture.

// Screen / Shell components for App Router composition
export { BoardWorkspaceRouteContent as BoardScreen } from "./boards/components/board-workspace-view-content"
export { BoardWorkspaceShell } from "./boards/components/board-workspace-shell"
export { BoardWorkspaceViewContent } from "./boards/components/board-workspace-view-content"

// Individual Board Views
export { MainTableView } from "./boards/components/views/table/main-table-view"
export { KanbanView } from "./boards/components/views/kanban/kanban-view"
export { BoardCalendarView } from "./boards/components/views/calendar/board-calendar-view"
export { BoardTimelineView } from "./boards/components/views/timeline/board-timeline-view"

// API / Config exports
export { boardApi, defaultTableViewConfig } from "./boards/api/board.api"

// Hook exports - Boards
export { useFullBoard } from "./boards/hooks/queries/use-full-board"
export { useWorkspaceBoards } from "./boards/hooks/queries/use-workspace-boards"
export { useResolvedWorkspaceBoard } from "./boards/hooks/queries/use-resolved-workspace-board"
export { useBoardView } from "./boards/hooks/state/use-board-view"
export { useBoardKanban } from "./boards/hooks/state/use-board-kanban"
export { useBoardTable } from "./boards/hooks/state/use-board-table"
export { useKanbanFilters } from "./boards/hooks/state/use-kanban-filters"
export { useKanbanSearch } from "./boards/hooks/state/use-kanban-search"
export { useKanbanColumns } from "./boards/hooks/state/use-kanban-columns"
export { useTableFilters } from "./boards/hooks/state/use-table-filters"
export { useTableSearch } from "./boards/hooks/state/use-table-search"
export { useTableSort } from "./boards/hooks/state/use-table-sort"
export { useCreateKanbanColumn } from "./boards/hooks/mutations/use-create-kanban-column"
export { useUpdateKanbanColumn } from "./boards/hooks/mutations/use-update-kanban-column"
export { useDeleteKanbanColumn } from "./boards/hooks/mutations/use-delete-kanban-column"
export { useReorderKanbanColumns } from "./boards/hooks/mutations/use-reorder-kanban-columns"

// Hook exports - Items / Cards
export { useCard } from "./items/hooks/queries/use-card"
export { useCardDetail } from "./items/hooks/queries/use-card-detail"
export { useCardActivity } from "./items/hooks/queries/use-card-activity"
export { useCardFiles } from "./items/hooks/queries/use-card-files"
export { useCardComments } from "./items/hooks/queries/use-card-comments"
export { useCreateCard } from "./items/hooks/mutations/use-create-card"
export { useCreateCardUpdate } from "./items/hooks/mutations/use-create-card-update"
export { useUpdateCard } from "./items/hooks/mutations/use-update-card"
export { useUpdateCardUpdate } from "./items/hooks/mutations/use-update-card-update"
export { useDeleteCard } from "./items/hooks/mutations/use-delete-card"
export { useDeleteCardUpdate } from "./items/hooks/mutations/use-delete-card-update"
export { useMoveCard } from "./items/hooks/mutations/use-move-card"
export { useDuplicateCard } from "./items/hooks/mutations/use-duplicate-card"
export { useUpdateFieldValue } from "./items/hooks/mutations/use-update-field-value"
export { useUploadCardFile } from "./items/hooks/mutations/use-upload-card-file"
export { useSelectedCardPanel } from "./items/hooks/state/use-selected-card-panel"

// Hook exports - Fields / Columns
export { useBoardColumns } from "./fields/hooks/queries/use-board-columns"
export { useCreateColumn } from "./fields/hooks/mutations/use-create-column"
export { useUpdateColumn } from "./fields/hooks/mutations/use-update-column"
export { useDeleteColumn } from "./fields/hooks/mutations/use-delete-column"
export { useColumnResize } from "./fields/hooks/state/use-column-resize"
export { useColumnVisibility } from "./fields/hooks/state/use-column-visibility"
export { useResizeColumn } from "./fields/hooks/state/use-resize-column"

// Hook exports - Groups / Lists
export { useBoardGroups } from "./groups/hooks/queries/use-board-groups"
export { useCreateGroup } from "./groups/hooks/mutations/use-create-group"
export { useUpdateGroup } from "./groups/hooks/mutations/use-update-group"
export { useDeleteGroup } from "./groups/hooks/mutations/use-delete-group"
export { useDuplicateGroup } from "./groups/hooks/mutations/use-duplicate-group"
export { useMoveRow } from "./groups/hooks/mutations/use-move-row"

// Hook exports - Checklists
export { useCardChecklists } from "./checklists/hooks/use-card-checklists"

// Type exports - strictly defined (no export-all)
export type {
  // Boards
  Board,
  BoardMember,
  ViewMode,
  ViewConfig,
  FilterConfig,
  SortConfig,
  FullBoardResponse,
  
  // Items / Cards
  Card,
  CardMember,
  CardDetailTab,
  CardComment,
  CardUpdate,
  CardActivity,
  CardFile,
  CardDetail,
  
  // Fields
  FieldType,
  FieldOption,
  FieldDefinition,
  FieldValue,
  BoardTableColumn,
  
  // Groups
  BoardGroup,
  
  // Checklists
  Checklist,
  ChecklistItem,
  
  // Labels
  CardLabel,
  
  // Shared / UI
  DragItem,
  BoardTableGroupState,
  BoardTableSelectionState,
  BoardTableDraftCard
} from "./types"

export type { KanbanFiltersState } from "./boards/hooks/state/use-kanban-filters"

// Shared Utils / Helpers
export { generatePosition } from "./shared/utils/fractional-index"
