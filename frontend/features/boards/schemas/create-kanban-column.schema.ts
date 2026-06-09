import { z } from "zod"

export const createKanbanColumnSchema = z.object({
  title: z.string().min(1, "Title is required").max(500, "Title is too long"),
  position: z.number().optional(),
  color: z.string().optional(),
})

export type CreateKanbanColumnInput = z.infer<typeof createKanbanColumnSchema>
