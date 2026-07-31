/**
 * @notrelix/wm-web — Work Management web UI components
 * 
 * React components for board views, card details, and work management UI.
 * Depends on @notrelix/work-management-core and @notrelix/work-management-state.
 */

// Board views
export * from './components/board-workspace-view-content'

// Board layout
export * from './components/board-layout/board-layout-shell'
export * from './components/board-layout/board-toolbar'
export * from './components/board-layout/view-tabs'

// Kanban view
export * from './components/views/kanban/kanban-view'
export * from './components/views/kanban/kanban-board'
export * from './components/views/kanban/kanban-column'
export * from './components/views/kanban/kanban-card'
export * from './components/views/kanban/kanban-add-card'
export * from './components/views/kanban/kanban-add-column'
export * from './components/views/kanban/kanban-card-detail-panel'
export * from './components/views/kanban/kanban-card-menu'
export * from './components/views/kanban/kanban-column-menu'
export * from './components/views/kanban/kanban-empty-state'
export * from './components/views/kanban/kanban-filter-menu'
export * from './components/views/kanban/kanban-skeleton'
export * from './components/views/kanban/kanban-sort-menu'
export * from './components/views/kanban/kanban-toolbar'

// Table view
export * from './components/views/table/main-table-view'
export * from './components/views/table/table-add-task-row'
export * from './components/views/table/table-cell'
export * from './components/views/table/table-group-section'
export * from './components/views/table/table-header-row'
export * from './components/views/table/table-row'
export * from './components/views/table/table-scroll-container'
export * from './components/views/table/table-sticky-header'

// Timeline view
export * from './components/views/timeline/board-timeline-view'

// Calendar view
export * from './components/views/calendar/board-calendar-view'

// Card detail
export * from './components/card-detail/task-detail-panel'
export * from './components/card-detail/task-detail-header'
export * from './components/card-detail/task-detail-tabs'
export * from './components/card-detail/task-detail-empty-state'
export * from './components/card-detail/task-activity-tab'
export * from './components/card-detail/task-files-tab'
export * from './components/card-detail/task-updates-tab'
export * from './components/card-detail/update-composer'
