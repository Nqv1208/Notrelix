import { z } from "zod"

export const blockTypeSchema = z.enum([
  "paragraph",
  "heading_1",
  "heading_2",
  "heading_3",
  "bulleted_list",
  "numbered_list",
  "todo",
  "quote",
  "divider",
  "code",
  "callout",
  "toggle",
  "image",
  "embed",
  "table",
  "board_reference",
  "page_reference",
])

export const mentionSchema = z.object({
  id: z.string(),
  type: z.enum(["user", "page", "task", "board"]),
  targetId: z.string(),
  label: z.string(),
})

export const blockPropertiesSchema = z.object({
  text: z.string().optional(),
  checked: z.boolean().optional(),
  language: z.string().optional(),
  url: z.string().url().optional(),
  caption: z.string().optional(),
  color: z.string().optional(),
  icon: z.string().optional(),
  title: z.string().optional(),
  items: z.array(z.string()).optional(),
  rows: z.array(z.array(z.string())).optional(),
  linkedPageId: z.string().optional(),
  linkedBoardId: z.string().optional(),
  linkedTaskId: z.string().optional(),
  mentionIds: z.array(z.string()).optional(),
  align: z.enum(["left", "center", "right"]).optional(),
  fontFamily: z.enum(["inter", "poppins", "serif", "mono"]).optional(),
  fontSize: z.enum(["sm", "base", "lg", "xl"]).optional(),
})

export const blockSchema = z.object({
  id: z.string(),
  pageId: z.string(),
  type: blockTypeSchema,
  properties: blockPropertiesSchema,
  position: z.number(),
  parentId: z.string().nullable(),
  children: z.array(z.unknown()).optional(),
  createdById: z.string(),
  updatedById: z.string(),
  createdAt: z.string().datetime(),
  updatedAt: z.string().datetime(),
})

export const createBlockSchema = z.object({
  type: blockTypeSchema,
  properties: blockPropertiesSchema.optional(),
  position: z.number().optional(),
  parentId: z.string().nullable().optional(),
})

export const updateBlockSchema = z.object({
  type: blockTypeSchema.optional(),
  properties: blockPropertiesSchema.optional(),
  position: z.number().optional(),
  parentId: z.string().nullable().optional(),
})
