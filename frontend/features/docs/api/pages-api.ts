import { mockPageService } from "../mock/mock-page-service"
import type { CreatePagePayload, UpdatePagePayload } from "../types"

export const pagesApi = {
  // TODO(api):
  // Swap mockPageService calls for real HTTP calls from "@/lib/api/api-client".
  // Endpoints already exist in "@/lib/api/endpoints".
  getTree: (workspaceId: string) => mockPageService.getTree(workspaceId),
  getDetail: (pageId: string) => mockPageService.getDetail(pageId),
  getBreadcrumb: (pageId: string) => mockPageService.getBreadcrumb(pageId),
  getList: (workspaceId: string) => mockPageService.getList(workspaceId),
  create: (payload: CreatePagePayload) => mockPageService.create(payload),
  update: (pageId: string, payload: UpdatePagePayload) => mockPageService.update(pageId, payload),
  delete: (pageId: string) => mockPageService.delete(pageId),
  favorite: (pageId: string, isFavorited: boolean) => mockPageService.update(pageId, { isFavorited }),
  search: (workspaceId: string, query: string) => mockPageService.search(workspaceId, query),
  getFavorites: (workspaceId: string) => mockPageService.getFavorites(workspaceId),
  getComments: (pageId: string) => mockPageService.getComments(pageId),
  getHistory: (pageId: string) => mockPageService.getHistory(pageId),
}
