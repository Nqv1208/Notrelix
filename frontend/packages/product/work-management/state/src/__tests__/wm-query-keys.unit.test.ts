import { describe, expect, it } from "vitest";
import { assertNotrelixQueryKey } from "@notrelix/query";
import { wmQueryKeys } from "../queries/keys";

describe("QRY-010 all WM keys require workspace ID", () => {
  it("every factory produces a workspace-scoped key under the work-management namespace", () => {
    const keys = [
      wmQueryKeys.all("ws-1"),
      wmQueryKeys.list("ws-1"),
      wmQueryKeys.workspaceList("ws-1"),
      wmQueryKeys.fullBoard("ws-1", "b1"),
      wmQueryKeys.view("ws-1", "b1"),
      wmQueryKeys.groups("ws-1", "b1"),
      wmQueryKeys.columns("ws-1", "b1"),
      wmQueryKeys.cardDetail("ws-1", "c1"),
      wmQueryKeys.cardUpdates("ws-1", "c1"),
      wmQueryKeys.cardFiles("ws-1", "c1"),
      wmQueryKeys.cardComments("ws-1", "c1"),
      wmQueryKeys.cardActivity("ws-1", "c1"),
      wmQueryKeys.cardChecklists("ws-1", "c1"),
    ];

    for (const key of keys) {
      expect(key[0]).toBe("workspace");
      expect(key[1]).toBe("ws-1");
      expect(key[2]).toBe("work-management");
      expect(() => assertNotrelixQueryKey(key)).not.toThrow();
    }
  });

  it("workspace-scoped keys differ per workspace", () => {
    expect(wmQueryKeys.list("ws-1")).not.toEqual(wmQueryKeys.list("ws-2"));
    expect(wmQueryKeys.cardDetail("ws-1", "c1")).not.toEqual(
      wmQueryKeys.cardDetail("ws-2", "c1"),
    );
    expect(wmQueryKeys.fullBoard("ws-1", "b1")).not.toEqual(
      wmQueryKeys.fullBoard("ws-2", "b1"),
    );
  });

  it("entity keys differ per entity id within the same workspace", () => {
    expect(wmQueryKeys.cardDetail("ws-1", "c1")).not.toEqual(
      wmQueryKeys.cardDetail("ws-1", "c2"),
    );
    expect(wmQueryKeys.fullBoard("ws-1", "b1")).not.toEqual(
      wmQueryKeys.fullBoard("ws-1", "b2"),
    );
  });

  it("keys are deterministic for the same input", () => {
    expect(wmQueryKeys.cardDetail("ws-1", "c1")).toEqual(
      wmQueryKeys.cardDetail("ws-1", "c1"),
    );
    expect(wmQueryKeys.fullBoard("ws-1", "b1")).toEqual(
      wmQueryKeys.fullBoard("ws-1", "b1"),
    );
  });
});
