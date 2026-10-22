import { describe, it, expect } from "vitest";
import { isValidSubscriptionId, parseRealtimeMessage } from "../validation";

describe("Realtime Protocol Parsing (gateway v1)", () => {
  const eventFrame = {
    type: "event",
    subscriptionId: "workspace:ws-123",
    schemaVersion: 1,
    eventId: "evt-100",
    eventType: "workspace.updated",
    changeKind: "updated",
    streamKey: "workspace:Workspace:ws123",
    streamVersion: 12,
    resourceKind: "Workspace",
    resourceId: "res-1",
    resourceVersion: 4,
    workspaceId: "ws-123",
    correlationId: "corr-456",
    occurredAt: "2026-01-01T00:00:00.000Z",
    payloadContract: "workspace.updated.v1",
  };

  it("maps an event frame to the envelope (occurredAt, resourceId, no payload)", () => {
    const res = parseRealtimeMessage(eventFrame);
    expect(res.ok).toBe(true);
    if (!res.ok || res.value.kind !== "domain") {
      throw new Error("Expected ok domain result");
    }
    expect(res.value.envelope).toEqual({
      schemaVersion: 1,
      eventId: "evt-100",
      eventType: "workspace.updated",
      changeKind: "updated",
      subscriptionId: "workspace:ws-123",
      streamKey: "workspace:Workspace:ws123",
      streamVersion: 12,
      resourceKind: "Workspace",
      resourceId: "res-1",
      aggregateId: "res-1",
      resourceVersion: 4,
      workspaceId: "ws-123",
      correlationId: "corr-456",
      timestamp: "2026-01-01T00:00:00.000Z",
      payloadContract: "workspace.updated.v1",
    });
    expect("payload" in res.value.envelope).toBe(false);
  });

  it("parses a JSON string frame", () => {
    const res = parseRealtimeMessage(JSON.stringify(eventFrame));
    expect(res.ok).toBe(true);
  });

  it.each([
    "eventId",
    "eventType",
    "subscriptionId",
    "streamKey",
    "streamVersion",
    "workspaceId",
    "resourceId",
    "occurredAt",
  ])("rejects an event frame missing %s", (field) => {
    const invalid: Record<string, unknown> = { ...eventFrame };
    delete invalid[field];
    const res = parseRealtimeMessage(invalid);
    expect(res.ok).toBe(false);
    if (!res.ok) expect(res.error.reason).toBe("invalid-envelope");
  });

  it("rejects an invalid occurredAt", () => {
    const res = parseRealtimeMessage({ ...eventFrame, occurredAt: "nope" });
    expect(res.ok).toBe(false);
    if (!res.ok) expect(res.error.reason).toBe("invalid-envelope");
  });

  it("rejects a negative or fractional streamVersion", () => {
    for (const streamVersion of [-1, 1.5, "3"]) {
      const res = parseRealtimeMessage({ ...eventFrame, streamVersion });
      expect(res.ok).toBe(false);
    }
  });

  it("rejects an unsupported schema version", () => {
    const res = parseRealtimeMessage({ ...eventFrame, schemaVersion: 999 });
    expect(res.ok).toBe(false);
    if (!res.ok) expect(res.error.reason).toBe("unsupported-schema-version");
  });

  it("tolerates a missing resourceVersion, correlationId and payloadContract", () => {
    const {
      resourceVersion: _r,
      correlationId: _c,
      payloadContract: _p,
      ...rest
    } = eventFrame;
    const res = parseRealtimeMessage(rest);
    expect(res.ok).toBe(true);
    if (res.ok && res.value.kind === "domain") {
      expect(res.value.envelope.resourceVersion).toBeUndefined();
      expect(res.value.envelope.correlationId).toBe("");
    }
  });

  it("parses ready", () => {
    const res = parseRealtimeMessage({
      type: "ready",
      schemaVersion: 1,
      connectionId: "c-1",
      pingIntervalMs: 25000,
      idleTimeoutMs: 75000,
      maxSubscriptions: 10,
    });
    expect(res.ok).toBe(true);
    if (res.ok && res.value.kind === "control") {
      expect(res.value.message).toEqual({
        type: "ready",
        schemaVersion: 1,
        connectionId: "c-1",
        pingIntervalMs: 25000,
        idleTimeoutMs: 75000,
        maxSubscriptions: 10,
      });
    } else {
      throw new Error("Expected ok control result");
    }
  });

  it("parses subscribed with scope and streamKey, rejecting a bad scope", () => {
    const ok = parseRealtimeMessage({
      type: "subscribed",
      subscriptionId: "board:b1",
      scope: { kind: "board", id: "b1" },
      streamKey: "board:Board:b1",
    });
    expect(ok.ok).toBe(true);

    const bad = parseRealtimeMessage({
      type: "subscribed",
      subscriptionId: "x:1",
      scope: { kind: "document", id: "1" },
      streamKey: "k",
    });
    expect(bad.ok).toBe(false);
    if (!bad.ok) expect(bad.error.reason).toBe("invalid-control-message");
  });

  it("parses unsubscribed, subscription-error, ping and pong", () => {
    for (const frame of [
      { type: "unsubscribed", subscriptionId: "workspace:w" },
      { type: "subscription-error", code: "subscription_revoked" },
      { type: "subscription-error", subscriptionId: "w", code: "new_code" },
      { type: "ping", sentAt: "2026-01-01T00:00:00Z" },
      { type: "pong", sentAt: "2026-01-01T00:00:00Z" },
      { type: "pong" },
    ]) {
      const res = parseRealtimeMessage(frame);
      expect(res.ok, JSON.stringify(frame)).toBe(true);
      if (res.ok) expect(res.value.kind).toBe("control");
    }
  });

  it("rejects malformed control frames", () => {
    for (const frame of [
      { type: "ready" },
      { type: "unsubscribed" },
      { type: "subscription-error" },
    ]) {
      const res = parseRealtimeMessage(frame);
      expect(res.ok, JSON.stringify(frame)).toBe(false);
      if (!res.ok) expect(res.error.reason).toBe("invalid-control-message");
    }
  });

  it("handles malformed JSON string gracefully", () => {
    const res = parseRealtimeMessage("{ invalid json");
    expect(res.ok).toBe(false);
    if (!res.ok) expect(res.error.reason).toBe("invalid-json");
  });

  it("rejects objects with an unknown or missing type", () => {
    for (const frame of [{ unknownField: true }, { type: "mystery" }]) {
      const res = parseRealtimeMessage(frame);
      expect(res.ok).toBe(false);
      if (!res.ok) expect(res.error.reason).toBe("unknown-message-type");
    }
  });
});

describe("isValidSubscriptionId", () => {
  it("accepts gateway-legal ids and rejects illegal ones", () => {
    expect(isValidSubscriptionId("workspace:3f2a-AB_c.d")).toBe(true);
    expect(isValidSubscriptionId("a".repeat(64))).toBe(true);
    expect(isValidSubscriptionId("a".repeat(65))).toBe(false);
    expect(isValidSubscriptionId("")).toBe(false);
    expect(isValidSubscriptionId("has space")).toBe(false);
    expect(isValidSubscriptionId("slash/inside")).toBe(false);
    expect(isValidSubscriptionId(42)).toBe(false);
  });
});
