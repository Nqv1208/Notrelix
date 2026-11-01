import type { ReactNode } from "react";
import { describe, expect, it, vi } from "vitest";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { act, renderHook, waitFor } from "@notrelix/testing";
import { endpoints } from "@notrelix/contracts";
import type { SpacesApiClient } from "../../../../core/api/spaces.service";
import type { Team, TeamMember } from "../../../../core/model/teams";
import { workspaceQueryKeys } from "../../../../query/keys";
import { createUseTeamMutations } from "../use-team-mutations";

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
    defaultOptions: { queries: { retry: false }, mutations: { retry: 3 } },
  });
  const wrapper = ({ children }: { children: ReactNode }) => (
    <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
  );
  return { queryClient, wrapper };
}

const membersKey = workspaceQueryKeys.teamMembers("ws-a", "t1");
const teamKey = workspaceQueryKeys.team("ws-a", "t1");
const teamsKey = workspaceQueryKeys.teams("ws-a");

const members = (): TeamMember[] => [
  { userId: "u1", displayName: "An", email: null, role: "Lead" },
  { userId: "u2", displayName: "Binh", email: null, role: "Member" },
];

const team = (): Team => ({
  id: "t1",
  name: "Helpdesk",
  description: null,
  status: "Active",
  memberCount: 2,
  version: 3,
  createdAt: "2026-01-01T00:00:00Z",
});

function apiWith(overrides: Record<string, unknown> = {}) {
  return {
    get: vi.fn(() => new Promise(() => undefined)),
    post: vi.fn(() => Promise.resolve(undefined)),
    put: vi.fn(() => Promise.resolve(undefined)),
    patch: vi.fn(),
    delete: vi.fn(() => Promise.resolve(undefined)),
    ...overrides,
  };
}

describe("Team mutations (WX-TEAM-001)", () => {
  it("changes a role optimistically and rolls back when the producer rejects it", async () => {
    const pending = deferred<void>();
    const api = apiWith({ put: vi.fn(() => pending.promise) });
    const useMutations = createUseTeamMutations({
      api: asClient(api),
      endpoints,
    });
    const { wrapper, queryClient } = setup();
    queryClient.setQueryData(membersKey, members());
    const { result } = renderHook(() => useMutations("ws-a"), { wrapper });

    act(() => {
      result.current.changeRole.mutate({
        teamId: "t1",
        userId: "u2",
        role: "Lead",
        expectedVersion: 3,
      });
    });
    await waitFor(() => {
      const cached = queryClient.getQueryData<TeamMember[]>(membersKey)!;
      expect(cached.find((m) => m.userId === "u2")?.role).toBe("Lead");
    });
    expect(api.put).toHaveBeenCalledWith(
      "/workspaces/ws-a/teams/t1/members/u2/role",
      { newRole: "Lead", expectedVersion: 3 },
    );

    await act(async () => {
      pending.reject(Object.assign(new Error("stale"), { status: 412 }));
    });
    await waitFor(() => expect(result.current.changeRole.isError).toBe(true));
    const restored = queryClient.getQueryData<TeamMember[]>(membersKey)!;
    expect(restored.find((m) => m.userId === "u2")?.role).toBe("Member");
    // Not retried automatically; the stale Team version is surfaced.
    expect(api.put).toHaveBeenCalledTimes(1);
    expect(queryClient.getQueryState(membersKey)?.isInvalidated).toBe(true);
  });

  it("refreshes the member list, the Team and the Team list after adding a member", async () => {
    const api = apiWith();
    const useMutations = createUseTeamMutations({
      api: asClient(api),
      endpoints,
    });
    const { wrapper, queryClient } = setup();
    queryClient.setQueryData(membersKey, members());
    queryClient.setQueryData(teamKey, team());
    queryClient.setQueryData(teamsKey, [team()]);
    const { result } = renderHook(() => useMutations("ws-a"), { wrapper });

    await act(async () => {
      await result.current.addMember.mutateAsync({
        teamId: "t1",
        userId: "u3",
        role: "Member",
        expectedVersion: 3,
      });
    });
    expect(api.post).toHaveBeenCalledWith("/workspaces/ws-a/teams/t1/members", {
      userId: "u3",
      role: "Member",
      expectedVersion: 3,
    });
    expect(queryClient.getQueryState(membersKey)?.isInvalidated).toBe(true);
    expect(queryClient.getQueryState(teamKey)?.isInvalidated).toBe(true);
    expect(queryClient.getQueryState(teamsKey)?.isInvalidated).toBe(true);
  });

  it("never retries a failed write and still refreshes", async () => {
    const api = apiWith({
      delete: vi.fn(() =>
        Promise.reject(Object.assign(new Error("stale"), { status: 412 })),
      ),
    });
    const useMutations = createUseTeamMutations({
      api: asClient(api),
      endpoints,
    });
    const { wrapper, queryClient } = setup();
    queryClient.setQueryData(teamsKey, [team()]);
    const { result } = renderHook(() => useMutations("ws-a"), { wrapper });

    await act(async () => {
      await result.current.removeMember
        .mutateAsync({ teamId: "t1", userId: "u2", expectedVersion: 2 })
        .catch(() => undefined);
    });
    expect(api.delete).toHaveBeenCalledTimes(1);
    expect(api.delete).toHaveBeenCalledWith(
      "/workspaces/ws-a/teams/t1/members/u2?expectedVersion=2",
    );
    expect(queryClient.getQueryState(teamsKey)?.isInvalidated).toBe(true);
  });

  it("sends the read version with Team lifecycle writes and refreshes the Team list on create", async () => {
    const api = apiWith({ post: vi.fn(() => Promise.resolve("t9")) });
    const useMutations = createUseTeamMutations({
      api: asClient(api),
      endpoints,
    });
    const { wrapper, queryClient } = setup();
    queryClient.setQueryData(teamsKey, [team()]);
    const { result } = renderHook(() => useMutations("ws-a"), { wrapper });

    await act(async () => {
      await result.current.create.mutateAsync({
        name: "Ops",
        description: null,
      });
      await result.current.rename.mutateAsync({
        teamId: "t1",
        name: "Support",
        expectedVersion: 3,
      });
      await result.current.describe.mutateAsync({
        teamId: "t1",
        description: "Tier 1",
        expectedVersion: 4,
      });
      await result.current.archive.mutateAsync({
        teamId: "t1",
        expectedVersion: 5,
      });
      await result.current.unarchive.mutateAsync({
        teamId: "t1",
        expectedVersion: 6,
      });
    });
    // A create carries a key, so a retry of the same attempt makes one Team.
    expect(api.post).toHaveBeenCalledWith(
      "/workspaces/ws-a/teams",
      { name: "Ops", description: null },
      { idempotencyKey: expect.any(String) },
    );
    expect(api.put).toHaveBeenCalledWith("/workspaces/ws-a/teams/t1", {
      name: "Support",
      expectedVersion: 3,
    });
    expect(api.put).toHaveBeenCalledWith(
      "/workspaces/ws-a/teams/t1/description",
      { description: "Tier 1", expectedVersion: 4 },
    );
    expect(api.post).toHaveBeenCalledWith("/workspaces/ws-a/teams/t1/archive", {
      expectedVersion: 5,
    });
    expect(api.post).toHaveBeenCalledWith(
      "/workspaces/ws-a/teams/t1/unarchive",
      { expectedVersion: 6 },
    );
    expect(queryClient.getQueryState(teamsKey)?.isInvalidated).toBe(true);
  });
});
