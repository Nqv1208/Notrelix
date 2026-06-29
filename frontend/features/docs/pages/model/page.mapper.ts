import type { Page, BreadcrumbItem, PageActivity } from "../types/page.types"
import type { PageDtoApi, BreadcrumbDtoApi, HistoryDtoApi } from "../../shared/types/dto"

export function mapPage(dto: PageDtoApi): Page {
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

export function mapBreadcrumb(dto: BreadcrumbDtoApi): BreadcrumbItem {
  return {
    id: dto.id,
    title: dto.title,
    icon: dto.iconValue ?? null,
  }
}

export function mapHistory(dto: HistoryDtoApi, pageId: string): PageActivity {
  return {
    id: dto.id,
    pageId,
    actorId: dto.actorId,
    action: "edited",
    targetLabel: dto.resourceTitle ?? dto.action,
    createdAt: dto.createdAt,
  }
}
