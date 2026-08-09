import { describe, it, expect, vi } from "vitest";
import { createMobileRuntime } from "../runtime/mobile-runtime";
import { createMobileWorkspaceLifecycle } from "../runtime/mobile-workspace-lifecycle";
import type { QueryClient } from "@tanstack/react-query";
import type { RealtimeTransport } from "@notrelix/realtime";

describe("MobileRuntime and WorkspaceLifecycle", () => {
  it("creates mobile runtime with env and clock", () => {
    const runtime = createMobileRuntime({
      apiUrl: "https://api.test.com",
      realtimeUrl: "wss://realtime.test.com",
      mode: "test",
    });

    expect(runtime.env.apiUrl).toBe("https://api.test.com");
    expect(runtime.clock).toBeDefined();
  });

  it("handles workspace transitions A -> B correctly", () => {
    const clearMock = vi.fn();
    const disconnectMock = vi.fn();

    const mockQueryClient = { clear: clearMock } as unknown as QueryClient;
    const mockRealtime = {
      disconnect: disconnectMock,
    } as unknown as RealtimeTransport;

    const lifecycle = createMobileWorkspaceLifecycle({
      queryClient: mockQueryClient,
      realtime: mockRealtime,
    });

    // Initial navigation
    lifecycle.prepareWorkspaceTransition("ws-1");
    expect(clearMock).not.toHaveBeenCalled();

    // Navigation to same workspace A -> A
    lifecycle.prepareWorkspaceTransition("ws-1");
    expect(clearMock).not.toHaveBeenCalled();

    // Transition A -> B
    lifecycle.prepareWorkspaceTransition("ws-2");
    expect(clearMock).toHaveBeenCalled();
    expect(disconnectMock).toHaveBeenCalledWith("workspace-switch");
  });
});
