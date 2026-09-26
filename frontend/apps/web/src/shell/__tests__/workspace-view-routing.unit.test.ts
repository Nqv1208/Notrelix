import { describe, expect, it } from "vitest";
import type { WorkspaceView } from "@notrelix/features-workspace/core";
import { resolveActiveWorkspaceView } from "../workspace-view-routing";

function view(
  overrides: Partial<WorkspaceView> & Pick<WorkspaceView, "id" | "type">,
): WorkspaceView {
  return {
    workspaceId: "ws-1",
    name: overrides.id,
    icon: "▦",
    description: "Test view",
    target: {},
    config: {},
    visibility: "workspace",
    isDefault: false,
    position: 0,
    createdAt: "2026-01-01T00:00:00.000Z",
    ...overrides,
  };
}

describe("resolveActiveWorkspaceView", () => {
  const views = [
    view({ id: "dashboard", type: "dashboard" }),
    view({ id: "board", type: "kanban", target: { boardId: "board-1" } }),
    view({ id: "doc", type: "doc", target: { pageId: "doc-1" } }),
  ];

  it("matches the dashboard explicitly", () => {
    expect(
      resolveActiveWorkspaceView(views, "/workspaces/ws-1/dashboard")?.id,
    ).toBe("dashboard");
  });

  it("matches board and document targets", () => {
    expect(
      resolveActiveWorkspaceView(views, "/workspaces/ws-1/boards/board-1")?.id,
    ).toBe("board");
    expect(
      resolveActiveWorkspaceView(views, "/workspaces/ws-1/docs/doc-1")?.id,
    ).toBe("doc");
  });

  it("does not select an arbitrary first view for an unmatched route", () => {
    expect(
      resolveActiveWorkspaceView(views, "/workspaces/ws-1/unknown"),
    ).toBeNull();
  });
});
