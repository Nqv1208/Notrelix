import type { ReactNode } from "react";
import { describe, expect, it, vi } from "vitest";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { renderHook, waitFor } from "@notrelix/testing";
import { endpoints } from "@notrelix/contracts";
import type { SpacesApiClient } from "../../../../core/api/spaces.service";
import { createUseTeams, createUseWorkspaceTeams } from "../use-teams";

/**
 * One cache entry per `workspaceQueryKeys.teams(ws)`: the Teams management
 * list and the Space member picker read the same Team[] and derive their
 * own view, so neither can poison the other within the stale window.
 */

const rows = [
  {
    id: "t1",
    name: "Helpdesk",
    description: null,
    status: "Active",
    memberCount: 4,
    version: 2,
    createdAt: "2026-01-01T00:00:00Z",
  },
  {
    id: "t2",
    name: "Legacy",
    description: null,
    status: "Archived",
    memberCount: 1,
    version: 5,
    createdAt: "2026-01-02T00:00:00Z",
  },
];

function setup() {
  const get = vi.fn(() => Promise.resolve(rows));
  const api = {
    get,
    post: vi.fn(),
    put: vi.fn(),
    patch: vi.fn(),
    delete: vi.fn(),
  } as unknown as SpacesApiClient;
  const deps = { api, endpoints };
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  const wrapper = ({ children }: { children: ReactNode }) => (
    <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
  );
  return {
    get,
    wrapper,
    useTeams: createUseTeams(deps),
    useWorkspaceTeams: createUseWorkspaceTeams(deps),
  };
}

describe("workspace Teams cache (WX-TEAM-001, WX-SPACE)", () => {
  it("shows archived and active Teams in the list after the Space picker hook ran first", async () => {
    const { get, wrapper, useTeams, useWorkspaceTeams } = setup();

    const picker = renderHook(() => useWorkspaceTeams("ws-a"), { wrapper });
    await waitFor(() => expect(picker.result.current.isSuccess).toBe(true));
    expect(picker.result.current.data?.map((t) => t.id)).toEqual(["t1"]);

    const list = renderHook(() => useTeams("ws-a"), { wrapper });
    await waitFor(() => expect(list.result.current.isSuccess).toBe(true));
    expect(
      list.result.current.data?.map((t) => [t.id, t.status, t.version]),
    ).toEqual([
      ["t1", "Active", 2],
      ["t2", "Archived", 5],
    ]);
    expect(get).toHaveBeenCalledTimes(1);
  });

  it("offers only active Teams in the Space picker after the Teams list hook ran first", async () => {
    const { get, wrapper, useTeams, useWorkspaceTeams } = setup();

    const list = renderHook(() => useTeams("ws-a"), { wrapper });
    await waitFor(() => expect(list.result.current.isSuccess).toBe(true));
    expect(list.result.current.data).toHaveLength(2);

    const picker = renderHook(() => useWorkspaceTeams("ws-a"), { wrapper });
    await waitFor(() => expect(picker.result.current.isSuccess).toBe(true));
    expect(picker.result.current.data).toEqual([
      { id: "t1", name: "Helpdesk", memberCount: 4 },
    ]);
    expect(get).toHaveBeenCalledTimes(1);
  });

  it("does not share a cache entry across workspaces", async () => {
    const { get, wrapper, useWorkspaceTeams } = setup();

    const a = renderHook(() => useWorkspaceTeams("ws-a"), { wrapper });
    await waitFor(() => expect(a.result.current.isSuccess).toBe(true));
    const b = renderHook(() => useWorkspaceTeams("ws-b"), { wrapper });
    await waitFor(() => expect(b.result.current.isSuccess).toBe(true));
    expect(get).toHaveBeenCalledTimes(2);
  });
});
