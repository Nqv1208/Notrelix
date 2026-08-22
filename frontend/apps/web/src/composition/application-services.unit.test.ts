import { describe, expect, it, vi } from "vitest";
import type { AppRuntime } from "@notrelix/runtime-web";
import type { NotrelixClient } from "@notrelix/contracts";
import { createWebApplicationServices } from "./application-services";

function createRuntime(client: NotrelixClient): AppRuntime {
  const listeners: Set<(event: unknown) => void> = new Set();
  return {
    api: client,
    environment: {
      apiBaseUrl: "http://api.test",
      realtimeUrl: "ws://realtime.test",
    },
    sessionEvents: {
      publish: vi.fn((event: unknown) => {
        listeners.forEach((listener) => listener(event));
      }),
      subscribe: vi.fn((listener: (event: unknown) => void) => {
        listeners.add(listener);
        return () => {
          listeners.delete(listener);
        };
      }),
      clear: vi.fn(() => {
        listeners.clear();
      }),
    },
    realtime: {
      connect: vi.fn(async () => undefined),
      disconnect: vi.fn(),
      subscribe: vi.fn(() => () => undefined),
      subscribeState: vi.fn(() => () => undefined),
      subscribeRecovery: vi.fn(() => () => undefined),
      getState: vi.fn(() => "idle"),
      dispose: vi.fn(),
    },
    telemetry: {
      track: vi.fn(),
      reportError: vi.fn(),
      withContext: vi.fn(),
      flush: vi.fn(async () => undefined),
    },
    dispose: vi.fn(),
  } as unknown as AppRuntime;
}

function createClient(label: string): NotrelixClient {
  return {
    api: {
      get: vi.fn(async () => label),
      post: vi.fn(async () => label),
      put: vi.fn(async () => label),
      patch: vi.fn(async () => label),
      delete: vi.fn(async () => label),
    },
    endpoints: {},
  } as unknown as NotrelixClient;
}

describe("createWebApplicationServices", () => {
  it("keeps Work Management services scoped to their runtime client", async () => {
    const firstClient = createClient("first");
    const secondClient = createClient("second");

    const first = createWebApplicationServices(createRuntime(firstClient), {
      navigateToSignedOut: vi.fn(),
    });
    const second = createWebApplicationServices(createRuntime(secondClient), {
      navigateToSignedOut: vi.fn(),
    });

    await first.workManagement.cards.moveCard({
      cardId: "card-1",
      listId: "group-1",
      position: 1,
    });
    await second.workManagement.cards.moveCard({
      cardId: "card-2",
      listId: "group-2",
      position: 2,
    });

    expect(firstClient.api.post).toHaveBeenCalledWith("/board-items/card-1/move", {
      groupId: "group-1",
      position: 1,
    });
    expect(secondClient.api.post).toHaveBeenCalledWith("/board-items/card-2/move", {
      groupId: "group-2",
      position: 2,
    });
    expect(firstClient.api.post).not.toHaveBeenCalledWith(
      "/board-items/card-2/move",
      expect.anything(),
    );
    expect(secondClient.api.post).not.toHaveBeenCalledWith(
      "/board-items/card-1/move",
      expect.anything(),
    );
  });
});
