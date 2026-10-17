import type { ReactNode } from "react";
import { describe, expect, it, vi } from "vitest";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { act, renderHook, waitFor } from "@notrelix/testing";
import { endpoints } from "@notrelix/contracts";
import type { SpacesApiClient } from "../../../core/api/spaces.service";
import {
  buildSpaceIndex,
  childrenOf,
  type SpaceNode,
} from "../../../core/model/spaces";
import { workspaceQueryKeys } from "../../../query/keys";
import { spaceTreeFixture } from "../../../verification/workspace-spaces-fixtures";
import { createUseSpaceMutations } from "../../hooks/mutations/use-space-mutations";
import { createUseSpaceTree } from "../../query/hooks/use-spaces";
import {
  spaceExpansionStorageKey,
  useSpaceTreeExpansion,
  type ExpansionStorage,
} from "../use-space-tree-expansion";

const asClient = (client: object) => client as unknown as SpacesApiClient;

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

const treeKey = workspaceQueryKeys.spaceTree("ws-a");

describe("Space state (OT-5)", () => {
  it("moves a Space optimistically and rolls back when the producer rejects it", async () => {
    const pending = deferred<void>();
    const api = {
      get: vi.fn(() => new Promise(() => undefined)),
      post: vi.fn(() => pending.promise),
      put: vi.fn(),
      patch: vi.fn(),
      delete: vi.fn(),
    };
    const useMutations = createUseSpaceMutations({
      api: asClient(api),
      endpoints,
    });
    const { wrapper, queryClient } = setup();
    queryClient.setQueryData(treeKey, spaceTreeFixture());
    const { result } = renderHook(() => useMutations("ws-a"), { wrapper });

    act(() => {
      result.current.move.mutate({
        spaceId: "sp-marketing",
        parentId: "sp-it",
        previousSiblingId: "sp-it-security",
        nextSiblingId: null,
        expectedVersion: 3,
      });
    });
    await waitFor(() => {
      const nodes = queryClient.getQueryData<SpaceNode[]>(treeKey)!;
      expect(
        childrenOf(buildSpaceIndex(nodes), "sp-it").map((node) => node.id),
      ).toContain("sp-marketing");
    });
    expect(api.post).toHaveBeenCalledWith(
      "/workspaces/ws-a/spaces/sp-marketing/move",
      {
        newParentSpaceId: "sp-it",
        previousSiblingId: "sp-it-security",
        nextSiblingId: null,
        expectedVersion: 3,
      },
    );

    await act(async () => {
      pending.reject(Object.assign(new Error("stale"), { status: 412 }));
    });
    await waitFor(() => expect(result.current.move.isError).toBe(true));
    const restored = queryClient.getQueryData<SpaceNode[]>(treeKey)!;
    expect(
      restored.find((node) => node.id === "sp-marketing")?.parentId,
    ).toBeNull();
    // The tree is refreshed so the next attempt carries the current version.
    expect(queryClient.getQueryState(treeKey)?.isInvalidated).toBe(true);
  });

  it("sends the read version with membership writes and refreshes members and tree", async () => {
    const api = {
      get: vi.fn(() => new Promise(() => undefined)),
      post: vi.fn(() => Promise.resolve(undefined)),
      put: vi.fn(),
      patch: vi.fn(() => Promise.resolve(undefined)),
      delete: vi.fn(() => Promise.resolve(undefined)),
    };
    const useMutations = createUseSpaceMutations({
      api: asClient(api),
      endpoints,
    });
    const { wrapper, queryClient } = setup();
    const membersKey = workspaceQueryKeys.spaceMembers("ws-a", "sp-it");
    queryClient.setQueryData(membersKey, []);
    queryClient.setQueryData(treeKey, spaceTreeFixture());
    const { result } = renderHook(() => useMutations("ws-a"), { wrapper });

    await act(async () => {
      await result.current.changeMemberRole.mutateAsync({
        spaceId: "sp-it",
        subjectType: "Team",
        subjectId: "team-helpdesk",
        role: "Viewer",
        expectedVersion: 3,
      });
      await result.current.removeMember.mutateAsync({
        spaceId: "sp-it",
        subjectType: "User",
        subjectId: "user-minh",
        expectedVersion: 4,
      });
    });
    expect(api.patch).toHaveBeenCalledWith(
      "/workspaces/ws-a/spaces/sp-it/members/Team/team-helpdesk",
      { role: "Viewer", expectedVersion: 3 },
    );
    expect(api.delete).toHaveBeenCalledWith(
      "/workspaces/ws-a/spaces/sp-it/members/User/user-minh?expectedVersion=4",
    );
    expect(queryClient.getQueryState(membersKey)?.isInvalidated).toBe(true);
    expect(queryClient.getQueryState(treeKey)?.isInvalidated).toBe(true);
  });

  it("keeps each workspace's tree under its own key", async () => {
    const api = {
      get: vi.fn((url: string) =>
        Promise.resolve(
          url.includes("ws-a")
            ? [
                {
                  id: "a1",
                  name: "A",
                  spaceType: "Folder",
                  visibility: "Workspace",
                },
              ]
            : [
                {
                  id: "b1",
                  name: "B",
                  spaceType: "Folder",
                  visibility: "Workspace",
                },
              ],
        ),
      ),
      post: vi.fn(),
      put: vi.fn(),
      patch: vi.fn(),
      delete: vi.fn(),
    };
    const useTree = createUseSpaceTree({ api: asClient(api), endpoints });
    const { wrapper } = setup();
    const { result, rerender } = renderHook(({ ws }) => useTree(ws), {
      wrapper,
      initialProps: { ws: "ws-a" },
    });
    await waitFor(() => expect(result.current.data?.[0]?.id).toBe("a1"));
    rerender({ ws: "ws-b" });
    await waitFor(() => expect(result.current.data?.[0]?.id).toBe("b1"));
    expect(api.get).toHaveBeenCalledWith("/workspaces/ws-b/spaces/tree");
  });

  it("persists expansion per member and workspace and survives blocked storage", () => {
    const store = new Map<string, string>();
    const storage: ExpansionStorage = {
      getItem: (key) => store.get(key) ?? null,
      setItem: (key, value) => void store.set(key, value),
    };
    const { result, rerender } = renderHook(
      ({ ws }) => useSpaceTreeExpansion("user-1", ws, storage),
      { initialProps: { ws: "ws-a" } },
    );
    act(() => result.current.setExpanded("sp-it", true));
    act(() => result.current.reveal(["sp-it", "sp-it-platform"]));
    expect([...result.current.expandedIds]).toEqual([
      "sp-it",
      "sp-it-platform",
    ]);
    expect(store.get(spaceExpansionStorageKey("user-1", "ws-a"))).toBe(
      JSON.stringify(["sp-it", "sp-it-platform"]),
    );
    rerender({ ws: "ws-b" });
    expect([...result.current.expandedIds]).toEqual([]);
    rerender({ ws: "ws-a" });
    expect(result.current.expandedIds.has("sp-it")).toBe(true);

    const blocked: ExpansionStorage = {
      getItem: () => {
        throw new Error("SecurityError");
      },
      setItem: () => {
        throw new Error("QuotaExceededError");
      },
    };
    const safe = renderHook(() =>
      useSpaceTreeExpansion("user-1", "ws-a", blocked),
    );
    act(() => safe.result.current.setExpanded("sp-x", true));
    expect(safe.result.current.expandedIds.has("sp-x")).toBe(true);
  });
});
