import type { ReactNode } from "react";
import { describe, expect, it, vi } from "vitest";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { act, renderHook, waitFor } from "@notrelix/testing";
import { endpoints } from "@notrelix/contracts";
import {
  NAVIGATION_RESOURCE_KIND,
  type NavigationReference,
} from "../../../core/model/navigation";
import type { NavigationApiClient } from "../../../core/api/navigation.service";
import { workspaceQueryKeys } from "../../../query/keys";

/** Test doubles only implement what a case needs. */
const asClient = (client: object) => client as unknown as NavigationApiClient;
import { createUseWorkspaceRecents } from "../../query/hooks/use-workspace-navigation";
import {
  createUseRecordRecent,
  createUseTogglePin,
} from "../../hooks/mutations/use-workspace-navigation-mutations";

function deferred<T>() {
  let resolve!: (value: T) => void;
  let reject!: (error: unknown) => void;
  const promise = new Promise<T>((res, rej) => {
    resolve = res;
    reject = rej;
  });
  return { promise, resolve, reject };
}

function setup() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  const wrapper = ({ children }: { children: ReactNode }) => (
    <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
  );
  return { queryClient, wrapper };
}

const board = NAVIGATION_RESOURCE_KIND.board;

describe("workspace navigation state (WX-SHELL-004)", () => {
  it("a late response from the previous workspace never shows in the new scope", async () => {
    const responses = new Map<string, ReturnType<typeof deferred<unknown>>>();
    const api = {
      get: vi.fn((url: string) => {
        const next = deferred<unknown>();
        responses.set(url, next);
        return next.promise;
      }),
      post: vi.fn(),
      put: vi.fn(),
      delete: vi.fn(),
    };
    const useRecents = createUseWorkspaceRecents({
      api: asClient(api),
      endpoints,
    });
    const { wrapper, queryClient } = setup();

    const { result, rerender } = renderHook(({ ws }) => useRecents(ws), {
      wrapper,
      initialProps: { ws: "ws-a" },
    });
    rerender({ ws: "ws-b" });

    await act(async () => {
      responses.get("/workspaces/ws-b/recents")!.resolve([
        {
          kind: board,
          resourceId: "b-of-b",
          lastOpenedAt: "2026-10-09T08:00:00Z",
        },
      ]);
    });
    await waitFor(() =>
      expect(result.current.data?.[0]?.resourceId).toBe("b-of-b"),
    );

    await act(async () => {
      responses.get("/workspaces/ws-a/recents")!.resolve([
        {
          kind: board,
          resourceId: "b-of-a",
          lastOpenedAt: "2026-10-09T09:00:00Z",
        },
      ]);
    });

    expect(result.current.data?.map((item) => item.resourceId)).toEqual([
      "b-of-b",
    ]);
    expect(
      queryClient.getQueryData<NavigationReference[]>(
        workspaceQueryKeys.recents("ws-a"),
      )?.[0]?.resourceId,
    ).toBe("b-of-a");
  });

  it("rolls an optimistic pin back when the producer rejects it", async () => {
    const pending = deferred<null>();
    const api = {
      get: vi.fn(),
      post: vi.fn(),
      put: vi.fn(() => pending.promise),
      delete: vi.fn(),
    };
    const useTogglePin = createUseTogglePin({ api: asClient(api), endpoints });
    const { wrapper, queryClient } = setup();
    const key = workspaceQueryKeys.pins("ws-a");
    const initial: NavigationReference[] = [
      { kind: board, resourceId: "b1", at: "2026-10-09T08:00:00Z" },
    ];
    queryClient.setQueryData(key, initial);

    const { result } = renderHook(() => useTogglePin("ws-a"), { wrapper });
    act(() => {
      result.current.mutate({ kind: board, resourceId: "b2", pinned: true });
    });
    await waitFor(() =>
      expect(
        queryClient.getQueryData<NavigationReference[]>(key)?.[0]?.resourceId,
      ).toBe("b2"),
    );

    await act(async () => {
      pending.reject(new Error("Workspaces_Navigation_PinLimitReached"));
    });
    await waitFor(() => expect(result.current.isError).toBe(true));
    expect(queryClient.getQueryData<NavigationReference[]>(key)).toEqual(
      initial,
    );
  });

  it("records a recent fire-and-forget with one idempotency key per open", async () => {
    const api = {
      get: vi.fn(),
      post: vi
        .fn()
        .mockRejectedValueOnce(new Error("offline"))
        .mockResolvedValueOnce(null),
      put: vi.fn(),
      delete: vi.fn(),
    };
    let counter = 0;
    const useRecordRecent = createUseRecordRecent({
      api,
      endpoints,
      createIdempotencyKey: () => `idem-${++counter}`,
    });
    const { wrapper, queryClient } = setup();
    const invalidate = vi.spyOn(queryClient, "invalidateQueries");

    const { result } = renderHook(() => useRecordRecent(), { wrapper });
    expect(() =>
      result.current({ workspaceId: "ws-a", kind: board, resourceId: "b1" }),
    ).not.toThrow();
    result.current({ workspaceId: "ws-a", kind: board, resourceId: "b1" });

    await waitFor(() => expect(invalidate).toHaveBeenCalledTimes(1));
    expect(api.post.mock.calls.map((call) => call[2]?.idempotencyKey)).toEqual([
      "idem-1",
      "idem-2",
    ]);
    expect(invalidate).toHaveBeenCalledWith({
      queryKey: workspaceQueryKeys.recents("ws-a"),
    });
  });

  it("a recent recorded while the first list read is in flight shows up", async () => {
    const reads: ReturnType<typeof deferred<unknown>>[] = [];
    const api = {
      get: vi.fn(() => {
        const next = deferred<unknown>();
        reads.push(next);
        return next.promise;
      }),
      post: vi.fn().mockResolvedValue(null),
      put: vi.fn(),
      delete: vi.fn(),
    };
    const useRecents = createUseWorkspaceRecents({
      api: asClient(api),
      endpoints,
    });
    const useRecordRecent = createUseRecordRecent({
      api: asClient(api),
      endpoints,
      createIdempotencyKey: () => "idem",
    });
    const { wrapper } = setup();

    const { result } = renderHook(
      () => ({ recents: useRecents("ws-a"), record: useRecordRecent() }),
      { wrapper },
    );
    await waitFor(() => expect(reads).toHaveLength(1));
    act(() =>
      result.current.record({
        workspaceId: "ws-a",
        kind: board,
        resourceId: "b1",
      }),
    );

    await waitFor(() => expect(reads).toHaveLength(2));
    await act(async () => {
      reads[0]!.resolve([]);
      reads[1]!.resolve([
        { kind: board, resourceId: "b1", lastOpenedAt: "2026-10-09T08:00:00Z" },
      ]);
    });
    await waitFor(() =>
      expect(result.current.recents.data?.[0]?.resourceId).toBe("b1"),
    );
  });
});
