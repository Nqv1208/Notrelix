import { describe, expect, it } from "vitest";
import { defaultConfig } from "../config/mock-config";
import { mockIds } from "../state/mock-ids";
import { MockStore } from "../state/mock-store";

const W = mockIds.workspaces.primary;
const owner = { userId: mockIds.users.owner, workspaceRole: "owner" } as const;
const member = { userId: "someone", workspaceRole: "member" } as const;
const NOTHING = { boards: 0, pages: 0 };

function setup() {
  const store = new MockStore({ ...defaultConfig });
  const now = store.now();
  const empty = store.spaces
    .spacesOf(W)
    .find((s) => store.spaces.spacesOf(W).every((o) => o.parentId !== s.id));
  return { store, now, empty: empty! };
}

describe("deleting and restoring a department in the mock world", () => {
  it("refuses a stale version, then deletes, lists and restores with the version moving twice", () => {
    const { store, now, empty } = setup();
    const before = empty.version;
    expect(
      store.spaces.delete(W, empty.id, owner, NOTHING, before + 3, now),
    ).toEqual({ kind: "stale" });
    expect(
      store.spaces.delete(W, empty.id, owner, NOTHING, before, now),
    ).toEqual({ kind: "ok" });
    expect(store.spaces.getSpace(empty.id)).toBeUndefined();
    expect(store.spaces.deletedOf(W, now).map((d) => d.record.id)).toContain(
      empty.id,
    );
    expect(store.spaces.restore(W, empty.id, owner, now)).toEqual({
      kind: "ok",
    });
    expect(store.spaces.getSpace(empty.id)?.version).toBe(before + 2);
    // A second restore of a live department changes nothing.
    expect(store.spaces.restore(W, empty.id, owner, now)).toEqual({
      kind: "ok",
    });
    expect(store.spaces.getSpace(empty.id)?.version).toBe(before + 2);
  });

  it("refuses a department with content and a restore by a plain member", () => {
    const { store, now, empty } = setup();
    const parent = store.spaces.getSpace(mockIds.spaces.engineering)!;
    expect(
      store.spaces.delete(W, parent.id, owner, NOTHING, parent.version, now),
    ).toMatchObject({ kind: "rule" });
    expect(
      store.spaces.delete(
        W,
        empty.id,
        owner,
        { boards: 1, pages: 0 },
        empty.version,
        now,
      ),
    ).toMatchObject({ kind: "rule" });
    store.spaces.delete(W, empty.id, owner, NOTHING, empty.version, now);
    expect(store.spaces.restore(W, empty.id, member, now)).toEqual({
      kind: "not-found",
    });
  });

  it("drops a department from the list and refuses its restore after thirty days", () => {
    const { store, now, empty } = setup();
    store.spaces.delete(W, empty.id, owner, NOTHING, empty.version, now);
    const later = "2026-11-20T03:40:00.000Z";
    expect(store.spaces.deletedOf(W, later)).toEqual([]);
    expect(store.spaces.restore(W, empty.id, owner, later)).toMatchObject({
      kind: "rule",
    });
  });
});
