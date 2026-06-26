import { api } from "@/lib/api/api-client"
import { endpoints } from "@/lib/api/endpoints"
import type { BoardTableColumn, FieldDefinition } from "@/features/work-management/types"

export type CreateColumnInput = {
  boardId: string
  name: string
  fieldType: FieldDefinition["fieldType"]
  settings?: Record<string, unknown>
  position?: number
}

export type UpdateColumnInput = {
  boardId: string
  columnId: string
  name?: string
  fieldType?: FieldDefinition["fieldType"]
  settings?: Record<string, unknown>
  isHidden?: boolean
}

export const columnApi = {
  async createColumn(input: CreateColumnInput): Promise<string> {
    return api.post<string>(endpoints.boards.columns(input.boardId), {
      name: input.name,
      fieldType: input.fieldType,
      settings: input.settings ? JSON.stringify(input.settings) : undefined,
      position: input.position,
    })
  },

  async updateColumn(input: UpdateColumnInput): Promise<void> {
    await api.patch<void>(endpoints.boards.column(input.boardId, input.columnId), {
      name: input.name,
      fieldType: input.fieldType,
      settings: input.settings ? JSON.stringify(input.settings) : undefined,
      isHidden: input.isHidden,
    })
  },

  async deleteColumn(boardId: string, columnId: string): Promise<void> {
    await api.delete<void>(endpoints.boards.column(boardId, columnId))
  },

  async reorderColumns(boardId: string, columns: Pick<BoardTableColumn, "id">[] | string[]): Promise<void> {
    await api.post<void>(endpoints.boards.reorderColumns(boardId), {
      items: columns.map((column, index) => ({
        id: typeof column === "string" ? column : column.id,
        newPosition: index + 1,
      })),
    })
  },
}
