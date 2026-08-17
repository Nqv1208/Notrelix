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

  it("handles recovery subscription lifecycle", () => {
    const transport = new MockRealtimeTransport();
    const unsubscribe = transport.subscribeRecovery(() => {});
    expect(typeof unsubscribe).toBe("function");
    unsubscribe();
    transport.dispose();
  });
});
