/**
 * Kanban-specific types
 */

export interface KanbanFiltersState {
  status: string[]
  priority: string[]
  assigneeId: string[]
  labelId: string[]
}

export type KanbanSortOption = 'title' | 'position' | 'priority' | 'dueDate'
