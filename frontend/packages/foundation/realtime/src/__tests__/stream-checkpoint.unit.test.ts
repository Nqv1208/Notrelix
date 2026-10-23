import { describe, expect, it } from "vitest";
import { StreamCheckpointTracker } from "../transport/stream-checkpoint";

describe("StreamCheckpointTracker", () => {
  it("the first observation of a stream sets the checkpoint", () => {
    const tracker = new StreamCheckpointTracker();

    expect(tracker.observe("s1", 17)).toEqual({ kind: "first" });
    expect(tracker.get("s1")).toBe(17);
  });

  it("accepts last + 1", () => {
    const tracker = new StreamCheckpointTracker();
    tracker.observe("s1", 1);

    expect(tracker.observe("s1", 2)).toEqual({ kind: "next" });
    expect(tracker.get("s1")).toBe(2);
  });

  it("classifies a skipped version as a gap and rebases", () => {
    const tracker = new StreamCheckpointTracker();
    tracker.observe("s1", 1);

    expect(tracker.observe("s1", 5)).toEqual({
      kind: "recover",
      reason: "gap",
      expected: 2,
      received: 5,
    });
    expect(tracker.get("s1")).toBe(5);
    expect(tracker.observe("s1", 6)).toEqual({ kind: "next" });
  });

  it("classifies the same version again as a duplicate", () => {
    const tracker = new StreamCheckpointTracker();
    tracker.observe("s1", 3);

    expect(tracker.observe("s1", 3)).toEqual({
      kind: "recover",
      reason: "duplicate",
      expected: 4,
      received: 3,
    });
  });

  it("classifies a lower version as a reset and rebases to it", () => {
    const tracker = new StreamCheckpointTracker();
    tracker.observe("s1", 10);

    expect(tracker.observe("s1", 1)).toEqual({
      kind: "recover",
      reason: "reset",
      expected: 11,
      received: 1,
    });
    expect(tracker.observe("s1", 2)).toEqual({ kind: "next" });
  });

  it("keeps streams independent", () => {
    const tracker = new StreamCheckpointTracker();
    tracker.observe("a", 1);
    tracker.observe("b", 100);

    expect(tracker.observe("a", 2)).toEqual({ kind: "next" });
    expect(tracker.observe("b", 101)).toEqual({ kind: "next" });
    expect(tracker.observe("a", 4).kind).toBe("recover");
    expect(tracker.get("b")).toBe(101);
  });

  it("resetByPrefix and clear forget checkpoints so the next event is a first", () => {
    const tracker = new StreamCheckpointTracker();
    tracker.observe("sub-1|s1", 4);
    tracker.observe("sub-2|s1", 9);

    tracker.resetByPrefix("sub-1|");
    expect(tracker.observe("sub-1|s1", 50)).toEqual({ kind: "first" });
    expect(tracker.get("sub-2|s1")).toBe(9);

    tracker.clear();
    expect(tracker.observe("sub-2|s1", 1)).toEqual({ kind: "first" });
  });
});
