export interface ChecklistItem {
  id: string
  title: string
  isDone: boolean
  position: number
}

export interface Checklist {
  id: string
  title: string
  items: ChecklistItem[]
  position: number
}

export interface ChecklistDtoApi {
  id: string
  title: string
  position: number
  items: ChecklistItemDtoApi[]
}

export interface ChecklistItemDtoApi {
  id: string
  title: string
  isChecked: boolean
  dueDate?: string | null
  assigneeId?: string | null
  position: number
}
