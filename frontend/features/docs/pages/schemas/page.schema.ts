import { z } from "zod"

export const docsRoleSchema = z.enum(["owner", "editor", "commenter", "viewer"])
export const pageStatusSchema = z.enum(["draft", "review", "published", "archived"])

export const docsUserSchema = z.object({
  id: z.string().min(1),
  name: z.string().min(1),
  email: z.string().email(),
  avatarUrl: z.string().url().nullable(),
  color: z.string().min(1),
  role: docsRoleSchema,
})

export const collaborativeMetadataSchema = z.object({
  version: z.number().int().nonnegative(),
  lockOwnerId: z.string().nullable(),
  activeUserIds: z.array(z.string()),
  lastSyncedAt: z.string().datetime(),
  realtimeChannel: z.string().min(1),
  aiSummaryStatus: z.enum(["idle", "queued", "ready"]),
})

export const pageSchema = z.object({
  id: z.string().min(1),
  workspaceId: z.string().min(1),
  workspaceSlug: z.string().min(1),
  title: z.string().min(1),
  icon: z.string().nullable(),
  coverUrl: z.string().url().nullable(),
  coverColor: z.string().min(1),
  parentId: z.string().nullable(),
  position: z.number(),
  status: pageStatusSchema,
  isPublished: z.boolean(),
  isFavorited: z.boolean(),
  isShared: z.boolean(),
  tags: z.array(z.string()),
  authorId: z.string(),
  lastEditedById: z.string(),
  lastEditedAt: z.string().datetime(),
  createdAt: z.string().datetime(),
  updatedAt: z.string().datetime(),
  collaboratorIds: z.array(z.string()),
  metadata: collaborativeMetadataSchema,
  linkedTaskIds: z.array(z.string()),
  linkedBoardIds: z.array(z.string()),
})

export const pageTreeNodeSchema: z.ZodType<unknown> = pageSchema.extend({
  children: z.array(z.lazy(() => pageTreeNodeSchema)),
  depth: z.number().int().nonnegative(),
})

export const breadcrumbItemSchema = z.object({
  id: z.string(),
  title: z.string(),
  icon: z.string().nullable(),
})

export const createPageSchema = z.object({
  title: z.string().min(1).max(120),
  workspaceId: z.string().min(1),
  workspaceSlug: z.string().optional(),
  parentId: z.string().nullable().optional(),
  templateId: z.string().optional(),
})

export const updatePageSchema = z.object({
  title: z.string().min(1).max(120).optional(),
  icon: z.string().nullable().optional(),
  coverUrl: z.string().url().nullable().optional(),
  coverColor: z.string().optional(),
  status: pageStatusSchema.optional(),
  isPublished: z.boolean().optional(),
  isFavorited: z.boolean().optional(),
  tags: z.array(z.string()).optional(),
})

export const commentSchema = z.object({
  id: z.string(),
  pageId: z.string(),
  blockId: z.string().nullable(),
  authorId: z.string(),
  body: z.string().min(1),
  mentionIds: z.array(z.string()),
  resolved: z.boolean(),
  createdAt: z.string().datetime(),
  updatedAt: z.string().datetime(),
})

export const activitySchema = z.object({
  id: z.string(),
  pageId: z.string(),
  actorId: z.string(),
  action: z.enum(["created", "edited", "commented", "shared", "moved", "published"]),
  targetLabel: z.string(),
  createdAt: z.string().datetime(),
})

export const searchResultSchema = z.object({
  id: z.string(),
  type: z.enum(["page", "block", "task", "board"]),
  title: z.string(),
  excerpt: z.string(),
  icon: z.string().nullable(),
  pageId: z.string().optional(),
  score: z.number(),
  group: z.enum(["Pages", "Blocks", "Tasks", "Boards"]),
})
