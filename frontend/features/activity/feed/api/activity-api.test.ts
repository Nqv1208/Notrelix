import { describe, expect, test } from "bun:test"
import { mapActivityResponse } from "./activity.api"

describe("activity API mappers", () => {
  test("maps activity response into workspace activity items", () => {
    expect(mapActivityResponse({
      data: [{
        id: "activity-a",
        actorId: "user-a",
        action: "created",
        resourceTitle: null,
        createdAt: "2026-01-01T00:00:00.000Z",
      }],
    })).toEqual([{
      id: "activity-a",
      actor: "Workspace",
      action: "created",
      target: "item",
      createdAt: "2026-01-01T00:00:00.000Z",
    }])
  })
})
