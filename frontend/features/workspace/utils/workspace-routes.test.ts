import { describe, expect, test } from "bun:test"
import { getWorkspaceRootHref, getWorkspaceViewHref } from "./workspace-routes"
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

  test("uses stable workspace id when building workspace view links", () => {
    const workspace: WorkspaceSummary = {
      id: "workspace-a",
      slug: "duplicated-name",
      name: "Duplicated Name",
      icon: "D",
      plan: "pro",
      memberCount: 4,
      isPersonal: false,
    }
    const view = { id: "table", type: "table" } satisfies Pick<WorkspaceView, "id" | "type">

    expect(getWorkspaceViewHref(workspace, view)).toBe("/workspace-a?view=table")
  })
})
