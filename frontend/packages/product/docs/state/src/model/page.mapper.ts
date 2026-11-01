import type { Page, BreadcrumbItem, PageActivity } from "@notrelix/docs-core";
import type { PageDtoApi, BreadcrumbDtoApi, HistoryDtoApi } from "../dto";

export function mapPage(dto: PageDtoApi): Page {
  return {
    id: dto.id,
    workspaceId: dto.workspaceId,
    workspaceSlug: dto.workspaceId,
    title: dto.title,
    ...(dto.navTitle ? { navTitle: dto.navTitle } : {}),
    icon: dto.icon ?? null,
    coverUrl: dto.coverImage ?? null,
    coverColor: "var(--muted)",
    parentId: dto.parentId ?? null,
    position: 0,
    deadline: dto.deadline ?? null,
    status: dto.isArchived ? "archived" : "active",
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
      activeUserIds: dto.activeUserIds ?? [],
      lastSyncedAt: dto.updatedAt ?? dto.createdAt,
      realtimeChannel: `page:${dto.id}`,
      aiSummaryStatus: "idle",
    },
    linkedTaskIds: [],
    linkedBoardIds: [],
    version:
      typeof dto.version === "number" && dto.version > 0 ? dto.version : null,
    spaceId: dto.spaceId ?? null,
    people: dto.people ?? [],
  };
}

export function mapBreadcrumb(dto: BreadcrumbDtoApi): BreadcrumbItem {
  if (dto.unavailable || !dto.id)
    return { id: "", title: "", icon: null, unavailable: true };
  return {
    id: dto.id,
    title: dto.title ?? "",
    icon: dto.icon ?? null,
  };
}

function activityOf(action: string): PageActivity["action"] {
  const name = action.toLowerCase();
  if (name.includes("comment")) return "commented";
  if (name.includes("share")) return "shared";
  if (name.includes("move")) return "moved";
  if (name.includes("publish")) return "published";
  if (name.startsWith("create") && name.includes("page")) return "created";
  return "edited";
}

export function mapHistory(dto: HistoryDtoApi, pageId: string): PageActivity {
  return {
    id: dto.id,
    pageId,
    actorId: dto.actorId,
    action: activityOf(dto.action),
    targetLabel: dto.resourceTitle ?? "",
    ...(dto.versionName ? { versionName: dto.versionName } : {}),
    createdAt: dto.createdAt,
  };
}
