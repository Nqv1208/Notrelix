import { describe, expect, it } from "vitest";
import { moveCardByKey, type KanbanArrangement } from "../kanban-dnd";

const arrangement: KanbanArrangement = new Map([
  ["a", ["a1", "a2", "a3"]],
  ["b", ["b1"]],
  ["none", ["n1"]],
  ["c", []],
]);
const keys = ["none", "a", "b", "c"];

describe("moveCardByKey", () => {
  it("goes to the next lane at the card's own place, clamped", () => {
    const moved = moveCardByKey(arrangement, keys, "a3", "right");
    expect(moved?.laneKey).toBe("b");
    expect(moved?.arrangement.get("b")).toEqual(["b1", "a3"]);
    expect(moved?.arrangement.get("a")).toEqual(["a1", "a2"]);
  });

  it("never targets the lane without a value", () => {
    expect(moveCardByKey(arrangement, keys, "a1", "left")).toBeNull();
  });

  it("goes into an empty lane", () => {
    const moved = moveCardByKey(arrangement, keys, "b1", "right");
    expect(moved?.arrangement.get("c")).toEqual(["b1"]);
  });

  it("moves one place up or down and stops at the ends", () => {
    expect(
      moveCardByKey(arrangement, keys, "a2", "down")?.arrangement.get("a"),
    ).toEqual(["a1", "a3", "a2"]);
    expect(
      moveCardByKey(arrangement, keys, "a2", "up")?.arrangement.get("a"),
    ).toEqual(["a2", "a1", "a3"]);
    expect(moveCardByKey(arrangement, keys, "a1", "up")).toBeNull();
    expect(moveCardByKey(arrangement, keys, "a3", "down")).toBeNull();
  });

  it("knows no card that is not shown", () => {
    expect(moveCardByKey(arrangement, keys, "zz", "right")).toBeNull();
  });
});
