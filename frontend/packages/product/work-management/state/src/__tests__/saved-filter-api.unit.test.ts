import { describe, expect, it, vi } from "vitest";
import type { NotrelixClient } from "@notrelix/contracts";
import {
  parseSavedFilterCriteria,
  serializeSavedFilterCriteria,
} from "@notrelix/work-management-core";
import { createSavedFilterApi } from "../api/saved-filter.api";

function apiWith(methods: {
  get?: unknown;
  post?: unknown;
  patch?: unknown;
  delete?: unknown;
}) {
  return createSavedFilterApi({ api: methods } as unknown as NotrelixClient);
}

describe("createSavedFilterApi", () => {
  it("reads a board's filters with who may change them and what they keep", async () => {
    const get = vi.fn(async () => [
      {
        id: "f-1",
        boardId: "b-1",
        name: "Gấp",
        visibility: "Private",
        isMine: true,
        criteria: '{"filter":"p.high","groupBy":"status","extra":1}',
        version: 3,
      },
      {
        id: "f-2",
        name: null,
        visibility: "Public",
        isMine: false,
        criteria: "bad",
        version: 1,
      },
      { name: "no id" },
    ]);

    const rows = await apiWith({ get }).list("b-1");

    expect(get).toHaveBeenCalledWith("/boards/b-1/saved-filters");
    expect(rows).toEqual([
      {
        id: "f-1",
        boardId: "b-1",
        name: "Gấp",
        visibility: "me",
        isMine: true,
        criteria: { filter: "p.high", groupBy: "status" },
        version: 3,
      },
      {
        id: "f-2",
        boardId: "b-1",
        name: "",
        visibility: "board",
        isMine: false,
        criteria: {},
        version: 1,
      },
    ]);
  });

  it("creates a filter with the bar's criteria and who sees it, and answers its id", async () => {
    const post = vi.fn(async () => "f-9");

    const id = await apiWith({ post }).create("b-1", {
      name: "Mới",
      criteria: { filter: "o" },
      visibility: "board",
    });

    expect(id).toBe("f-9");
    expect(post).toHaveBeenCalledWith("/boards/b-1/saved-filters", {
      name: "Mới",
      criteria: '{"v":2,"filter":"o"}',
      visibility: 1,
    });
  });

  it("sends the version the member saw with every change", async () => {
    const patch = vi.fn(async () => undefined);
    const del = vi.fn(async () => undefined);
    const post = vi.fn(async () => undefined);
    const api = apiWith({ patch, delete: del, post });

    await api.rename("f-1", "Tên", 4);
    await api.setVisibility("f-1", "me", 5);
    await api.remove("f-1", 6);
    await api.restore("f-1");

    expect(patch).toHaveBeenNthCalledWith(1, "/saved-filters/f-1/rename", {
      name: "Tên",
      expectedVersion: 4,
    });
    expect(patch).toHaveBeenNthCalledWith(2, "/saved-filters/f-1/visibility", {
      visibility: 0,
      expectedVersion: 5,
    });
    expect(del).toHaveBeenCalledWith("/saved-filters/f-1?expectedVersion=6");
    expect(post).toHaveBeenCalledWith("/saved-filters/f-1/restore", {});
  });
});

describe("saved filter criteria", () => {
  it("round-trips the filter and grouping and drops anything else", () => {
    const json = serializeSavedFilterCriteria({
      filter: "p.low",
      groupBy: "g",
    });
    expect(parseSavedFilterCriteria(json)).toEqual({
      filter: "p.low",
      groupBy: "g",
    });
    expect(parseSavedFilterCriteria('{"filter":3,"groupBy":""}')).toEqual({});
    expect(parseSavedFilterCriteria("[1]")).toEqual({});
    expect(parseSavedFilterCriteria(null)).toEqual({});
  });
});
