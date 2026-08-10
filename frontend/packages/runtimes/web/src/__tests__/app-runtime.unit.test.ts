import { describe, it, expect, vi } from "vitest";
import { createAppRuntime } from "../runtime/app-runtime";
import type { RealtimeTransport } from "@notrelix/realtime";

describe("AppRuntime", () => {
  it("instantiates runtime with normalized environment and freezes top-level runtime object", () => {
    const runtime = createAppRuntime({
      apiUrl: "http://api.test",
      realtimeUrl: "ws://realtime.test",
    });

    expect(runtime.env.apiUrl).toBe("http://api.test");
    expect(runtime.env.realtimeUrl).toBe("ws://realtime.test");
    expect(Object.isFrozen(runtime)).toBe(true);
  });

  it("supports injecting custom test factories", () => {
    const mockApiClient = { api: {} } as unknown;
    const createApiClientSpy = vi.fn().mockReturnValue(mockApiClient);

    const mockClock = {
      now: () => new Date("2026-01-01T00:00:00Z"),
      isoNow: () => "2026-01-01T00:00:00Z",
    };

    const runtime = createAppRuntime(
      { apiUrl: "http://api.test" },
      {
        createApiClient: createApiClientSpy,
        clock: mockClock,
      },
    );

    expect(createApiClientSpy).toHaveBeenCalled();
    expect(runtime.clock.isoNow()).toBe("2026-01-01T00:00:00Z");
  });

  it("executes dispose idempotently and cleans up session events and realtime connection", async () => {
    const mockRealtime = {
      dispose: vi.fn(),
    } as unknown as RealtimeTransport;
    const telemetry = {
      track: vi.fn(),
      reportError: vi.fn(),
      withContext: vi.fn(function withContext() {
        return telemetry;
      }),
      flush: vi.fn(),
    };

    const runtime = createAppRuntime(
      { apiUrl: "http://api.test" },
      { createRealtimeClient: () => mockRealtime, telemetry },
    );

    const sessionSpy = vi.fn();
    runtime.sessionEvents.subscribe(sessionSpy);

    await runtime.dispose();
    expect(mockRealtime.dispose).toHaveBeenCalledTimes(1);
    expect(telemetry.flush).toHaveBeenCalledTimes(1);

    // Second dispose call should do nothing (idempotent)
    await runtime.dispose();
    expect(mockRealtime.dispose).toHaveBeenCalledTimes(1);
    expect(telemetry.flush).toHaveBeenCalledTimes(1);
  });

  it("defaults unknown feature flags to disabled", () => {
    const runtime = createAppRuntime({
      apiUrl: "http://api.test",
      realtimeUrl: "ws://realtime.test",
    });

    expect(runtime.featureFlags.isEnabled("unknown.flag")).toBe(false);
  });
});
