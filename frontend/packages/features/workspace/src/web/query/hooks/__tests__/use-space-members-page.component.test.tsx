import type { ReactNode } from "react";
import { describe, expect, it, vi } from "vitest";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { AccountScopeProvider } from "@notrelix/query";
import { act, renderHook, waitFor } from "@notrelix/testing";
import type {
  SpacesApiClient,
  SpacesEndpoints,
} from "../../../../core/api/spaces.service";
import type {
  SpaceRole,
  SpaceSubjectType,
} from "../../../../core/model/spaces";
import { createUseSpaceMembersPage } from "../use-spaces";

const endpoints = {
  workspaces: {
    spaceMembers: (w: string, s: string) =>
      `/workspaces/${w}/spaces/${s}/members`,
  },
} as unknown as SpacesEndpoints;

const row = (n: number, role: string) => ({
  subjectType: "User",
  subjectId: `u-${String(n).padStart(3, "0")}`,
  role,
  displayName: `P ${n}`,
  avatarUrl: null,
  addedAt: "2026-01-01T00:00:00Z",
});

function setup(
  initial: { role: SpaceRole | null; kind: SpaceSubjectType | null },
  enabled = true,
) {
  const all = Array.from({ length: 120 }, (_, n) =>
    row(n, n < 20 ? "Admin" : "Member"),
  );
  const get = vi.fn(async (url: string) => {
    const q = new URL(url, "http://p.test").searchParams;
    const rows = all.filter((r) => !q.get("role") || r.role === q.get("role"));
    const limit = Number(q.get("limit"));
    const start = Number(q.get("cursor") ?? 0);
    const next = start + limit < rows.length ? String(start + limit) : null;
    return { items: rows.slice(start, start + limit), nextCursor: next };
  });
  const api = { get } as unknown as SpacesApiClient;
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  const wrapper = ({ children }: { children: ReactNode }) => (
    <QueryClientProvider client={queryClient}>
      <AccountScopeProvider accountId="acc-1">{children}</AccountScopeProvider>
    </QueryClientProvider>
  );
  const useIt = createUseSpaceMembersPage({ api, endpoints });
  const hook = renderHook(
    ({ filter, enabled }) => useIt("ws-1", "sp-1", filter, enabled),
    { wrapper, initialProps: { filter: initial, enabled } },
  );
  return { get, ...hook };
}

describe("the members tab's paged read", () => {
  it("reads nothing until the tab is open", () => {
    const { get, result } = setup({ role: null, kind: null }, false);
    expect(result.current.fetchStatus).toBe("idle");
    expect(get).not.toHaveBeenCalled();
  });

  it("loads fifty, then the next fifty on demand, then the last page has no next", async () => {
    const { result, get } = setup({ role: null, kind: null });
    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(result.current.data?.pages).toHaveLength(1);
    expect(result.current.data?.pages[0]?.entries).toHaveLength(50);
    expect(result.current.hasNextPage).toBe(true);

    await act(async () => {
      await result.current.fetchNextPage();
    });
    await waitFor(() => expect(result.current.data?.pages).toHaveLength(2));
    expect(
      result.current.data?.pages.flatMap((p) =>
        p.entries.map((e) => e.subjectId),
      ),
    ).toHaveLength(100);

    await act(async () => {
      await result.current.fetchNextPage();
    });
    await waitFor(() => expect(result.current.data?.pages).toHaveLength(3));
    const ids = result.current.data?.pages.flatMap((p) =>
      p.entries.map((e) => e.subjectId),
    );
    expect(ids).toHaveLength(120);
    expect(new Set(ids).size).toBe(120);
    expect(result.current.hasNextPage).toBe(false);
    expect(get).toHaveBeenCalledTimes(3);
  });

  it("reads a new list when the role filter changes, keeping the other filter's list cached", async () => {
    const { result, rerender, get } = setup({ role: null, kind: null });
    await waitFor(() => expect(result.current.isSuccess).toBe(true));

    rerender({ filter: { role: "Admin", kind: null }, enabled: true });
    await waitFor(() => expect(result.current.isPlaceholderData).toBe(false));
    await waitFor(() =>
      expect(result.current.data?.pages[0]?.entries).toHaveLength(20),
    );
    expect(result.current.hasNextPage).toBe(false);
    expect(String(get.mock.calls.at(-1)?.[0])).toContain("role=Admin");

    rerender({ filter: { role: null, kind: null }, enabled: true });
    await waitFor(() =>
      expect(result.current.data?.pages[0]?.entries).toHaveLength(50),
    );
  });
});
