import { describe, expect, it, vi } from "vitest";
import { createQueryClient } from "@notrelix/query";
import { createSessionEventBus } from "@notrelix/runtime-web";
import type { SessionExpiredEvent } from "@notrelix/contracts";
import {
  createApplicationLifecycle,
  createWorkspaceEventSource,
} from "../application-lifecycle";

function createTerminalEvent(eventId: string): SessionExpiredEvent {
  return {
    eventId,
    occurredAt: "2026-08-08T00:00:00.000Z",
    reason: "session-revoked",
    error: {
      name: "AppError",
      message: "Session revoked",
      kind: "auth",
    } as unknown as SessionExpiredEvent["error"],
  };
}

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

describe("application lifecycle — session invalidation", () => {
  it("LIFE-001 terminal session-expired event clears QueryClient", () => {
    const queryClient = createQueryClient();
    const sessionEvents = createSessionEventBus();
    const realtime = createFakeRealtime();
    const lifecycle = createApplicationLifecycle({
      queryClient,
      realtime,
      sessionEvents,
      workspaceEvents: createWorkspaceEventSource(),
      clearSessionState: vi.fn(),
      clearWorkspaceState: vi.fn(),
      navigateToSignedOut: vi.fn(),
    });

    queryClient.setQueryData(["workspaces", "detail", "ws-1"], { name: "A" });

    sessionEvents.publish(createTerminalEvent("evt-1"));
    expect(queryClient.getQueryCache().getAll()).toHaveLength(0);
    lifecycle.dispose();
  });

  it("LIFE-002 terminal event clears auth/session state exactly once", () => {
    const queryClient = createQueryClient();
    const sessionEvents = createSessionEventBus();
    const clearSessionState = vi.fn();
    const lifecycle = createApplicationLifecycle({
      queryClient,
      realtime: createFakeRealtime(),
      sessionEvents,
      workspaceEvents: createWorkspaceEventSource(),
      clearSessionState,
      clearWorkspaceState: vi.fn(),
      navigateToSignedOut: vi.fn(),
    });

    sessionEvents.publish(createTerminalEvent("evt-2"));
    expect(clearSessionState).toHaveBeenCalledTimes(1);
    lifecycle.dispose();
  });

  it("LIFE-003 terminal event clears workspace transient state", () => {
    const queryClient = createQueryClient();
    const sessionEvents = createSessionEventBus();
    const clearWorkspaceState = vi.fn();
    const lifecycle = createApplicationLifecycle({
      queryClient,
      realtime: createFakeRealtime(),
      sessionEvents,
      workspaceEvents: createWorkspaceEventSource(),
      clearSessionState: vi.fn(),
      clearWorkspaceState,
      navigateToSignedOut: vi.fn(),
    });

    sessionEvents.publish(createTerminalEvent("evt-3"));
    expect(clearWorkspaceState).toHaveBeenCalledTimes(1);
    lifecycle.dispose();
  });

  it("LIFE-004 terminal event disconnects realtime but does not dispose runtime", () => {
    const queryClient = createQueryClient();
    const sessionEvents = createSessionEventBus();
    const realtime = createFakeRealtime();
    const lifecycle = createApplicationLifecycle({
      queryClient,
      realtime,
      sessionEvents,
      workspaceEvents: createWorkspaceEventSource(),
      clearSessionState: vi.fn(),
      clearWorkspaceState: vi.fn(),
      navigateToSignedOut: vi.fn(),
    });

    sessionEvents.publish(createTerminalEvent("evt-4"));
    expect(realtime.disconnect).toHaveBeenCalledWith("session-expired");
    expect(realtime.dispose).not.toHaveBeenCalled();
    lifecycle.dispose();
  });

  it("LIFE-005 terminal event navigates to existing signed-out route", () => {
    const queryClient = createQueryClient();
    const sessionEvents = createSessionEventBus();
    const navigateToSignedOut = vi.fn();
    const lifecycle = createApplicationLifecycle({
      queryClient,
      realtime: createFakeRealtime(),
      sessionEvents,
      workspaceEvents: createWorkspaceEventSource(),
      clearSessionState: vi.fn(),
      clearWorkspaceState: vi.fn(),
      navigateToSignedOut,
    });

    sessionEvents.publish(createTerminalEvent("evt-5"));
    expect(navigateToSignedOut).toHaveBeenCalledTimes(1);
    lifecycle.dispose();
  });

  it("LIFE-006 duplicate terminal event is idempotent", () => {
    const queryClient = createQueryClient();
    const sessionEvents = createSessionEventBus();
    const clearSessionState = vi.fn();
    const realtime = createFakeRealtime();
    const navigateToSignedOut = vi.fn();
    const lifecycle = createApplicationLifecycle({
      queryClient,
      realtime,
      sessionEvents,
      workspaceEvents: createWorkspaceEventSource(),
      clearSessionState,
      clearWorkspaceState: vi.fn(),
      navigateToSignedOut,
    });

    const event = createTerminalEvent("evt-6");
    sessionEvents.publish(event);
    sessionEvents.publish(event);

    expect(clearSessionState).toHaveBeenCalledTimes(1);
    expect(realtime.disconnect).toHaveBeenCalledTimes(1);
    expect(navigateToSignedOut).toHaveBeenCalledTimes(1);
    lifecycle.dispose();
  });

  it("LIFE-007 normal non-terminal token/session update does not clear cache", () => {
    const queryClient = createQueryClient();
    const sessionEvents = createSessionEventBus();
    const clearSessionState = vi.fn();
    const realtime = createFakeRealtime();
    const lifecycle = createApplicationLifecycle({
      queryClient,
      realtime,
      sessionEvents,
      workspaceEvents: createWorkspaceEventSource(),
      clearSessionState,
      clearWorkspaceState: vi.fn(),
      navigateToSignedOut: vi.fn(),
    });

    queryClient.setQueryData(["auth", "profile"], { id: "user-1" });
    queryClient.setQueryData(["workspaces", "detail", "ws-1"], { name: "A" });

    // Simulate a normal profile refresh that is not a terminal transition.
    queryClient.setQueryData(["auth", "profile"], {
      id: "user-1",
      name: "Updated",
    });

    expect(queryClient.getQueryCache().getAll().length).toBeGreaterThan(0);
    expect(clearSessionState).not.toHaveBeenCalled();
    expect(realtime.disconnect).not.toHaveBeenCalled();
    lifecycle.dispose();
  });

  it("LIFE-008 coordinator dispose unsubscribes session listener", () => {
    const queryClient = createQueryClient();
    const sessionEvents = createSessionEventBus();
    const clearSessionState = vi.fn();
    const lifecycle = createApplicationLifecycle({
      queryClient,
      realtime: createFakeRealtime(),
      sessionEvents,
      workspaceEvents: createWorkspaceEventSource(),
      clearSessionState,
      clearWorkspaceState: vi.fn(),
      navigateToSignedOut: vi.fn(),
    });

    lifecycle.dispose();
    sessionEvents.publish(createTerminalEvent("evt-8"));
    expect(clearSessionState).not.toHaveBeenCalled();
  });
});
