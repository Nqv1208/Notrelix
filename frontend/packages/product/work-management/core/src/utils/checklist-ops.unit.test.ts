import { describe, expect, it } from "vitest";
import type { Checklist } from "../types/checklist-types";
import {
  checklistProgressOf,
  listProgress,
  moveById,
  placementOf,
} from "./checklist-ops";

const list = (id: string, done: boolean[]): Checklist => ({
  id,
  title: id,
  position: 1,
  items: done.map((isDone, i) => ({
    id: `${id}-${i}`,
    title: "x",
    isDone,
    position: i,
  })),
});

describe("checklist-ops", () => {
  it("counts done over all items, null without any item", () => {
    expect(listProgress(list("a", [true, false, true]))).toEqual({
      done: 2,
      total: 3,
    });
    expect(
      checklistProgressOf([list("a", [true]), list("b", [false, false])]),
    ).toEqual({ done: 1, total: 3 });
    expect(checklistProgressOf([list("a", [])])).toBeNull();
    expect(checklistProgressOf([])).toBeNull();
  });

  it("moves an entry to an index and names its neighbours", () => {
    const items = [{ id: "a" }, { id: "b" }, { id: "c" }];
    const moved = moveById(items, "a", 2);
    expect(moved.map((i) => i.id)).toEqual(["b", "c", "a"]);
    expect(placementOf(moved, "a")).toEqual({
      previousSiblingId: "c",
      nextSiblingId: null,
    });
    expect(placementOf(moveById(items, "c", 0), "c")).toEqual({
      previousSiblingId: null,
      nextSiblingId: "a",
    });
    expect(moveById(items, "zzz", 0)).toEqual(items);
  });
});
