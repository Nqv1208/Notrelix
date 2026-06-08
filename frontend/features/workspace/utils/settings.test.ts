import { describe, expect, test } from "bun:test"
import type { WorkspaceView } from "../types"
import { parseSettings, stringifySettings } from "./settings"

const tableView = {
  id: "table",
  workspaceId: "workspace-a",
  name: "Main Table",
  type: "table",
  icon: "Table",
  description: "Workspace tasks",
  target: { boardId: "board-a" },
  config: {},
  visibility: "workspace",
  isDefault: true,
  position: 1,
  createdAt: "2026-01-01T00:00:00.000Z",
} satisfies WorkspaceView

describe("workspace settings helpers", () => {
  test("parses custom views and custom view order from workspace settings JSON", () => {
    const settings = parseSettings(JSON.stringify({
      customViews: [tableView],
      customViewsOrder: ["dashboard", "table"],
      unrelated: true,
    }))

    expect(settings.customViews).toEqual([tableView])
    expect(settings.customViewsOrder).toEqual(["dashboard", "table"])
  })

  test("returns an empty settings object when settings JSON is missing or invalid", () => {
    expect(parseSettings(undefined)).toEqual({})
    expect(parseSettings(null)).toEqual({})
    expect(parseSettings("{not-json")).toEqual({})
  })

  test("stringifies settings without dropping workspace view fields", () => {
    expect(stringifySettings({ customViews: [tableView], customViewsOrder: ["table"] })).toBe(
      JSON.stringify({ customViews: [tableView], customViewsOrder: ["table"] })
    )
  })
})
