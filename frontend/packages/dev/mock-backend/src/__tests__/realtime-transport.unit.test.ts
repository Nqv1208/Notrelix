import { describe, it, expect } from "vitest";
import {
  createMockRealtimeTransport,
  MockRealtimeTransport,
} from "../realtime/mock-realtime-transport";

describe("MFB-FZ-01: MockRealtimeTransport (Zero-Network Realtime)", () => {
  it("initializes in idle state and transitions to connected without WebSocket", async () => {
    const transport = createMockRealtimeTransport();
    expect(transport.getState()).toBe("idle");

    const states: string[] = [];
    const unsubscribe = transport.subscribeState((state: string) => {
      states.push(state);
    });

    await transport.connect({ sessionGeneration: "gen-1" });
    expect(transport.getState()).toBe("connected");
    expect(states).toContain("connected");

    transport.disconnect("test");
    expect(transport.getState()).toBe("closed");

    unsubscribe();
    transport.dispose();
  });

  it("handles subscription lifecycle deterministically", () => {
    const transport = new MockRealtimeTransport();
    const unsubscribe = transport.subscribe(
      { workspaceId: "ws-0001" },
      () => {},
    );

    expect(typeof unsubscribe).toBe("function");
    unsubscribe();
    transport.dispose();
  });

  it("delivers gateway-shaped events only to the subscribed workspace", () => {
    const transport = new MockRealtimeTransport();
    const received: string[] = [];
    transport.subscribe({ workspaceId: "ws-1" }, (envelope) => {
      received.push(envelope.eventId);
    });

    const base = {
      schemaVersion: 1,
      eventType: "workspace.updated",
      changeKind: "updated",
      subscriptionId: "workspace:ws-1",
      streamKey: "workspace:Workspace:ws1",
      streamVersion: 1,
      resourceKind: "Workspace",
      resourceId: "ws-1",
      aggregateId: "ws-1",
      correlationId: "corr-1",
      timestamp: "2026-01-01T00:00:00.000Z",
    };
    transport.deliverEvent({ ...base, eventId: "evt-1", workspaceId: "ws-1" });
    transport.deliverEvent({ ...base, eventId: "evt-2", workspaceId: "ws-2" });

    expect(received).toEqual(["evt-1"]);
    transport.dispose();
  });

  it("delivers recovery signals to recovery listeners", () => {
    const transport = new MockRealtimeTransport();
    const reasons: string[] = [];
    transport.subscribeRecovery((signal) => reasons.push(signal.reason));

    transport.deliverRecovery({
      reason: "gap",
      workspaceId: "ws-1",
      scope: { kind: "workspace", id: "ws-1" },
      subscriptionId: "workspace:ws-1",
      expected: 2,
      received: 5,
    });

    expect(reasons).toEqual(["gap"]);
    transport.dispose();
  });

  it("handles recovery subscription lifecycle", () => {
    const transport = new MockRealtimeTransport();
    const unsubscribe = transport.subscribeRecovery(() => {});
    expect(typeof unsubscribe).toBe("function");
    unsubscribe();
    transport.dispose();
  });
});
