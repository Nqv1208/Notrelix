import { api } from "@/lib/api/api-client"
import { endpoints } from "@/lib/api/endpoints"
import type { BreadcrumbItem, CreatePagePayload, Page, PageDetail, SearchResult, UpdatePagePayload } from "../types"
import type { BreadcrumbDtoApi, PageDtoApi } from "../types/dto"
import { blockService } from "./block.service"
import { buildPageTree } from "../utils/page-tree"

function mapPage(dto: PageDtoApi): Page {
  return {
    id: dto.id,
    workspaceId: dto.workspaceId,
    workspaceSlug: dto.workspaceId,
    title: dto.title,
    icon: dto.iconValue ?? null,
    coverUrl: dto.coverUrl ?? null,
    coverColor: "var(--muted)",
    parentId: dto.parentId ?? null,
    position: dto.position,
    status: dto.isArchived ? "archived" : dto.publishedAt ? "published" : "draft",
    isPublished: Boolean(dto.publishedAt),
    isFavorited: false,
    isShared: false,
    tags: [],
    authorId: "",
    lastEditedById: "",
    lastEditedAt: dto.updatedAt ?? dto.createdAt,
    createdAt: dto.createdAt,
    updatedAt: dto.updatedAt ?? dto.createdAt,
    collaboratorIds: [],
    metadata: {
      version: 1,
      lockOwnerId: null,
      activeUserIds: [],
      lastSyncedAt: dto.updatedAt ?? dto.createdAt,
      realtimeChannel: `page:${dto.id}`,
      aiSummaryStatus: "idle",
    },
    linkedTaskIds: [],
    linkedBoardIds: [],
  }
}

function mapBreadcrumb(dto: BreadcrumbDtoApi): BreadcrumbItem {
  return {
    id: dto.id,
    title: dto.title,
    icon: dto.iconValue ?? null,
  }
}

export const pageApi = {
  async getList(workspaceId: string): Promise<Page[]> {
    const pages = await api.get<PageDtoApi[]>(endpoints.pages.list(workspaceId))
    return pages.map(mapPage)
  },

  async getTree(workspaceId: string) {
    const pages = await this.getList(workspaceId)
    return buildPageTree(pages)
  },

  async getDetail(pageId: string): Promise<PageDetail> {
    const [page, blocks, breadcrumb] = await Promise.all([
      api.get<PageDtoApi>(endpoints.pages.detail(pageId)),
      blockService.getByPage(pageId),
      api.get<BreadcrumbDtoApi[]>(endpoints.pages.breadcrumb(pageId)),
    ])
    return {
      ...mapPage(page),
      blocks,
      breadcrumb: breadcrumb.map(mapBreadcrumb),
      collaborators: [],
      linkedTasks: [],
      linkedBoards: [],
    }
  },

  async getBreadcrumb(pageId: string): Promise<BreadcrumbItem[]> {
    const breadcrumb = await api.get<BreadcrumbDtoApi[]>(endpoints.pages.breadcrumb(pageId))
    return breadcrumb.map(mapBreadcrumb)
  },

  async create(payload: CreatePagePayload): Promise<PageDetail> {
    const id = await api.post<string>(endpoints.pages.list(payload.workspaceId), {
      title: payload.title,
      parentId: payload.parentId,
    })
    return this.getDetail(id)
  },

  async update(pageId: string, payload: UpdatePagePayload): Promise<PageDetail> {
    await api.patch<void>(endpoints.pages.detail(pageId), {
      title: payload.title,
      iconValue: payload.icon,
      coverUrl: payload.coverUrl,
    })
    return this.getDetail(pageId)
  },

  async delete(pageId: string): Promise<void> {
    await api.delete<void>(endpoints.pages.detail(pageId))
  },

  async favorite(pageId: string, isFavorited: boolean): Promise<PageDetail> {
    void isFavorited
    return this.getDetail(pageId)
  },

  async search(workspaceId: string, query: string): Promise<SearchResult[]> {
    const pages = await api.get<PageDtoApi[]>(`${endpoints.pages.search(workspaceId)}?query=${encodeURIComponent(query)}`)
    return pages.map((page) => ({
      id: page.id,
      type: "page",
      title: page.title,
      excerpt: page.title,
      icon: page.iconValue ?? null,
      pageId: page.id,
      score: 1,
      group: "Pages",
    }))
  },

  async getFavorites(workspaceId: string): Promise<Page[]> {
    const pages = await this.getList(workspaceId)
    return pages.filter((page) => page.isFavorited)
  },
}
