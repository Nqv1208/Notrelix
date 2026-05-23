import { api } from "@/lib/api/api-client"
import { endpoints } from "@/lib/api/endpoints"
import type {
  BreadcrumbItem,
  CreateCommentPayload,
  CreatePagePayload,
  Page,
  PageActivity,
  PageComment,
  PageDetail,
  SearchResult,
  UpdatePagePayload,
} from "../types"
import { blockService } from "./block.service"
import { buildPageTree } from "../utils/page-tree"

type PageDtoApi = {
  id: string
  workspaceId: string
  parentId?: string | null
  title: string
  iconType?: string | null
  iconValue?: string | null
  coverUrl?: string | null
  position: number
  depth: number
  isTemplate: boolean
  isArchived: boolean
  publishedAt?: string | null
  deadline?: string | null
  createdAt: string
  updatedAt?: string | null
}

type BreadcrumbDtoApi = {
  id: string
  title: string
  iconType?: string | null
  iconValue?: string | null
}

type CommentDtoApi = {
  id: string
  userId: string
  contentMd: string
  createdAt: string
  isEdited: boolean
}

type HistoryDtoApi = {
  id: string
  actorId: string
  action: string
  resourceTitle?: string | null
  createdAt: string
}

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

function mapComment(dto: CommentDtoApi, pageId: string): PageComment {
  return {
    id: dto.id,
    pageId,
    blockId: null,
    authorId: dto.userId,
    body: dto.contentMd,
    mentionIds: [],
    resolved: false,
    createdAt: dto.createdAt,
    updatedAt: dto.createdAt,
  }
}

function mapHistory(dto: HistoryDtoApi, pageId: string): PageActivity {
  return {
    id: dto.id,
    pageId,
    actorId: dto.actorId,
    action: "edited",
    targetLabel: dto.resourceTitle ?? dto.action,
    createdAt: dto.createdAt,
  }
}

export const pageService = {
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

  async getComments(pageId: string): Promise<PageComment[]> {
    const comments = await api.get<CommentDtoApi[]>(endpoints.pages.comments(pageId))
    return comments.map((comment) => mapComment(comment, pageId))
  },

  async createComment(payload: CreateCommentPayload): Promise<PageComment> {
    const id = await api.post<string>(endpoints.pages.comments(payload.pageId), { contentMd: payload.body })
    return {
      id,
      pageId: payload.pageId,
      blockId: payload.blockId ?? null,
      authorId: "current-user",
      body: payload.body,
      mentionIds: payload.mentionIds ?? [],
      resolved: false,
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
    }
  },

  async getHistory(pageId: string): Promise<PageActivity[]> {
    const history = await api.get<HistoryDtoApi[]>(endpoints.pages.history(pageId))
    return history.map((item) => mapHistory(item, pageId))
  },
}
