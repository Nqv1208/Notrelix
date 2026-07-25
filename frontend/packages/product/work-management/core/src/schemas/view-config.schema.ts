import { z } from "zod"

export const filterConfigSchema = z.object({
  fieldId: z.string(),
  operator: z.enum(["is", "is_not", "contains", "is_empty", "is_not_empty"]),
  value: z.unknown(),
})

export const sortConfigSchema = z.object({
  fieldId: z.string(),
  direction: z.enum(["asc", "desc"]),
})

export const viewConfigSchema = z.object({
  groupBy: z.string(),
  hiddenFields: z.array(z.string()),
  columnOrder: z.array(z.string()),
  columnWidths: z.record(z.string(), z.number()),
  collapsedGroups: z.record(z.string(), z.boolean()).default({}),
  filters: z.array(filterConfigSchema),
  sortBy: z.array(sortConfigSchema),
  searchQuery: z.string().optional(),
})

export type ViewConfigInput = z.infer<typeof viewConfigSchema>
