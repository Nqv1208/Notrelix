import { z } from "zod"

export const createChecklistSchema = z.object({
  title: z.string().min(1, "Title is required").max(200),
})

export type CreateChecklistSchemaInput = z.infer<typeof createChecklistSchema>

export const updateChecklistSchema = z.object({
  title: z.string().min(1, "Title is required").max(200).optional(),
  position: z.number().optional(),
})

export type UpdateChecklistSchemaInput = z.infer<typeof updateChecklistSchema>

export const createChecklistItemSchema = z.object({
  title: z.string().min(1, "Title is required").max(500),
})

export type CreateChecklistItemSchemaInput = z.infer<typeof createChecklistItemSchema>

export const updateChecklistItemSchema = z.object({
  title: z.string().min(1, "Title is required").max(500).optional(),
  isChecked: z.boolean().optional(),
  dueDate: z.string().nullable().optional(),
  assigneeId: z.string().uuid().nullable().optional(),
})

export type UpdateChecklistItemSchemaInput = z.infer<typeof updateChecklistItemSchema>
