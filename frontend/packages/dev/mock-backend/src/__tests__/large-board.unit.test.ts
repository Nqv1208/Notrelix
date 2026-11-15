import { describe, expect, it } from "vitest";
import { createNotrelixClient, endpoints } from "@notrelix/contracts";
import { defaultConfig } from "../config/mock-config";
import { MockStore } from "../state/mock-store";
import { createMockFetch } from "../transport/create-mock-fetch";

const boardId = "mock-board-it-large";
const doing = "mock-itl-status-doing";

function client() {
  const store = new MockStore({ ...defaultConfig, overlays: ["large-board"] });
  return createNotrelixClient({
    baseUrl: "http://localhost:8000/api/v1",
    fetchImpl: createMockFetch(store),
  });
}

type Full = {
  lanes?: {
    optionId: string;
    itemCount: number;
    filteredCount: number;
    nextCursor: string | null;
  }[];
  totalItems?: number;
  filteredTotal?: number;
  lists?: { boardItems?: unknown[] }[];
};

describe("the large-board overlay", () => {
  it("reads a page of each lane with the sizes of the lanes and of the board", async () => {
    const dto = await client().api.get<Full>(
      `${endpoints.boards.full(boardId)}?pageSize=10`,
    );
    expect(dto.totalItems).toBe(5000);
    expect(dto.filteredTotal).toBe(5000);
    expect(dto.lanes?.find((l) => l.optionId === doing)).toMatchObject({
      itemCount: 200,
      filteredCount: 200,
      nextCursor: "10",
    });
    expect(dto.lists?.[0]?.boardItems).toHaveLength(10 + 10 + 3 + 10);
  });

  it("counts a person's items 12 of 200 in a lane and 37 on the board, and pages a lane on", async () => {
    const api = client().api;
    const filter = encodeURIComponent(
      "c2:and~$assignee.has_any.mock-user-erp-ngoc-anh",
    );
    const dto = await api.get<Full>(
      `${endpoints.boards.full(boardId)}?pageSize=10&filter=${filter}`,
    );
    expect(dto.filteredTotal).toBe(37);
    expect(dto.lanes?.find((l) => l.optionId === doing)?.filteredCount).toBe(
      12,
    );
    const page = await api.get<{ items: unknown[]; nextCursor: string | null }>(
      `${endpoints.boards.lane(boardId, doing)}?cursor=10&limit=50`,
    );
    expect(page.items).toHaveLength(50);
    expect(page.nextCursor).toBe("60");
  });
});
