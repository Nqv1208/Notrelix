import { describe, expect, it } from "vitest";
import { act, renderHook } from "@notrelix/testing";
import {
  tableCollapsedGroupsKey,
  useCollapsedSet,
  type CollapsedLanesStorage,
} from "../use-kanban-collapsed-lanes";

function memoryStorage(): CollapsedLanesStorage & {
  data: Map<string, string>;
} {
  const data = new Map<string, string>();
  return {
    data,
    getItem: (key) => data.get(key) ?? null,
    setItem: (key, value) => void data.set(key, value),
    removeItem: (key) => void data.delete(key),
  };
}

/** UC-WM-E05: collapsed groups are personal, kept on this device per workspace and view (D38). */
describe("useCollapsedSet (Table groups)", () => {
  it("keeps a collapsed group collapsed across a refetch and a reopen", () => {
    const storage = memoryStorage();
    const key = tableCollapsedGroupsKey("ws-1", "view-a");
    const { result, rerender, unmount } = renderHook(
      ({ k }) => useCollapsedSet(k, storage),
      { initialProps: { k: key } },
    );
    act(() => result.current[1](new Set(["status-done"])));
    expect([...result.current[0]]).toEqual(["status-done"]);
    // A refetch renders the view again with the same key: nothing re-expands.
    rerender({ k: key });
    expect([...result.current[0]]).toEqual(["status-done"]);
    unmount();
    // Reopening the view on this device reads it back.
    const reopened = renderHook(() => useCollapsedSet(key, storage));
    expect([...reopened.result.current[0]]).toEqual(["status-done"]);
  });

  it("keeps each view's groups apart and reads them again when the view changes", () => {
    const storage = memoryStorage();
    storage.setItem(
      tableCollapsedGroupsKey("ws-1", "view-b"),
      JSON.stringify(["priority-low"]),
    );
    const { result, rerender } = renderHook(
      ({ k }) => useCollapsedSet(k, storage),
      { initialProps: { k: tableCollapsedGroupsKey("ws-1", "view-a") } },
    );
    act(() => result.current[1](new Set(["status-todo"])));
    rerender({ k: tableCollapsedGroupsKey("ws-1", "view-b") });
    expect([...result.current[0]]).toEqual(["priority-low"]);
    rerender({ k: tableCollapsedGroupsKey("ws-1", "view-a") });
    expect([...result.current[0]]).toEqual(["status-todo"]);
    // Another workspace starts expanded.
    rerender({ k: tableCollapsedGroupsKey("ws-2", "view-a") });
    expect(result.current[0].size).toBe(0);
  });

  it("still collapses in memory when the storage throws", () => {
    const broken: CollapsedLanesStorage = {
      getItem: () => {
        throw new Error("blocked");
      },
      setItem: () => {
        throw new Error("blocked");
      },
      removeItem: () => {
        throw new Error("blocked");
      },
    };
    const { result } = renderHook(() =>
      useCollapsedSet(tableCollapsedGroupsKey("ws-1", "v"), broken),
    );
    act(() => result.current[1](new Set(["g"])));
    expect([...result.current[0]]).toEqual(["g"]);
  });
});
