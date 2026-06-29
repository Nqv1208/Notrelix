import { api, type ApiRequestOptions } from "@/lib/api/api-client"
import { endpoints } from "@/lib/api/endpoints"
import type { BreadcrumbItem, CreatePagePayload, Page, PageDetail, UpdatePagePayload } from "../types/page.types"
import type { SearchResult } from "../../tree/types/tree.types"
import type { BreadcrumbDtoApi, PageDtoApi } from "../../shared/types/dto"
import { blockApi } from "../../blocks/api/block.api"
import { buildPageTree } from "../../tree/model/page-tree"
import { mapPage, mapBreadcrumb } from "../model/page.mapper"

export const pageApi = {
  async getList(workspaceId: string, options?: ApiRequestOptions): Promise<Page[]> {
    const pages = await api.get<PageDtoApi[]>(endpoints.pages.list(workspaceId), options)
    return pages.map(mapPage)
  },

  async getTree(workspaceId: string, options?: ApiRequestOptions) {
    const pages = await this.getList(workspaceId, options)
    return buildPageTree(pages)
  },

  async getDetail(pageId: string, options?: ApiRequestOptions): Promise<PageDetail> {
    const [page, blocks, breadcrumb] = await Promise.all([
      api.get<PageDtoApi>(endpoints.pages.detail(pageId), options),
      blockApi.getByPage(pageId, options),
      api.get<BreadcrumbDtoApi[]>(endpoints.pages.breadcrumb(pageId), options),
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

  async getBreadcrumb(pageId: string, options?: ApiRequestOptions): Promise<BreadcrumbItem[]> {
    const breadcrumb = await api.get<BreadcrumbDtoApi[]>(endpoints.pages.breadcrumb(pageId), options)
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

  async search(workspaceId: string, query: string, options?: ApiRequestOptions): Promise<SearchResult[]> {
    const pages = await api.get<PageDtoApi[]>(`${endpoints.pages.search(workspaceId)}?query=${encodeURIComponent(query)}`, options)
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

  async getFavorites(workspaceId: string, options?: ApiRequestOptions): Promise<Page[]> {
    const pages = await this.getList(workspaceId, options)
    return pages.filter((page) => page.isFavorited)
  },
}
