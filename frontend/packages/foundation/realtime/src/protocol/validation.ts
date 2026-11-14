import type { RealtimeEnvelope } from "./envelope";
import type {
  RealtimeControlMessage,
  RealtimeEphemeralFrame,
  RealtimeSubscriptionScope,
} from "./control-message";

export const SUPPORTED_SCHEMA_VERSIONS = new Set([1]);

/** `subscriptionId` limits of the gateway: <= 64 chars of `[A-Za-z0-9-_.:]`. */
const SUBSCRIPTION_ID_PATTERN = /^[A-Za-z0-9\-_.:]{1,64}$/;

export function isValidSubscriptionId(value: unknown): value is string {
  return typeof value === "string" && SUBSCRIPTION_ID_PATTERN.test(value);
}

export type RealtimeProtocolErrorReason =
  | "invalid-json"
  | "unsupported-schema-version"
  | "invalid-envelope"
  | "invalid-control-message"
  | "unknown-message-type";

export interface RealtimeProtocolError {
  readonly reason: RealtimeProtocolErrorReason;
  readonly message: string;
  readonly rawData?: unknown;
}

export type ParsedRealtimeMessage =
  | {
      readonly kind: "control";
      readonly message: RealtimeControlMessage;
    }
  | {
      readonly kind: "domain";
      readonly envelope: RealtimeEnvelope;
    }
  | {
      readonly kind: "ephemeral";
      readonly frame: RealtimeEphemeralFrame;
    };

export type RealtimeParseResult =
  | {
      readonly ok: true;
      readonly value: ParsedRealtimeMessage;
    }
  | {
      readonly ok: false;
      readonly error: RealtimeProtocolError;
    };

function isNonEmptyString(value: unknown): value is string {
  return typeof value === "string" && value.trim().length > 0;
}

function isOptionalNonNegativeNumber(value: unknown): boolean {
  return (
    value === undefined ||
    value === null ||
    (typeof value === "number" && Number.isFinite(value) && value >= 0)
  );
}

function parseScope(value: unknown): RealtimeSubscriptionScope | null {
  if (!value || typeof value !== "object") return null;
  const scope = value as Record<string, unknown>;
  if (
    (scope.kind === "workspace" ||
      scope.kind === "board" ||
      scope.kind === "channel" ||
      scope.kind === "user") &&
    isNonEmptyString(scope.id)
  ) {
    return { kind: scope.kind, id: scope.id };
  }
  return null;
}

/**
 * Validates an `event` frame and maps it to a `RealtimeEnvelope`.
 * Returns null when the structure is invalid.
 */
function parseEventFrame(
  record: Record<string, unknown>,
): RealtimeEnvelope | null {
  const streamVersion = record.streamVersion;
  if (
    typeof record.schemaVersion !== "number" ||
    record.schemaVersion <= 0 ||
    !isNonEmptyString(record.eventId) ||
    !isNonEmptyString(record.eventType) ||
    !isNonEmptyString(record.subscriptionId) ||
    !isNonEmptyString(record.streamKey) ||
    typeof streamVersion !== "number" ||
    !Number.isInteger(streamVersion) ||
    streamVersion < 0 ||
    !isNonEmptyString(record.workspaceId) ||
    !isNonEmptyString(record.resourceId) ||
    typeof record.occurredAt !== "string" ||
    Number.isNaN(Date.parse(record.occurredAt)) ||
    !isOptionalNonNegativeNumber(record.resourceVersion)
  ) {
    return null;
  }

  // Only the chat and per-user streams carry a payload; a notify-only frame has none.
  const payload = isPlainObject(record.payload) ? record.payload : undefined;
  const payloadOmitted =
    typeof record.payloadOmitted === "boolean"
      ? record.payloadOmitted
      : undefined;

  return {
    schemaVersion: record.schemaVersion,
    eventId: record.eventId,
    eventType: record.eventType,
    changeKind: typeof record.changeKind === "string" ? record.changeKind : "",
    subscriptionId: record.subscriptionId,
    streamKey: record.streamKey,
    streamVersion,
    resourceKind:
      typeof record.resourceKind === "string" ? record.resourceKind : "",
    resourceId: record.resourceId,
    aggregateId: record.resourceId,
    resourceVersion:
      typeof record.resourceVersion === "number"
        ? record.resourceVersion
        : undefined,
    workspaceId: record.workspaceId,
    correlationId:
      typeof record.correlationId === "string" ? record.correlationId : "",
    timestamp: record.occurredAt,
    payloadContract:
      typeof record.payloadContract === "string"
        ? record.payloadContract
        : undefined,
    ...(payload === undefined ? {} : { payload }),
    ...(payloadOmitted === undefined ? {} : { payloadOmitted }),
  };
}

function isPlainObject(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null && !Array.isArray(value);
}

function optionalNumber(value: unknown): number | undefined {
  return typeof value === "number" && Number.isFinite(value)
    ? value
    : undefined;
}

function parseControlFrame(
  record: Record<string, unknown>,
): RealtimeControlMessage | null {
  switch (record.type) {
    case "ready":
      if (!isNonEmptyString(record.connectionId)) return null;
      return {
        type: "ready",
        schemaVersion:
          typeof record.schemaVersion === "number" ? record.schemaVersion : 1,
        connectionId: record.connectionId,
        pingIntervalMs: optionalNumber(record.pingIntervalMs),
        idleTimeoutMs: optionalNumber(record.idleTimeoutMs),
        maxSubscriptions: optionalNumber(record.maxSubscriptions),
      };
    case "subscribed": {
      const scope = parseScope(record.scope);
      if (
        !isNonEmptyString(record.subscriptionId) ||
        !scope ||
        !isNonEmptyString(record.streamKey)
      ) {
        return null;
      }
      return {
        type: "subscribed",
        subscriptionId: record.subscriptionId,
        scope,
        streamKey: record.streamKey,
      };
    }
    case "unsubscribed":
      if (!isNonEmptyString(record.subscriptionId)) return null;
      return { type: "unsubscribed", subscriptionId: record.subscriptionId };
    case "access-changed":
      if (!isNonEmptyString(record.workspaceId)) return null;
      return { type: "access-changed", workspaceId: record.workspaceId };
    case "subscription-error":
      if (!isNonEmptyString(record.code)) return null;
      return {
        type: "subscription-error",
        subscriptionId: isNonEmptyString(record.subscriptionId)
          ? record.subscriptionId
          : undefined,
        code: record.code,
      };
    case "ping":
    case "pong":
      return {
        type: record.type,
        sentAt: typeof record.sentAt === "string" ? record.sentAt : undefined,
      };
    default:
      return null;
  }
}

function parseEphemeralFrame(
  record: Record<string, unknown>,
): RealtimeEphemeralFrame | null {
  if (
    !isNonEmptyString(record.subscriptionId) ||
    !isNonEmptyString(record.userId)
  ) {
    return null;
  }
  if (record.type === "typing") {
    const expiresInMs = optionalNumber(record.expiresInMs);
    if (expiresInMs === undefined || expiresInMs < 0) return null;
    return {
      type: "typing",
      subscriptionId: record.subscriptionId,
      userId: record.userId,
      expiresInMs,
    };
  }
  const ttlMs = optionalNumber(record.ttlMs);
  if (
    (record.state !== "online" &&
      record.state !== "idle" &&
      record.state !== "offline") ||
    ttlMs === undefined ||
    ttlMs < 0
  ) {
    return null;
  }
  return {
    type: "presence",
    subscriptionId: record.subscriptionId,
    userId: record.userId,
    state: record.state,
    ttlMs,
  };
}

const CONTROL_TYPES = new Set([
  "ready",
  "subscribed",
  "unsubscribed",
  "access-changed",
  "subscription-error",
  "ping",
  "pong",
]);

export function parseRealtimeMessage(input: unknown): RealtimeParseResult {
  let data = input;
  if (typeof input === "string") {
    try {
      data = JSON.parse(input);
    } catch {
      return {
        ok: false,
        error: {
          reason: "invalid-json",
          message: "Failed to parse JSON string message.",
          rawData: input,
        },
      };
    }
  }

  if (!data || typeof data !== "object") {
    return {
      ok: false,
      error: {
        reason: "unknown-message-type",
        message: "Message is not an object.",
        rawData: data,
      },
    };
  }

  const record = data as Record<string, unknown>;

  if (typeof record.type === "string" && CONTROL_TYPES.has(record.type)) {
    const message = parseControlFrame(record);
    if (message) {
      return { ok: true, value: { kind: "control", message } };
    }
    return {
      ok: false,
      error: {
        reason: "invalid-control-message",
        message: `Malformed control message of type '${record.type}'.`,
        rawData: record,
      },
    };
  }

  if (record.type === "typing" || record.type === "presence") {
    const frame = parseEphemeralFrame(record);
    if (!frame) {
      return {
        ok: false,
        error: {
          reason: "invalid-control-message",
          message: `Malformed ephemeral message of type '${record.type}'.`,
          rawData: record,
        },
      };
    }
    return { ok: true, value: { kind: "ephemeral", frame } };
  }

  if (record.type === "event") {
    const envelope = parseEventFrame(record);
    if (!envelope) {
      return {
        ok: false,
        error: {
          reason: "invalid-envelope",
          message: "Frame does not satisfy the event schema.",
          rawData: record,
        },
      };
    }
    if (!SUPPORTED_SCHEMA_VERSIONS.has(envelope.schemaVersion)) {
      return {
        ok: false,
        error: {
          reason: "unsupported-schema-version",
          message: `Unsupported schema version: ${envelope.schemaVersion}.`,
          rawData: record,
        },
      };
    }
    return { ok: true, value: { kind: "domain", envelope } };
  }

  return {
    ok: false,
    error: {
      reason: "unknown-message-type",
      message: `Unknown message type '${String(record.type)}'.`,
      rawData: record,
    },
  };
}
