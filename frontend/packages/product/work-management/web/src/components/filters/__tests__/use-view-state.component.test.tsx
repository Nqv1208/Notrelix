import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { act, renderHook } from "@notrelix/testing";
import type { CompoundFilter } from "@notrelix/work-management-core";
import { serializeCompoundFilter } from "@notrelix/work-management-core";
import { useViewState, type UseViewStateOptions } from "../use-view-state";

const mocks = vi.hoisted(() => ({
  viewId: "v1" as string | undefined,
  save: vi.fn(),
  criteria: {} as Record<string, Record<string, string> | undefined>,
}));

vi.mock("@notrelix/work-management-state", async (importOriginal) => {
  const actual =
    await importOriginal<typeof import("@notrelix/work-management-state")>();
  return {
    ...actual,
    useBoardView: () => ({ viewId: mocks.viewId }),
    useBoardPreference: (boardId: string, _w: string, viewId?: string) => ({
      criteria: mocks.criteria[`${boardId}:${viewId}`],
      isLoading: false,
      persistent: true,
      save: mocks.save,
      saving: false,
      saveFailed: false,
      saveIssue: null,
      retry: vi.fn(),
    }),
  };
});

const filterOf = (value: string): CompoundFilter => ({
  join: "and",
  conditions: [{ fieldId: "$title", operator: "contains", values: [value] }],
});
const routeOf = (value: string) => serializeCompoundFilter(filterOf(value));

type Props = Pick<UseViewStateOptions, "boardId" | "routeFilter"> & {
  workspaceId?: string;
};

function setup(initial: Props) {
  const onRouteFilterChange = vi.fn();
  const hook = renderHook(
    (props: Props) =>
      useViewState({
        workspaceId: "w1",
        ...props,
        onRouteFilterChange,
        fieldDefinitions: undefined,
      }),
    { initialProps: initial },
  );
  return { ...hook, onRouteFilterChange };
}

beforeEach(() => {
  vi.useFakeTimers();
  mocks.viewId = "v1";
  mocks.save.mockReset();
  mocks.criteria = {};
});
afterEach(() => vi.useRealTimers());

describe("useViewState", () => {
  it("FUI[wm.filters.bar:state-switch-pending] saves a pending edit to the view it was made on, then shows the new view clean", () => {
    const { result, rerender } = setup({
      boardId: "b1",
      routeFilter: undefined,
    });
    act(() => result.current.setFilter(filterOf("lỗi")));
    expect(result.current.state.filter.conditions).toHaveLength(1);
    // The member switches view before the 400 ms pause ends.
    mocks.viewId = "v2";
    rerender({ boardId: "b1", routeFilter: undefined });
    expect(mocks.save).toHaveBeenCalledTimes(1);
    expect(mocks.save.mock.calls[0]![1]).toEqual({
      workspaceId: "w1",
      boardId: "b1",
      viewId: "v1",
    });
    expect(result.current.state.filter.conditions).toHaveLength(0);
    act(() => vi.advanceTimersByTime(1000));
    expect(mocks.save).toHaveBeenCalledTimes(1);
  });

  it("FUI[wm.filters.bar:state-switch-board] a board switch drops the local edit and saves a pending one to the old board", () => {
    const { result, rerender } = setup({
      boardId: "b1",
      routeFilter: undefined,
    });
    act(() => result.current.setFilter(filterOf("a")));
    rerender({ boardId: "b2", routeFilter: undefined });
    expect(mocks.save.mock.calls[0]![1]).toEqual({
      workspaceId: "w1",
      boardId: "b1",
      viewId: "v1",
    });
    expect(result.current.state.filter.conditions).toHaveLength(0);
  });

  it("FUI[wm.filters.bar:state-switch-workspace] a pending save flushed on a workspace switch keeps the workspace it was made in", () => {
    const { result, rerender } = setup({
      boardId: "b1",
      routeFilter: undefined,
      workspaceId: "w-old",
    });
    act(() => result.current.setFilter(filterOf("a")));
    rerender({ boardId: "b9", routeFilter: undefined, workspaceId: "w-new" });
    expect(mocks.save.mock.calls[0]![1]).toEqual({
      workspaceId: "w-old",
      boardId: "b1",
      viewId: "v1",
    });
    expect(result.current.state.filter.conditions).toHaveLength(0);
  });

  it("FUI[wm.filters.bar:state-switch-saved] an edit already saved does not leak into another view either", () => {
    const { result, rerender } = setup({
      boardId: "b1",
      routeFilter: undefined,
    });
    act(() => result.current.setFilter(filterOf("a")));
    act(() => vi.advanceTimersByTime(500));
    expect(mocks.save).toHaveBeenCalledTimes(1);
    mocks.viewId = "v2";
    rerender({ boardId: "b1", routeFilter: undefined });
    expect(result.current.state.filter.conditions).toHaveLength(0);
    expect(result.current.personal).toBe(false);
  });

  it("FUI[wm.filters.bar:state-back-forward] browser Back applies the route filter even after a local edit", () => {
    const { result, rerender } = setup({
      boardId: "b1",
      routeFilter: routeOf("one"),
    });
    act(() => result.current.setFilter(filterOf("two")));
    // The router shows what the edit wrote.
    rerender({ boardId: "b1", routeFilter: routeOf("two") });
    expect(result.current.state.filter.conditions[0]!.values).toEqual(["two"]);
    // Back: the route returns to the earlier filter.
    rerender({ boardId: "b1", routeFilter: routeOf("one") });
    expect(result.current.state.filter.conditions[0]!.values).toEqual(["one"]);
    // Back to no filter at all.
    rerender({ boardId: "b1", routeFilter: undefined });
    expect(result.current.state.filter.conditions).toHaveLength(0);
  });

  it("FUI[wm.filters.bar:state-route-lag] keeps the edit while the route has not caught up yet", () => {
    const { result, rerender } = setup({
      boardId: "b1",
      routeFilter: routeOf("one"),
    });
    act(() => result.current.setFilter(filterOf("two")));
    rerender({ boardId: "b1", routeFilter: routeOf("one") });
    expect(result.current.state.filter.conditions[0]!.values).toEqual(["two"]);
  });
});
