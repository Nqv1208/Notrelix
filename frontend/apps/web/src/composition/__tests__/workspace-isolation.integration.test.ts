import { describe, expect, it, vi } from "vitest";
import { createQueryClient } from "@notrelix/query";
import { createSessionEventBus } from "@notrelix/runtime-web";
import {
  createApplicationLifecycle,
  createWorkspaceEventSource,
  type WorkspaceEventSource,
} from "../application-lifecycle";

function createFakeRealtime() {
  return {
    connect: vi.fn(async () => undefined),
    disconnect: vi.fn(),
    subscribe: vi.fn(() => () => undefined),
    subscribeState: vi.fn(() => () => undefined),
    subscribeRecovery: vi.fn(() => () => undefined),
    getState: vi.fn(() => "connected" as const),
    dispose: vi.fn(),
  };
}

function createHarness() {
  const queryClient = createQueryClient();
  const sessionEvents = createSessionEventBus();
  const workspaceEvents: WorkspaceEventSource = createWorkspaceEventSource();
  const realtime = createFakeRealtime();
  const clearWorkspaceState = vi.fn();
  const lifecycle = createApplicationLifecycle({
    queryClient,
    realtime,
    sessionEvents,
    workspaceEvents,
    clearSessionState: vi.fn(),
    clearWorkspaceState,
    navigateToSignedOut: vi.fn(),
  });

  return {
    queryClient,
    workspaceEvents,
    realtime,
    clearWorkspaceState,
    lifecycle,
  };
}

function seedWorkspaceA(queryClient: ReturnType<typeof createQueryClient>) {
  queryClient.setQueryData(["workspaces", "detail", "ws-a"], { name: "A" });
  queryClient.setQueryData(["workspaces", "views", "ws-a"], [{ id: "v1" }]);
}

describe("application lifecycle — workspace isolation", () => {
  it("LIFE-020 full QueryClient cache is empty before B activation completes", () => {
    const { queryClient, workspaceEvents, lifecycle } = createHarness();
    seedWorkspaceA(queryClient);

    workspaceEvents.publish({
      previousWorkspaceId: "ws-a",
      nextWorkspaceId: "ws-b",
    });
    expect(queryClient.getQueryCache().getAll()).toHaveLength(0);
    lifecycle.dispose();
  });

  it("LIFE-021 workspace transient reset is called", () => {
    const { queryClient, workspaceEvents, clearWorkspaceState, lifecycle } =
      createHarness();
    seedWorkspaceA(queryClient);

    workspaceEvents.publish({
      previousWorkspaceId: "ws-a",
      nextWorkspaceId: "ws-b",
    });
    expect(clearWorkspaceState).toHaveBeenCalledTimes(1);
    lifecycle.dispose();
  });

  it("LIFE-022 realtime A connection/subscriptions are disconnected/reset", () => {
    const { queryClient, workspaceEvents, realtime, lifecycle } =
      createHarness();
    seedWorkspaceA(queryClient);

    workspaceEvents.publish({
      previousWorkspaceId: "ws-a",
      nextWorkspaceId: "ws-b",
    });
    expect(realtime.disconnect).toHaveBeenCalledWith("workspace-switch");
    expect(realtime.disconnect).toHaveBeenCalledTimes(1);
    lifecycle.dispose();
  });

  it("LIFE-023 B activation/reconnect happens only after old state reset", () => {
    const {
      queryClient,
      workspaceEvents,
      realtime,
      clearWorkspaceState,
      lifecycle,
    } = createHarness();
    seedWorkspaceA(queryClient);

    const calls: string[] = [];
    const originalDisconnect = realtime.disconnect;
    realtime.disconnect.mockImplementation((reason?: string) => {
      calls.push(`disconnect:${reason}`);
      originalDisconnect(reason);
    });
    clearWorkspaceState.mockImplementation(() => {
      calls.push("clear-workspace");
    });

    workspaceEvents.publish({
      previousWorkspaceId: "ws-a",
      nextWorkspaceId: "ws-b",
    });

    // Cache must be empty and workspace state cleared before any reconnect is allowed.
    const cacheCleared = queryClient.getQueryCache().getAll().length === 0;
    expect(cacheCleared).toBe(true);
    expect(calls.indexOf("clear-workspace")).toBeGreaterThanOrEqual(0);
    expect(calls.indexOf("disconnect:workspace-switch")).toBeGreaterThan(
      calls.indexOf("clear-workspace"),
    );
    lifecycle.dispose();
  });

  it("LIFE-024 switching A -> A is a no-op", () => {
    const {
      queryClient,
      workspaceEvents,
      realtime,
      clearWorkspaceState,
      lifecycle,
    } = createHarness();
    seedWorkspaceA(queryClient);

    workspaceEvents.publish({
      previousWorkspaceId: "ws-a",
      nextWorkspaceId: "ws-a",
    });
    expect(queryClient.getQueryCache().getAll().length).toBeGreaterThan(0);
    expect(clearWorkspaceState).not.toHaveBeenCalled();
    expect(realtime.disconnect).not.toHaveBeenCalled();
    lifecycle.dispose();
  });

  it("LIFE-025 rapid A -> B -> C cannot restore A/B cache state", () => {
    const { queryClient, workspaceEvents, clearWorkspaceState, lifecycle } =
      createHarness();
    seedWorkspaceA(queryClient);

    workspaceEvents.publish({
      previousWorkspaceId: "ws-a",
      nextWorkspaceId: "ws-b",
    });
    expect(queryClient.getQueryCache().getAll()).toHaveLength(0);

    workspaceEvents.publish({
      previousWorkspaceId: "ws-b",
      nextWorkspaceId: "ws-c",
    });
    expect(queryClient.getQueryCache().getAll()).toHaveLength(0);
    expect(clearWorkspaceState).toHaveBeenCalledTimes(2);
    lifecycle.dispose();
  });

  it("LIFE-026 lifecycle dispose unsubscribes workspace listener", () => {
    const { queryClient, workspaceEvents, clearWorkspaceState, lifecycle } =
      createHarness();
    seedWorkspaceA(queryClient);

    lifecycle.dispose();
    workspaceEvents.publish({
      previousWorkspaceId: "ws-a",
      nextWorkspaceId: "ws-b",
    });
    expect(clearWorkspaceState).not.toHaveBeenCalled();
    expect(queryClient.getQueryCache().getAll().length).toBeGreaterThan(0);
  });
});
