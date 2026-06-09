import { z } from "zod"

export const updateKanbanColumnSchema = z.object({
  title: z.string().min(1, "Title is required").max(500, "Title is too long").optional(),
  color: z.string().optional(),
  isArchived: z.boolean().optional(),
})

export type UpdateKanbanColumnInput = z.infer<typeof updateKanbanColumnSchema>
