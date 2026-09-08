import type {
  Block,
  BreadcrumbItem,
  PageActivity,
  PageComment,
  PageTreeNode,
} from "@notrelix/docs-core";

import type { DocPageScreenSurfaceProps } from "../components/doc-page-surfaces";

const BASE_TIME = "2026-01-15T10:30:00.000Z";

function pageNode(
  overrides: Partial<PageTreeNode> & Pick<PageTreeNode, "id" | "title">,
): PageTreeNode {
  return {
    id: overrides.id,
    workspaceId: "workspace-docs",
    workspaceSlug: "docs-workspace",
    title: overrides.title,
    icon: overrides.icon ?? null,
    coverUrl: null,
    coverColor: "#f8fafc",
    parentId: overrides.parentId ?? null,
    position: overrides.position ?? 0,
    status: overrides.status ?? "draft",
    isPublished: overrides.isPublished ?? false,
    isFavorited: overrides.isFavorited ?? false,
    isShared: overrides.isShared ?? false,
    tags: overrides.tags ?? [],
    authorId: overrides.authorId ?? "user-docs-owner",
    lastEditedById: overrides.lastEditedById ?? "user-docs-owner",
    lastEditedAt: overrides.lastEditedAt ?? BASE_TIME,
    createdAt: overrides.createdAt ?? BASE_TIME,
    updatedAt: overrides.updatedAt ?? BASE_TIME,
    collaboratorIds: overrides.collaboratorIds ?? [],
    metadata: overrides.metadata ?? {
      version: 1,
      lockOwnerId: null,
      activeUserIds: [],
      lastSyncedAt: BASE_TIME,
      realtimeChannel: "workspace-docs:pages",
      aiSummaryStatus: "idle",
    },
    linkedTaskIds: overrides.linkedTaskIds ?? [],
    linkedBoardIds: overrides.linkedBoardIds ?? [],
    children: overrides.children ?? [],
    depth: overrides.depth ?? 0,
  };
}

function block(overrides: Partial<Block> & Pick<Block, "id" | "type">): Block {
  return {
    id: overrides.id,
    pageId: overrides.pageId ?? "page-operating-plan",
    type: overrides.type,
    properties: overrides.properties ?? {},
    position: overrides.position ?? 0,
    parentId: overrides.parentId ?? null,
    children: overrides.children,
    createdById: overrides.createdById ?? "user-docs-owner",
    updatedById: overrides.updatedById ?? "user-docs-owner",
    createdAt: overrides.createdAt ?? BASE_TIME,
    updatedAt: overrides.updatedAt ?? BASE_TIME,
  };
}

export function docsPageTreeDefaultScenario(): PageTreeNode[] {
  return [
    pageNode({
      id: "page-operating-plan",
      title: "Operating plan",
      icon: "📘",
      isFavorited: true,
      children: [
        pageNode({
          id: "page-migration-risks",
          title: "Migration risks",
          icon: "⚠️",
          parentId: "page-operating-plan",
          depth: 1,
        }),
      ],
    }),
    pageNode({
      id: "page-release-notes",
      title: "Release notes",
      icon: "🚀",
      position: 1,
    }),
  ];
}

export function docsPageTreeEmptyScenario(): PageTreeNode[] {
  return [];
}

export function docsPageTreeEdgeDataScenario(): PageTreeNode[] {
  return [
    pageNode({
      id: "page-enterprise-rollout",
      title:
        "Enterprise rollout readiness checklist with regional localization and audit evidence",
      icon: "🧭",
      isShared: true,
      isPublished: true,
      children: [
        pageNode({
          id: "page-enterprise-rollout-apac",
          title: "APAC workspace tenant exceptions",
          icon: "🌏",
          parentId: "page-enterprise-rollout",
          depth: 1,
        }),
        pageNode({
          id: "page-enterprise-rollout-emea",
          title: "EMEA data residency review",
          icon: "🔒",
          parentId: "page-enterprise-rollout",
          depth: 1,
        }),
      ],
    }),
  ];
}

export function docsBreadcrumbDefaultScenario(): BreadcrumbItem[] {
  return [
    { id: "page-home", title: "Workspace home", icon: "🏠" },
    { id: "page-operating-plan", title: "Operating plan", icon: "📘" },
  ];
}

export function docsBreadcrumbEdgeDataScenario(): BreadcrumbItem[] {
  return [
    { id: "page-home", title: "Global Enterprise Program", icon: "🌐" },
    {
      id: "page-enterprise-rollout",
      title:
        "Enterprise rollout readiness checklist with regional localization",
      icon: "🧭",
    },
  ];
}

export function docsBlocksDefaultScenario(): Block[] {
  return [
    block({
      id: "block-heading",
      type: "heading_1",
      properties: { text: "Operating plan" },
      position: 0,
    }),
    block({
      id: "block-summary",
      type: "paragraph",
      properties: {
        text: "Coordinate launch readiness, evidence capture, and follow-up owners.",
      },
      position: 1,
    }),
    block({
      id: "block-todo",
      type: "todo",
      properties: {
        text: "Publish customer-facing migration guide",
        checked: false,
      },
      position: 2,
    }),
    block({
      id: "block-callout",
      type: "callout",
      properties: {
        icon: "💡",
        text: "Keep launch notes short enough for workspace administrators.",
      },
      position: 3,
    }),
  ];
}

export function docsBlocksEmptyScenario(): Block[] {
  return [];
}

export function docsBlocksEdgeDataScenario(): Block[] {
  return [
    block({
      id: "block-edge-heading",
      type: "heading_2",
      properties: {
        text: "Enterprise rollout readiness checklist with regional localization",
      },
      position: 0,
    }),
    block({
      id: "block-edge-code",
      type: "code",
      properties: {
        text: "notrelix rollout verify --workspace enterprise-apac --evidence required",
      },
      position: 1,
    }),
    block({
      id: "block-edge-quote",
      type: "quote",
      properties: {
        text: "Do not mark the document ready until owner review and audit evidence both pass.",
      },
      position: 2,
    }),
  ];
}

export function docsCommentsDefaultScenario(): PageComment[] {
  return [
    {
      id: "comment-1",
      pageId: "page-operating-plan",
      blockId: null,
      authorId: "nina",
      body: "Can we link this to the launch checklist?",
      mentionIds: [],
      resolved: false,
      createdAt: "2026-01-15T09:00:00.000Z",
      updatedAt: "2026-01-15T09:00:00.000Z",
    },
    {
      id: "comment-2",
      pageId: "page-operating-plan",
      blockId: "block-todo",
      authorId: "vinh",
      body: "Resolved after adding the owner evidence section.",
      mentionIds: ["nina"],
      resolved: true,
      createdAt: "2026-01-15T10:00:00.000Z",
      updatedAt: "2026-01-15T10:00:00.000Z",
    },
  ];
}

export function docsCommentsEmptyScenario(): PageComment[] {
  return [];
}

export function docsCommentsEdgeDataScenario(): PageComment[] {
  return [
    {
      id: "comment-edge",
      pageId: "page-enterprise-rollout",
      blockId: null,
      authorId: "enterprise-reviewer-with-long-name",
      body: "The regional readiness section needs explicit sign-off from security, legal, and workspace operations before this can be published.",
      mentionIds: ["security", "legal", "workspace-ops"],
      resolved: false,
      createdAt: "2026-01-15T08:15:00.000Z",
      updatedAt: "2026-01-15T08:15:00.000Z",
    },
  ];
}

export function docsHistoryDefaultScenario(): PageActivity[] {
  return [
    {
      id: "activity-created",
      pageId: "page-operating-plan",
      actorId: "vinh",
      action: "created",
      targetLabel: "Operating plan",
      createdAt: "2026-01-14T15:00:00.000Z",
    },
    {
      id: "activity-commented",
      pageId: "page-operating-plan",
      actorId: "nina",
      action: "commented",
      targetLabel: "Launch checklist",
      createdAt: "2026-01-15T09:00:00.000Z",
    },
  ];
}

export function docsHistoryEmptyScenario(): PageActivity[] {
  return [];
}

export function docsHistoryEdgeDataScenario(): PageActivity[] {
  return [
    {
      id: "activity-edge-published",
      pageId: "page-enterprise-rollout",
      actorId: "enterprise-reviewer-with-long-name",
      action: "published",
      targetLabel:
        "Enterprise rollout readiness checklist with regional localization and audit evidence",
      createdAt: "2026-01-15T11:45:00.000Z",
    },
  ];
}

export function docsPageScreenDefaultScenario(): DocPageScreenSurfaceProps {
  return {
    status: "ready",
    workspaceId: "workspace-docs",
    pageId: "page-operating-plan",
    pageTitle: "Operating plan",
    isFavorited: true,
    breadcrumbs: docsBreadcrumbDefaultScenario(),
    pages: docsPageTreeDefaultScenario(),
    blocks: docsBlocksDefaultScenario(),
    comments: docsCommentsDefaultScenario(),
    history: docsHistoryDefaultScenario(),
  };
}

export function docsPageScreenEmptyScenario(): DocPageScreenSurfaceProps {
  return {
    ...docsPageScreenDefaultScenario(),
    pageId: "page-empty",
    pageTitle: "Untitled page",
    isFavorited: false,
    breadcrumbs: [{ id: "page-empty", title: "Untitled page", icon: null }],
    pages: docsPageTreeEmptyScenario(),
    blocks: docsBlocksEmptyScenario(),
    comments: docsCommentsEmptyScenario(),
    history: docsHistoryEmptyScenario(),
  };
}

export function docsPageScreenLoadingScenario(): DocPageScreenSurfaceProps {
  return {
    ...docsPageScreenDefaultScenario(),
    status: "loading",
  };
}

export function docsPageScreenEdgeDataScenario(): DocPageScreenSurfaceProps {
  return {
    status: "ready",
    workspaceId: "workspace-enterprise",
    pageId: "page-enterprise-rollout",
    pageTitle:
      "Enterprise rollout readiness checklist with regional localization and audit evidence",
    isFavorited: false,
    breadcrumbs: docsBreadcrumbEdgeDataScenario(),
    pages: docsPageTreeEdgeDataScenario(),
    blocks: docsBlocksEdgeDataScenario(),
    comments: docsCommentsEdgeDataScenario(),
    history: docsHistoryEdgeDataScenario(),
  };
}
