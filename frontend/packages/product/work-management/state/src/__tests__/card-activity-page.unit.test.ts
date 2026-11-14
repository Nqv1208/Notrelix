import { describe, expect, it, vi } from "vitest";
import type { NotrelixClient } from "@notrelix/contracts";
import { createCardApi } from "../api/item.api";

describe("getCardActivity", () => {
  it("reads the page asked for, 100 at a time", async () => {
    const get = vi.fn(async () => ({
      data: [],
      total: 0,
      page: 2,
      pageSize: 100,
    }));
    const api = createCardApi({ api: { get } } as unknown as NotrelixClient);
    await api.getCardActivity("c-1", 2);
    expect(get).toHaveBeenCalledWith(
      expect.stringMatching(/\?page=2&pageSize=100$/),
    );
    await api.getCardActivity("c-1");
    expect(get).toHaveBeenLastCalledWith(
      expect.stringMatching(/\?page=1&pageSize=100$/),
    );
  });
});
