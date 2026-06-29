import { createSearchResults } from "../../tree/model/page-tree-search"
import { buildPageTree, getBreadcrumb } from "../../tree/model/page-tree"
import { mockDocsWorkspace } from "../../shared/mock/mock-data"
import type { Block, CreateBlockPayload, UpdateBlockPayload } from "../../blocks/types/block.types"
import type { CreateCommentPayload, PageComment } from "../../comments/types/comment.types"
import type { CreatePagePayload, Page, UpdatePagePayload } from "../types/page.types"

const workspace = structuredClone(mockDocsWorkspace)

const wait = async () => new Promise((resolve) => setTimeout(resolve, 120))
const now = () => new Date().toISOString()
const id = (prefix: string) => `${prefix}-${Date.now()}-${Math.random().toString(36).slice(2, 7)}`

function getPageOrThrow(pageId: string) {
  const page = workspace.pages.find((item) => item.id === pageId)
  if (!page) throw new Error(`Page ${pageId} not found`)
  return page
}

function hydratePage(page: Page) {
  return {
    ...page,
    blocks: workspace.blocks[page.id] ?? [],
    breadcrumb: getBreadcrumb(workspace.pages, page.id),
    collaborators: workspace.users.filter((user) => page.collaboratorIds.includes(user.id)),
    linkedTasks: workspace.tasks.filter((task) => page.linkedTaskIds.includes(task.id)),
    linkedBoards: workspace.boards.filter((board) => page.linkedBoardIds.includes(board.id)),
  }
}

export const mockPageService = {
  // TODO(api):
  // Replace with GET /api/v1/workspaces/:workspaceId/pages/tree.
  async getTree(workspaceId: string) {
    await wait()
    return buildPageTree(workspace.pages.filter((page) => page.workspaceId === workspaceId || page.workspaceSlug === workspaceId))
  },

  // TODO(api):
  // Replace with GET /api/v1/workspaces/:workspaceId/pages.
  async getList(workspaceId: string) {
    await wait()
    return workspace.pages
      .filter((page) => page.workspaceId === workspaceId || page.workspaceSlug === workspaceId)
      .sort((a, b) => b.lastEditedAt.localeCompare(a.lastEditedAt))
  },

  // TODO(api):
  // Replace with GET /api/v1/pages/:id.
  async getDetail(pageId: string) {
    await wait()
    return hydratePage(getPageOrThrow(pageId))
  },

  // TODO(api):
  // Replace with GET /api/v1/pages/:id/breadcrumb.
  async getBreadcrumb(pageId: string) {
    await wait()
    return getBreadcrumb(workspace.pages, pageId)
  },

  // TODO(api):
  // Replace with POST /api/v1/pages.
  async create(payload: CreatePagePayload) {
    await wait()
    const timestamp = now()
    const newPage: Page = {
      id: id("page"),
      workspaceId: payload.workspaceId,
      workspaceSlug: payload.workspaceSlug ?? payload.workspaceId,
      title: payload.title,
      icon: payload.templateId ? "🧩" : "📄",
      coverUrl: null,
      coverColor: "#e7ecff",
      parentId: payload.parentId ?? null,
      position: workspace.pages.length + 1,
      status: "draft",
      isPublished: false,
      isFavorited: false,
      isShared: false,
      tags: [],
      authorId: "u-ana",
      lastEditedById: "u-ana",
      lastEditedAt: timestamp,
      createdAt: timestamp,
      updatedAt: timestamp,
      collaboratorIds: ["u-ana"],
      metadata: {
        version: 1,
        lockOwnerId: null,
        activeUserIds: ["u-ana"],
        lastSyncedAt: timestamp,
        realtimeChannel: `docs:${payload.workspaceId}`,
        aiSummaryStatus: "idle",
      },
      linkedTaskIds: [],
      linkedBoardIds: [],
    }
    workspace.pages = [newPage, ...workspace.pages]
    workspace.blocks[newPage.id] = []
    return newPage
  },

  // TODO(api):
  // Replace with PATCH /api/v1/pages/:id.
  async update(pageId: string, payload: UpdatePagePayload) {
    await wait()
    let updated: Page | undefined
    workspace.pages = workspace.pages.map((page) => {
      if (page.id !== pageId) return page
      updated = { ...page, ...payload, updatedAt: now(), lastEditedAt: now(), lastEditedById: "u-ana" }
      return updated
    })
    return updated ?? getPageOrThrow(pageId)
  },

  // TODO(api):
  // Replace with DELETE /api/v1/pages/:id.
  async delete(pageId: string) {
    await wait()
    const childIds = new Set(workspace.pages.filter((page) => page.parentId === pageId).map((page) => page.id))
    workspace.pages = workspace.pages.filter((page) => page.id !== pageId && !childIds.has(page.id))
  },

  // TODO(api):
  // Replace with GET /api/v1/pages/:id/blocks.
  async getBlocks(pageId: string) {
    await wait()
    return [...(workspace.blocks[pageId] ?? [])].sort((a, b) => a.position - b.position)
  },

  // TODO(api):
  // Replace with POST /api/v1/pages/:id/blocks.
  async createBlock(pageId: string, payload: CreateBlockPayload) {
    await wait()
    const timestamp = now()
    const current = workspace.blocks[pageId] ?? []
    const newBlock: Block = {
      id: id("block"),
      pageId,
      type: payload.type,
      properties: payload.properties ?? { text: "" },
      position: payload.position ?? current.length + 1,
      parentId: payload.parentId ?? null,
      createdById: "u-ana",
      updatedById: "u-ana",
      createdAt: timestamp,
      updatedAt: timestamp,
    }
    workspace.blocks[pageId] = [...current, newBlock].sort((a, b) => a.position - b.position)
    return newBlock
  },

  // TODO(api):
  // Replace with PATCH /api/v1/blocks/:blockId.
  async updateBlock(blockId: string, payload: UpdateBlockPayload) {
    await wait()
    let updated: Block | undefined
    for (const pageId of Object.keys(workspace.blocks)) {
      workspace.blocks[pageId] = workspace.blocks[pageId].map((block) => {
        if (block.id !== blockId) return block
        updated = {
          ...block,
          ...payload,
          properties: { ...block.properties, ...payload.properties },
          updatedAt: now(),
          updatedById: "u-ana",
        }
        return updated
      })
    }
    if (!updated) throw new Error(`Block ${blockId} not found`)
    return updated
  },

  // TODO(api):
  // Replace mock mutation with real API call.
  // Endpoint: PATCH /api/v1/blocks/reorder
  async reorderBlocks(pageId: string, orderedBlockIds: string[]) {
    await wait()
    const blocks = workspace.blocks[pageId] ?? []
    const byId = new Map(blocks.map((block) => [block.id, block]))
    const ordered = orderedBlockIds
      .map((blockId, index) => {
        const block = byId.get(blockId)
        if (!block) return null
        return { ...block, position: index + 1, updatedAt: now(), updatedById: "u-ana" }
      })
      .filter((block): block is Block => Boolean(block))
    const missing = blocks.filter((block) => !orderedBlockIds.includes(block.id))
    workspace.blocks[pageId] = [...ordered, ...missing].sort((a, b) => a.position - b.position)
    return workspace.blocks[pageId]
  },

  async deleteBlock(blockId: string) {
    await wait()
    for (const pageId of Object.keys(workspace.blocks)) {
      workspace.blocks[pageId] = workspace.blocks[pageId].filter((block) => block.id !== blockId)
    }
  },

  async getComments(pageId: string) {
    await wait()
    return workspace.comments[pageId] ?? []
  },

  async createComment(payload: CreateCommentPayload) {
    await wait()
    const comment: PageComment = {
      id: id("comment"),
      pageId: payload.pageId,
      blockId: payload.blockId ?? null,
      authorId: "u-ana",
      body: payload.body,
      mentionIds: payload.mentionIds ?? [],
      resolved: false,
      createdAt: now(),
      updatedAt: now(),
    }
    workspace.comments[payload.pageId] = [comment, ...(workspace.comments[payload.pageId] ?? [])]
    return comment
  },

  async getHistory(pageId: string) {
    await wait()
    return workspace.activity[pageId] ?? []
  },

  async search(workspaceId: string, query: string) {
    await wait()
    return createSearchResults({
      query,
      pages: workspace.pages.filter((page) => page.workspaceId === workspaceId || page.workspaceSlug === workspaceId),
      blocks: workspace.blocks,
      tasks: workspace.tasks,
      boards: workspace.boards,
    })
  },

  async getFavorites(workspaceId: string) {
    await wait()
    return workspace.pages.filter((page) => (page.workspaceId === workspaceId || page.workspaceSlug === workspaceId) && page.isFavorited)
  },

  async getWorkspace() {
    await wait()
    return workspace
  },
}
