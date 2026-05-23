import { z } from "zod"

export const workspaceViewTypeSchema = z.enum([
  "table",
  "doc",
  "kanban",
  "calendar",
  "timeline",
  "dashboard",
  "form",
  "gallery",
  "chart",
  "gantt",
])

export const workspaceViewTargetSchema = z.object({
  boardId: z.string().optional(),
  pageId: z.string().optional(),
  calendarId: z.string().optional(),
  dashboardId: z.string().optional(),
})

export const workspaceViewConfigSchema = z.object({
  groupBy: z.string().optional(),
  hiddenFields: z.array(z.string()).optional(),
  columnOrder: z.array(z.string()).optional(),
  density: z.enum(["compact", "default", "comfortable"]).optional(),
  filters: z
    .array(z.object({ fieldId: z.string(), operator: z.string(), value: z.unknown() }))
    .optional(),
  sortBy: z
    .array(z.object({ fieldId: z.string(), direction: z.enum(["asc", "desc"]) }))
    .optional(),
})

export const createWorkspaceViewSchema = z.object({
  workspaceSlug: z.string().min(1),
  name: z.string().min(1).max(120),
  type: workspaceViewTypeSchema,
  target: workspaceViewTargetSchema.optional(),
})

export const updateWorkspaceViewSchema = z.object({
  name: z.string().min(1).max(120).optional(),
  icon: z.string().min(1).max(12).optional(),
  config: workspaceViewConfigSchema.partial().optional(),
  position: z.number().optional(),
})

export type CreateWorkspaceViewSchemaInput = z.infer<typeof createWorkspaceViewSchema>
export type UpdateWorkspaceViewSchemaInput = z.infer<typeof updateWorkspaceViewSchema>
