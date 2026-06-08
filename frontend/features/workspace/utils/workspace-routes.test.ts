import { describe, expect, test } from "bun:test"
import {
  getWorkspaceBoardHref,
  getWorkspaceRootHref,
  getWorkspaceViewHref,
  resolveWorkspaceTabbedActiveView,
  resolveWorkspaceTabbedRoute,
} from "./workspace-routes"
import type { WorkspaceSummary, WorkspaceView } from "../types"

describe("workspace route helpers", () => {
  test("uses stable workspace id instead of slug for workspace root links", () => {
    const workspace: WorkspaceSummary = {
      id: "workspace-123",
      slug: "product-team",
      name: "Product Team",
      icon: "P",
      plan: "business",
      memberCount: 8,
      isPersonal: false,
    }

    expect(getWorkspaceRootHref(workspace)).toBe("/workspace-123")
  })

  test("routes board-backed workspace views through board query routes", () => {
    const workspace: WorkspaceSummary = {
      id: "workspace-a",
      slug: "duplicated-name",
      name: "Duplicated Name",
      icon: "D",
      plan: "pro",
      memberCount: 4,
      isPersonal: false,
    }
    const tableView = { id: "table", type: "table", target: { boardId: "board-a" } } satisfies Pick<WorkspaceView, "id" | "type" | "target">
    const kanbanView = { id: "kanban", type: "kanban", target: { boardId: "board-a" } } satisfies Pick<WorkspaceView, "id" | "type" | "target">
    const calendarView = { id: "calendar", type: "calendar", target: { boardId: "board-a" } } satisfies Pick<WorkspaceView, "id" | "type" | "target">
    const timelineView = { id: "timeline", type: "timeline", target: { boardId: "board-a" } } satisfies Pick<WorkspaceView, "id" | "type" | "target">

    expect(getWorkspaceViewHref(workspace, tableView)).toBe("/workspace-a/boards/board-a?view=table")
    expect(getWorkspaceViewHref(workspace, kanbanView)).toBe("/workspace-a/boards/board-a?view=kanban")
    expect(getWorkspaceViewHref(workspace, calendarView)).toBe("/workspace-a/boards/board-a?view=calendar")
    expect(getWorkspaceViewHref(workspace, timelineView)).toBe("/workspace-a/boards/board-a?view=timeline")
  })

  test("routes docs views outside board routes", () => {
    const view = { id: "docs", type: "doc", target: { pageId: "page-a" } } satisfies Pick<WorkspaceView, "id" | "type" | "target">

    expect(getWorkspaceViewHref("workspace-a", view)).toBe("/workspace-a/docs/page-a")
  })

  test("routes dashboard views to the workspace dashboard route", () => {
    const view = { id: "dashboard", type: "dashboard", target: {} } satisfies Pick<WorkspaceView, "id" | "type" | "target">

    expect(getWorkspaceViewHref("workspace-a", view)).toBe("/workspace-a/dashboard")
  })

  test("defaults direct board links to the main table view", () => {
    expect(getWorkspaceBoardHref("workspace-a", "board-a")).toBe("/workspace-a/boards/board-a?view=table")
  })

  test("can route raw board tabs through the current board route", () => {
    const view = { id: "kanban", type: "kanban", target: {} } satisfies Pick<WorkspaceView, "id" | "type" | "target">

    expect(getWorkspaceViewHref("workspace-a", view, { currentBoardId: "board-a" })).toBe(
      "/workspace-a/boards/board-a?view=kanban"
    )
  })
})

describe("workspace tabbed route resolver", () => {
  test("resolves board routes from the view search parameter", () => {
    expect(resolveWorkspaceTabbedRoute("/workspace-a/boards/board-a", "workspace-a", "view=kanban")).toEqual({
      kind: "board",
      workspaceId: "workspace-a",
      boardId: "board-a",
      activeViewType: "kanban",
      viewParam: "kanban",
      contentClassName: "overflow-hidden",
      showToolbar: true,
    })
  })

  test("defaults board routes to table when the view parameter is missing or unsupported", () => {
    expect(resolveWorkspaceTabbedRoute("/workspace-a/boards/board-a", "workspace-a", "")?.activeViewType).toBe("table")
    expect(resolveWorkspaceTabbedRoute("/workspace-a/boards/board-a", "workspace-a", "view=unknown")?.activeViewType).toBe("table")
  })

  test("resolves dashboard routes as the dashboard tab", () => {
    expect(resolveWorkspaceTabbedRoute("/workspace-a/dashboard", "workspace-a", "")).toEqual({
      kind: "dashboard",
      workspaceId: "workspace-a",
      activeViewType: "dashboard",
      contentClassName: undefined,
      showToolbar: true,
    })
  })

  test("resolves docs overview and detail routes as the doc tab", () => {
    expect(resolveWorkspaceTabbedRoute("/workspace-a/docs", "workspace-a", "")).toEqual({
      kind: "docs",
      workspaceId: "workspace-a",
      activeViewType: "doc",
      pageId: undefined,
      contentClassName: undefined,
      showToolbar: false,
    })

    expect(resolveWorkspaceTabbedRoute("/workspace-a/docs/page-a", "workspace-a", "")).toEqual({
      kind: "docs",
      workspaceId: "workspace-a",
      activeViewType: "doc",
      pageId: "page-a",
      contentClassName: "overflow-hidden",
      showToolbar: true,
    })
  })

  test("resolves docs nested routes without editor-only frame options", () => {
    expect(resolveWorkspaceTabbedRoute("/workspace-a/docs/page-a/history", "workspace-a", "")).toEqual({
      kind: "docs",
      workspaceId: "workspace-a",
      activeViewType: "doc",
      pageId: "page-a",
      contentClassName: undefined,
      showToolbar: false,
    })
  })

  test("does not enable the tabbed frame for non-tabbed workspace routes", () => {
    expect(resolveWorkspaceTabbedRoute("/workspace-a/chat", "workspace-a", "")).toBeNull()
    expect(resolveWorkspaceTabbedRoute("/workspace-a/boards", "workspace-a", "")).toBeNull()
    expect(resolveWorkspaceTabbedRoute("/workspace-a", "workspace-a", "panel=settings")).toBeNull()
  })

  test("resolves active views from route metadata and creates synthetic views when needed", () => {
    const boardRoute = resolveWorkspaceTabbedRoute("/workspace-a/boards/board-a", "workspace-a", "view=kanban")
    const dashboardRoute = resolveWorkspaceTabbedRoute("/workspace-a/dashboard", "workspace-a", "")
    const docsRoute = resolveWorkspaceTabbedRoute("/workspace-a/docs/page-a", "workspace-a", "")

    expect(boardRoute && resolveWorkspaceTabbedActiveView([], boardRoute, "2026-01-01T00:00:00.000Z")).toMatchObject({
      id: "kanban",
      type: "kanban",
      target: { boardId: "board-a" },
    })
    expect(dashboardRoute && resolveWorkspaceTabbedActiveView([], dashboardRoute, "2026-01-01T00:00:00.000Z")).toMatchObject({
      id: "dashboard",
      type: "dashboard",
    })
    expect(docsRoute && resolveWorkspaceTabbedActiveView([], docsRoute, "2026-01-01T00:00:00.000Z")).toMatchObject({
      id: "doc",
      type: "doc",
      target: { pageId: "page-a" },
    })
  })
})
