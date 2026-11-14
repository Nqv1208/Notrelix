import { describe, expect, it, vi } from "vitest";
import { createQueryClient } from "@notrelix/query";
import { createSessionEventBus } from "@notrelix/runtime-web";
import { createApplicationLifecycle } from "../application-lifecycle";

function createFakeRealtime() {
  return {
    connect: vi.fn(async () => undefined),
    disconnect: vi.fn(),
    subscribe: vi.fn(() => () => undefined),
    subscribeState: vi.fn(() => () => undefined),
    subscribeRecovery: vi.fn(() => () => undefined),
    subscribeEphemeral: vi.fn(() => () => undefined),
    sendTyping: vi.fn(),
    sendPresence: vi.fn(),
    getState: vi.fn(() => "connected" as const),
    dispose: vi.fn(),
  };
}

function createHarness() {
  const queryClient = createQueryClient();
  const sessionEvents = createSessionEventBus();
  const realtime = createFakeRealtime();
  const lifecycle = createApplicationLifecycle({
    queryClient,
    realtime,
    sessionEvents,
    navigateToSignedOut: vi.fn(),
  });

  return {
    queryClient,
    realtime,
    lifecycle,
  };
}

function seedWorkspaceA(queryClient: ReturnType<typeof createQueryClient>) {
  queryClient.setQueryData(["workspaces", "detail", "ws-a"], { name: "A" });
  queryClient.setQueryData(["workspaces", "views", "ws-a"], [{ id: "v1" }]);
}

describe("application lifecycle — workspace isolation", () => {
  it("LIFE-020 first workspace initialization does not unnecessarily clear cache", () => {
    const { queryClient, lifecycle } = createHarness();
    seedWorkspaceA(queryClient);

    lifecycle.prepareWorkspaceTransition("ws-a");
    expect(queryClient.getQueryCache().getAll().length).toBeGreaterThan(0);
    lifecycle.dispose();
  });

  it("LIFE-021 full QueryClient cache is empty on workspace transition", () => {
    const { queryClient, lifecycle } = createHarness();
    lifecycle.prepareWorkspaceTransition("ws-a");
    seedWorkspaceA(queryClient);

    lifecycle.prepareWorkspaceTransition("ws-b");
    expect(queryClient.getQueryCache().getAll()).toHaveLength(0);
    lifecycle.dispose();
  });

  it("LIFE-022 realtime A connection is disconnected with workspace-switch reason", () => {
    const { queryClient, realtime, lifecycle } = createHarness();
    lifecycle.prepareWorkspaceTransition("ws-a");
    seedWorkspaceA(queryClient);

    lifecycle.prepareWorkspaceTransition("ws-b");
    expect(realtime.disconnect).toHaveBeenCalledWith("workspace-switch");
    expect(realtime.disconnect).toHaveBeenCalledTimes(1);
    lifecycle.dispose();
  });

  it("LIFE-024 switching A -> A is a no-op", () => {
    const { queryClient, realtime, lifecycle } = createHarness();
    lifecycle.prepareWorkspaceTransition("ws-a");
    seedWorkspaceA(queryClient);

    lifecycle.prepareWorkspaceTransition("ws-a");
    expect(queryClient.getQueryCache().getAll().length).toBeGreaterThan(0);
    expect(realtime.disconnect).not.toHaveBeenCalled();
    lifecycle.dispose();
  });

  it("LIFE-025 rapid A -> B -> C clears cache on each workspace change", () => {
    const { queryClient, lifecycle } = createHarness();
    lifecycle.prepareWorkspaceTransition("ws-a");
    seedWorkspaceA(queryClient);

    lifecycle.prepareWorkspaceTransition("ws-b");
    expect(queryClient.getQueryCache().getAll()).toHaveLength(0);

    queryClient.setQueryData(["workspaces", "detail", "ws-b"], { name: "B" });
    lifecycle.prepareWorkspaceTransition("ws-c");
    expect(queryClient.getQueryCache().getAll()).toHaveLength(0);

    lifecycle.dispose();
  });

  it("LIFE-027 a slow read of workspace A that settles after the switch to B leaves B without A data", async () => {
    const { queryClient, lifecycle } = createHarness();
    lifecycle.prepareWorkspaceTransition("ws-a");

    let release: (value: { name: string }) => void = () => undefined;
    const slowRead = queryClient.fetchQuery({
      queryKey: ["workspaces", "detail", "ws-a"],
      queryFn: () =>
        new Promise<{ name: string }>((resolve) => {
          release = resolve;
        }),
    });
    // The switch happens while the read is in flight.
    lifecycle.prepareWorkspaceTransition("ws-b");
    queryClient.setQueryData(["workspaces", "detail", "ws-b"], { name: "B" });

    release({ name: "A" });
    // The reader that asked for A may reject or settle; neither may reach the cache.
    await slowRead.catch(() => undefined);

    expect(
      queryClient.getQueryData(["workspaces", "detail", "ws-a"]),
    ).toBeUndefined();
    expect(queryClient.getQueryData(["workspaces", "detail", "ws-b"])).toEqual({
      name: "B",
    });
    expect(queryClient.getQueryCache().getAll()).toHaveLength(1);
    lifecycle.dispose();
  });

  it("LIFE-026 lifecycle dispose prevents further transitions", () => {
    const { queryClient, realtime, lifecycle } = createHarness();
    lifecycle.prepareWorkspaceTransition("ws-a");
    seedWorkspaceA(queryClient);

    lifecycle.dispose();
    lifecycle.prepareWorkspaceTransition("ws-b");
    expect(realtime.disconnect).not.toHaveBeenCalled();
    expect(queryClient.getQueryCache().getAll().length).toBeGreaterThan(0);
  });
});
