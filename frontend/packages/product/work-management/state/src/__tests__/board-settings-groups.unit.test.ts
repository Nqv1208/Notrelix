import { describe, expect, it, vi } from "vitest";
import {
  createBoardSettingsApi,
  mapBoardSettings,
} from "../api/board-settings.api";

/** Groups of one board as the read lists them: keys out of order, two sharing a key. */
const lists = [
  { id: "g-c", title: "Kiểm thử", position: "a1", version: 3 },
  { id: "g-b", title: "Dữ liệu", position: "a0V", version: 1 },
  { id: "g-z", title: "Go-live", position: "a2", version: 1 },
  { id: "g-y", title: "Bàn giao", position: "a2", version: 1 },
  { id: "g-a", title: "Chuẩn bị", position: "Zz", version: 2 },
  {
    id: "g-old",
    title: "Lưu trữ",
    position: "a0",
    version: 1,
    isArchived: true,
  },
];

const orderOf = (rows: readonly unknown[]) =>
  mapBoardSettings("b1", { id: "b1", lists: rows } as never).groups.map(
    (group) => group.id,
  );

describe("the order of a board's groups", () => {
  it("follows the ordering key by code unit, as the server's ordinal collation does", () => {
    // "Zz" sorts before "a0V" by code unit (uppercase first), as Postgres' "C" collation and
    // FractionalIndex.CompareTo do; a locale compare would put it last.
    expect(orderOf(lists)).toEqual(["g-a", "g-b", "g-c", "g-y", "g-z"]);
  });

  it("is the same whatever order the read lists the groups in", () => {
    const want = orderOf(lists);
    const reversed = [...lists].reverse();
    const rotated = [...lists.slice(3), ...lists.slice(0, 3)];
    expect(orderOf(reversed)).toEqual(want);
    expect(orderOf(rotated)).toEqual(want);
  });

  it("keeps archived groups out of the order and lists them on their own, also by key", () => {
    const settings = mapBoardSettings("b1", { id: "b1", lists } as never);
    expect(settings.groups.map((group) => group.id)).not.toContain("g-old");
    expect(settings.archivedGroups.map((group) => group.id)).toEqual(["g-old"]);
  });
});

describe("moving a group", () => {
  function api() {
    const post = vi.fn(async () => undefined);
    const patch = vi.fn(async () => undefined);
    const client = { api: { post, patch, get: vi.fn(), delete: vi.fn() } };
    return {
      settingsApi: createBoardSettingsApi(client as never),
      post,
      patch,
    };
  }
  const settings = mapBoardSettings("b1", { id: "b1", lists } as never);

  it("names the moved group's version and its new neighbours, nothing else", async () => {
    const { settingsApi, post } = api();
    // Kiểm thử (g-c) moves to the top.
    await settingsApi.reorderGroups(settings, [
      "g-c",
      "g-a",
      "g-b",
      "g-y",
      "g-z",
    ]);
    expect(post).toHaveBeenCalledTimes(1);
    expect(post).toHaveBeenCalledWith(
      "/board-groups/g-c/move",
      { expectedVersion: 3, previousSiblingId: null, nextSiblingId: "g-a" },
      expect.anything(),
    );
  });

  it("sends nothing when the order is the one already shown", async () => {
    const { settingsApi, post } = api();
    await settingsApi.reorderGroups(
      settings,
      settings.groups.map((group) => group.id),
    );
    expect(post).not.toHaveBeenCalled();
  });
});
