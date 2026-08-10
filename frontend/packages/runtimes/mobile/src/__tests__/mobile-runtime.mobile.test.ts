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
      releaseSha: "mobile-test-sha",
      mode: "test",
    });

    expect(runtime.env.apiUrl).toBe("https://api.test.com");
    expect(runtime.env.realtimeUrl).toBe("wss://realtime.test.com");
    expect(runtime.env.releaseSha).toBe("mobile-test-sha");
    expect(runtime.env.mode).toBe("test");
    expect(runtime.clock).toBeDefined();
  });

  it("passes appUrl to resolved runtime env", () => {
    const runtime = createMobileRuntime({
      apiUrl: "https://api.test.com",
      realtimeUrl: "wss://realtime.test.com",
      appUrl: "https://app.test.com",
      mode: "test",
    });

    expect(runtime.env.appUrl).toBe("https://app.test.com");
  });

  it("fails validation in production mode if API URL is missing", () => {
    expect(() =>
      createMobileRuntime({
        realtimeUrl: "wss://realtime.test.com",
        appUrl: "https://app.test.com",
        mode: "production",
      }),
    ).toThrow(/apiUrl/);
  });

  it("fails validation in production mode if realtime URL is missing", () => {
    expect(() =>
      createMobileRuntime({
        apiUrl: "https://api.test.com",
        appUrl: "https://app.test.com",
        mode: "production",
      }),
    ).toThrow(/realtimeUrl/);
  });

  it("disposes mobile runtime idempotently", async () => {
    const dispose = vi.fn();
    const flush = vi.fn();

    const runtime = createMobileRuntime(
      {
        apiUrl: "https://api.test.com",
        realtimeUrl: "wss://realtime.test.com",
        mode: "test",
      },
      {
        createRealtimeClient: () =>
          ({
            dispose,
          }) as unknown as RealtimeTransport,
        telemetry: {
          track: vi.fn(),
          reportError: vi.fn(),
          withContext: vi.fn(),
          flush,
        },
      },
    );

    await runtime.dispose();
    await runtime.dispose();

    expect(dispose).toHaveBeenCalledTimes(1);
    expect(flush).toHaveBeenCalledTimes(1);
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
