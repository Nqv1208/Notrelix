import { describe, expect, it } from "vitest";
import type { ActivityLogResponseApi } from "@notrelix/work-management-core";
import { mapActivityResponse } from "../api/board-api-mappers";

function response(
  data: ActivityLogResponseApi["data"],
): ActivityLogResponseApi {
  return { data, total: data.length, page: 1, pageSize: 20 };
}

describe("mapActivityResponse", () => {
  it("carries the stable type and the change of a history line", () => {
    const [line] = mapActivityResponse(
      response([
        {
          id: "a1",
          actorId: "u1",
          actorName: "An",
          action: "changed the status from To do to Doing",
          resourceType: "work-management.board-item",
          resourceId: "c1",
          createdAt: "2026-10-27T08:30:00Z",
          type: "work.item-status-changed",
          change: { fieldName: "Status", from: "To do", to: "Doing" },
        },
      ]),
      "c1",
    );

    expect(line).toMatchObject({
      id: "a1",
      cardId: "c1",
      actor: "An",
      code: "work.item-status-changed",
      change: { fieldName: "Status", from: "To do", to: "Doing" },
    });
  });

  it("marks the creation line and names an unknown actor", () => {
    const [line] = mapActivityResponse(
      response([
        {
          id: "a0",
          actorId: "00000000-0000-0000-0000-000000000000",
          actorName: null,
          action: "created this item",
          resourceType: "work-management.board-item",
          resourceId: "c1",
          createdAt: "2026-10-26T08:30:00Z",
          kind: "created",
          type: "work.item-created",
        },
      ]),
      "c1",
    );

    expect(line?.type).toBe("created");
    expect(line?.actor).toBe("Unknown");
    expect(line?.change).toBeUndefined();
  });

  it("keeps the order the server sent (newest first)", () => {
    const lines = mapActivityResponse(
      response(
        ["b", "a"].map((id) => ({
          id,
          actorId: "u1",
          action: "archived this item",
          resourceType: "work-management.board-item",
          resourceId: "c1",
          createdAt: "2026-10-27T08:30:00Z",
        })),
      ),
      "c1",
    );

    expect(lines.map((line) => line.id)).toEqual(["b", "a"]);
  });
});
