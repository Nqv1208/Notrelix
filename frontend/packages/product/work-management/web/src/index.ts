/**
 * @notrelix/wm-web — Work Management web UI components
 *
 * React components for board views, card details, and work management UI.
 * Depends on @notrelix/work-management-core and @notrelix/work-management-state.
 */

// Board views
export * from "./components/board-workspace-view-content";

// Board layout
export * from "./components/board-layout/board-layout-shell";
export { BoardTrashHost } from "./components/item-lifecycle/board-trash-host";
export { BoardArchiveHost } from "./components/item-lifecycle/board-archive-host";
export { itemLifecycleCatalog } from "./components/item-lifecycle/item-lifecycle.catalog";
export { itemActionsCatalog } from "./components/item-actions/item-actions.catalog";
export * from "./components/board-layout/board-toolbar";
export * from "./components/board-layout/view-tabs";
export * from "./components/board-layout/board-header-surface";
export { BoardConnectionNotice, type BoardConnectionState } from "./components/board-layout/board-connection-notice";
export { BoardHeaderDataContext } from "./components/board-layout/board-header-data-context";
export * from "./components/board-lifecycle/retired-boards-surface";
export { retiredBoardsCatalog } from "./components/board-lifecycle/retired-boards.catalog";
export { useRetiredBoardsCopy } from "./components/board-lifecycle/use-retired-boards-copy";
export { boardHeaderCatalog } from "./components/board-layout/board-header.catalog";
export {
  BoardCardCreate,
  type BoardCardCreateBoard,
} from "./components/card-popup/create/board-card-create";
export {
  boardHeaderCopy,
  type BoardHeaderCopy,
} from "./components/board-layout/board-header.copy";
export { BoardHeaderCopyContext } from "./components/board-layout/board-header-copy-context";
export { useBoardHeaderCopy } from "./components/board-layout/use-board-header-copy";
export { boardShareCatalog } from "./components/board-share/board-share.catalog";
export {
  boardShareCopy,
  fillBoardShareCopy,
  type BoardShareCopy,
} from "./components/board-share/board-share.copy";
export { useBoardShareCopy } from "./components/board-share/use-board-share-copy";
export * from "./components/board-share/board-share-models";
export {
  BoardShareDialogSurface,
  type BoardShareDialogSurfaceProps,
  type ShareCandidate,
} from "./components/board-share/board-share-dialog-surface";

// Kanban view
export * from "./components/views/kanban/kanban-view";
export * from "./components/views/kanban/kanban-board";
export * from "./components/views/kanban/kanban-column";
export * from "./components/views/kanban/kanban-card";
export { kanbanCardCatalog } from "./components/views/kanban/kanban-card.catalog";
export {
  kanbanCardCopy,
  type KanbanCardCopy,
} from "./components/views/kanban/kanban-card.copy";
export { useKanbanCardCopy } from "./components/views/kanban/use-kanban-card-copy";
export { kanbanLaneCatalog } from "./components/views/kanban/kanban-lane.catalog";
export {
  kanbanLaneCopy,
  type KanbanLaneCopy,
} from "./components/views/kanban/kanban-lane.copy";
export { useKanbanLaneCopy } from "./components/views/kanban/use-kanban-lane-copy";
export * from "./components/views/kanban/kanban-dnd";
export * from "./components/views/kanban/kanban-add-card";
export * from "./components/views/kanban/kanban-card-detail-panel";
export * from "./components/views/kanban/kanban-empty-state";
export * from "./components/views/kanban/kanban-filter";
export * from "./components/views/kanban/kanban-skeleton";
export * from "./components/views/kanban/kanban-toolbar";
export { kanbanToolbarCatalog } from "./components/views/kanban/kanban-toolbar.catalog";
export { savedFiltersCatalog } from "./components/saved-filters/saved-filters.catalog";
export { viewStateCatalog } from "./components/filters/filters.catalog";
export * from "./components/filters/view-state-bar";
export * from "./components/filters/view-state-control";
export * from "./components/filters/use-view-state";
export * from "./components/filters/use-board-view-state";
export * from "./components/filters/use-filters-copy";
export {
  kanbanToolbarCopy,
  type KanbanToolbarCopy,
} from "./components/views/kanban/kanban-toolbar.copy";
export { useKanbanToolbarCopy } from "./components/views/kanban/use-kanban-toolbar-copy";

// Status option form (name, colour, group of a Status or Select option)
export * from "./components/status-option/status-option-form";
export * from "./components/status-option/status-option-palette";
export { statusOptionFormCatalog } from "./components/status-option/status-option-form.catalog";
export {
  statusOptionFormCopy,
  type StatusOptionFormCopy,
} from "./components/status-option/status-option-form.copy";
export { useStatusOptionFormCopy } from "./components/status-option/use-status-option-form-copy";

// Table view
export * from "./components/views/table/main-table-view";
export * from "./components/views/table/main-table-surface";
export * from "./components/views/table/table-toolbar";
export { mainTableCatalog } from "./components/views/table/main-table.catalog";
export {
  mainTableCopy,
  type MainTableCopy,
} from "./components/views/table/main-table.copy";
export { useMainTableCopy } from "./components/views/table/use-main-table-copy";

// Timeline view
export * from "./components/views/timeline/board-timeline-view";
export * from "./components/views/timeline/board-timeline-surface";
export { timelineCatalog } from "./components/views/timeline/timeline.catalog";
export {
  timelineCopy,
  type TimelineCopy,
} from "./components/views/timeline/timeline.copy";
export { useTimelineCopy } from "./components/views/timeline/use-timeline-copy";

// Calendar view
export * from "./components/views/calendar/board-calendar-view";
export * from "./components/views/calendar/board-calendar-surface";
export { calendarCatalog } from "./components/views/calendar/calendar.catalog";
export {
  calendarCopy,
  type CalendarCopy,
} from "./components/views/calendar/calendar.copy";
export { useCalendarCopy } from "./components/views/calendar/use-calendar-copy";

// Card detail
export {
  BoardItemSheetHost,
  type BoardItemSheetHostProps,
} from "./components/shared/board-item-sheet-host";
export { itemSheetCatalog } from "./components/card-detail/item-sheet.catalog";
export { cardPopupCatalog } from "./components/card-popup/card-popup.catalog";
export { itemChecklistsCatalog } from "./components/checklists/item-checklists.catalog";
export {
  itemSheetCopy,
  type ItemSheetCopy,
} from "./components/card-detail/item-sheet.copy";
export { useItemSheetCopy } from "./components/card-detail/use-item-sheet-copy";

// Item page (full-width view of one item)
export * from "./components/item-page/item-page-types";
export { itemPageCatalog } from "./components/item-page/item-page.catalog";
export {
  itemPageCopy,
  type ItemPageCopy,
} from "./components/item-page/item-page.copy";
export { useItemPageCopy } from "./components/item-page/use-item-page-copy";

export * from "./components/board-settings/board-settings-container";
export * from "./components/board-settings/board-settings-surface";
export * from "./components/board-settings/board-settings-types";
export { boardSettingsCatalog } from "./components/board-settings/board-settings.catalog";
export {
  boardSettingsCopy,
  type BoardSettingsCopy,
} from "./components/board-settings/board-settings.copy";
export { useBoardSettingsCopy } from "./components/board-settings/use-board-settings-copy";
export { formsCatalog } from "./components/forms/forms.catalog";
export {
  FormBuilderPage,
  FormFillPage,
  FormInboxPage,
  formLink,
} from "./components/forms/forms-containers";

// Card popup capabilities (cover, repeat, reminder, vote, viewers, colour-blind labels, checklist copy, links, face fields)
export * from "./components/card-popup/capabilities";
