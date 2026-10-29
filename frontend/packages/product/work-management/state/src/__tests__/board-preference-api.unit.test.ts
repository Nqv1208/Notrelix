import { describe, expect, it, vi } from "vitest";
import type { NotrelixClient } from "@notrelix/contracts";
import { createBoardPreferenceApi } from "../api/board-preference.api";

function apiWith(methods: { get?: unknown; put?: unknown }) {
  return createBoardPreferenceApi({
    api: methods,
  } as unknown as NotrelixClient);
}

describe("createBoardPreferenceApi", () => {
  it("reads the member's criteria of one view and parses them like a saved filter's", async () => {
    const get = vi.fn(async () => ({
      criteria: '{"v":2,"filter":"c2:and","groupBy":"status"}',
    }));
    const criteria = await apiWith({ get }).get("b-1", "v-1");
    expect(get).toHaveBeenCalledWith(
      expect.stringContaining("/boards/b-1/preferences/v-1"),
    );
    expect(criteria.filter).toBe("c2:and");
    expect(criteria.groupBy).toBe("status");
  });

  it("answers empty criteria for a view with no preference or unreadable criteria", async () => {
    expect(
      await apiWith({ get: vi.fn(async () => ({ criteria: null })) }).get(
        "b-1",
        "v-1",
      ),
    ).toEqual({});
    expect(
      await apiWith({ get: vi.fn(async () => ({ criteria: "not json" })) }).get(
        "b-1",
        "v-1",
      ),
    ).toEqual({});
  });

  it("writes the criteria JSON as the only field, and an empty string to clear them", async () => {
    const put = vi.fn(async () => undefined);
    const api = apiWith({ put });
    await api.saveCriteria("b-1", "v-1", '{"v":2}');
    await api.saveCriteria("b-1", "v-1", "");
    expect(put).toHaveBeenNthCalledWith(
      1,
      expect.stringContaining("/boards/b-1/preferences/v-1"),
      { criteria: '{"v":2}' },
    );
    expect(put).toHaveBeenNthCalledWith(2, expect.any(String), {
      criteria: "",
    });
  });
});
