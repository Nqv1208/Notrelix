import { describe, it, expect } from "vitest";
import { parseRealtimeMessage } from "../validation";

describe("Realtime Protocol Parsing", () => {
  const validEnvelope = {
    schemaVersion: 1,
    eventId: "evt-100",
    eventType: "board.item.updated",
    workspaceId: "ws-123",
    correlationId: "corr-456",
    timestamp: new Date().toISOString(),
    payload: { id: "item-1", status: "done" },
  };

  it("parses valid domain envelope", () => {
    const res = parseRealtimeMessage(validEnvelope);
    expect(res.ok).toBe(true);
    if (res.ok && res.value.kind === "domain") {
      expect(res.value.envelope.eventId).toBe("evt-100");
    } else {
      throw new Error("Expected ok domain result");
    }
  });

  it("rejects envelope missing payload", () => {
    const { payload: _payload, ...invalid } = validEnvelope;
    const res = parseRealtimeMessage(invalid);
    expect(res.ok).toBe(false);
    if (!res.ok) {
      expect(res.error.reason).toBe("invalid-envelope");
    }
  });

  it("rejects envelope with invalid timestamp", () => {
    const res = parseRealtimeMessage({
      ...validEnvelope,
      timestamp: "invalid-date",
    });
    expect(res.ok).toBe(false);
    if (!res.ok) {
      expect(res.error.reason).toBe("invalid-envelope");
    }
  });

  it("rejects unsupported schema version", () => {
    const res = parseRealtimeMessage({ ...validEnvelope, schemaVersion: 999 });
    expect(res.ok).toBe(false);
    if (!res.ok) {
      expect(res.error.reason).toBe("unsupported-schema-version");
    }
  });

  it("rejects negative sequence number", () => {
    const res = parseRealtimeMessage({ ...validEnvelope, sequence: -1 });
    expect(res.ok).toBe(false);
    if (!res.ok) {
      expect(res.error.reason).toBe("invalid-envelope");
    }
  });

  it("parses control message: ping", () => {
    const res = parseRealtimeMessage({
      type: "ping",
      sentAt: new Date().toISOString(),
    });
    expect(res.ok).toBe(true);
    if (res.ok && res.value.kind === "control") {
      expect(res.value.message.type).toBe("ping");
    } else {
      throw new Error("Expected ok control result");
    }
  });

  it("parses control message: pong", () => {
    const res = parseRealtimeMessage({
      type: "pong",
      sentAt: new Date().toISOString(),
    });
    expect(res.ok).toBe(true);
    if (res.ok && res.value.kind === "control") {
      expect(res.value.message.type).toBe("pong");
    } else {
      throw new Error("Expected ok control result");
    }
  });

  it("parses control message: subscribed", () => {
    const res = parseRealtimeMessage({
      type: "subscribed",
      subscriptionId: "sub-1",
    });
    expect(res.ok).toBe(true);
    if (res.ok && res.value.kind === "control") {
      expect(res.value.message.type).toBe("subscribed");
    } else {
      throw new Error("Expected ok control result");
    }
  });

  it("parses control message: subscription-error", () => {
    const res = parseRealtimeMessage({
      type: "subscription-error",
      code: "unauthorized",
    });
    expect(res.ok).toBe(true);
    if (res.ok && res.value.kind === "control") {
      expect(res.value.message.type).toBe("subscription-error");
    } else {
      throw new Error("Expected ok control result");
    }
  });

  it("handles malformed JSON string gracefully", () => {
    const res = parseRealtimeMessage("{ invalid json");
    expect(res.ok).toBe(false);
    if (!res.ok) {
      expect(res.error.reason).toBe("invalid-json");
    }
  });

  it("handles unknown object message", () => {
    const res = parseRealtimeMessage({ unknownField: true });
    expect(res.ok).toBe(false);
    if (!res.ok) {
      expect(res.error.reason).toBe("invalid-envelope");
    }
  });

  it("parses JSON string envelope and preserves payload", () => {
    const jsonStr = JSON.stringify(validEnvelope);
    const res = parseRealtimeMessage(jsonStr);
    expect(res.ok).toBe(true);
    if (res.ok && res.value.kind === "domain") {
      expect(res.value.envelope.payload).toEqual({
        id: "item-1",
        status: "done",
      });
    } else {
      throw new Error("Expected ok domain result");
    }
  });
});
