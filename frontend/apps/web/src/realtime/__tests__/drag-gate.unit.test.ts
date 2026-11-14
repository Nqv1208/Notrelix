// @vitest-environment jsdom
import { describe, expect, it, vi } from "vitest";
import { holdRealtimeRefetch } from "@notrelix/work-management-state";
import { createDragGate } from "../drag-gate";

describe("drag gate", () => {
  it("passes a refetch straight through when nothing is being dragged", async () => {
    const invalidate = vi.fn(async () => {});
    const gate = createDragGate(document, invalidate);
    await gate.invalidate([["a"]]);
    expect(invalidate).toHaveBeenCalledWith([["a"]]);
    gate.dispose();
  });

  it("holds refetches during a pointer drag, merges them, and runs them once after the drop", async () => {
    const invalidate = vi.fn(async () => {});
    const gate = createDragGate(document, invalidate);
    const release = holdRealtimeRefetch();
    await gate.invalidate([["a"], ["b"]]);
    await gate.invalidate([["a"]]);
    expect(invalidate).not.toHaveBeenCalled();
    release();
    expect(invalidate).toHaveBeenCalledTimes(1);
    expect(invalidate).toHaveBeenCalledWith([["a"], ["b"]]);
    gate.dispose();
  });

  it("holds during a native drag until drop", async () => {
    const invalidate = vi.fn(async () => {});
    const gate = createDragGate(document, invalidate);
    document.dispatchEvent(new Event("dragstart"));
    await gate.invalidate([["c"]]);
    expect(invalidate).not.toHaveBeenCalled();
    document.dispatchEvent(new Event("drop"));
    expect(invalidate).toHaveBeenCalledWith([["c"]]);
    gate.dispose();
  });

  it("stops listening when disposed", async () => {
    const invalidate = vi.fn(async () => {});
    const gate = createDragGate(document, invalidate);
    gate.dispose();
    document.dispatchEvent(new Event("dragstart"));
    await gate.invalidate([["d"]]);
    expect(invalidate).toHaveBeenCalledTimes(1);
  });
});
