import type { ID } from "./ids.types"

export interface Mention {
  id: ID
  type: "user" | "page" | "task" | "board"
  targetId: ID
  label: string
}

export interface LinkedTask {
  id: ID
  title: string
  status: "todo" | "in_progress" | "done" | "blocked"
  dueDate: string | null
  assigneeId: ID | null
  boardId: ID
}

export interface LinkedBoard {
  id: ID
  name: string
  color: string
  openTasks: number
  doneTasks: number
}
