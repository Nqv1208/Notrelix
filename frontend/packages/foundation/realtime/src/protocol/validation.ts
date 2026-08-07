import type { RealtimeEnvelope } from './envelope';
import type { RealtimeControlMessage } from './control-message';

export const SUPPORTED_SCHEMA_VERSIONS = new Set([1]);

export type RealtimeProtocolErrorReason =
  | 'invalid-json'
  | 'unsupported-schema-version'
  | 'invalid-envelope'
  | 'invalid-control-message'
  | 'unknown-message-type';

export interface RealtimeProtocolError {
  readonly reason: RealtimeProtocolErrorReason;
  readonly message: string;
  readonly rawData?: unknown;
}

export type ParsedRealtimeMessage =
  | {
      readonly kind: 'control';
      readonly message: RealtimeControlMessage;
    }
  | {
      readonly kind: 'domain';
      readonly envelope: RealtimeEnvelope<unknown>;
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

export function isValidEnvelope(data: unknown): data is RealtimeEnvelope {
  if (!data || typeof data !== 'object') return false;
  const e = data as Record<string, unknown>;

  const hasSchemaVersion = typeof e.schemaVersion === 'number' && e.schemaVersion > 0;
  const hasEventId = typeof e.eventId === 'string' && e.eventId.trim().length > 0;
  const hasEventType = typeof e.eventType === 'string' && e.eventType.trim().length > 0;
  const hasWorkspaceId = typeof e.workspaceId === 'string' && e.workspaceId.trim().length > 0;
  const hasCorrelationId = typeof e.correlationId === 'string' && e.correlationId.trim().length > 0;
  const hasValidTimestamp = typeof e.timestamp === 'string' && !isNaN(Date.parse(e.timestamp));
  const hasPayload = 'payload' in e && e.payload !== undefined;

  const validSequence = e.sequence === undefined || (typeof e.sequence === 'number' && e.sequence >= 0);
  const validAggregateVersion = e.aggregateVersion === undefined || (typeof e.aggregateVersion === 'number' && e.aggregateVersion >= 0);

  return (
    hasSchemaVersion &&
    hasEventId &&
    hasEventType &&
    hasWorkspaceId &&
    hasCorrelationId &&
    hasValidTimestamp &&
    hasPayload &&
    validSequence &&
    validAggregateVersion
  );
}

export function isValidControlMessage(data: unknown): data is RealtimeControlMessage {
  if (!data || typeof data !== 'object') return false;
  const msg = data as Record<string, unknown>;

  if (typeof msg.type !== 'string') return false;

  switch (msg.type) {
    case 'ping':
    case 'pong':
      return typeof msg.sentAt === 'string';
    case 'subscribed':
      return typeof msg.subscriptionId === 'string';
    case 'subscription-error':
      return typeof msg.code === 'string';
    default:
      return false;
  }
}

export function parseRealtimeMessage(input: unknown): RealtimeParseResult {
  let data = input;
  if (typeof input === 'string') {
    try {
      data = JSON.parse(input);
    } catch {
      return {
        ok: false,
        error: {
          reason: 'invalid-json',
          message: 'Failed to parse JSON string message.',
          rawData: input,
        },
      };
    }
  }

  if (!data || typeof data !== 'object') {
    return {
      ok: false,
      error: {
        reason: 'unknown-message-type',
        message: 'Message is not an object.',
        rawData: data,
      },
    };
  }

  const record = data as Record<string, unknown>;

  // Check if it's a control message
  if (typeof record.type === 'string' && ['ping', 'pong', 'subscribed', 'subscription-error'].includes(record.type)) {
    if (isValidControlMessage(record)) {
      return {
        ok: true,
        value: {
          kind: 'control',
          message: record as unknown as RealtimeControlMessage,
        },
      };
    }
    return {
      ok: false,
      error: {
        reason: 'invalid-control-message',
        message: `Malformed control message of type '${record.type}'.`,
        rawData: record,
      },
    };
  }

  // Check if it's a domain envelope
  if (isValidEnvelope(record)) {
    if (!SUPPORTED_SCHEMA_VERSIONS.has(record.schemaVersion)) {
      return {
        ok: false,
        error: {
          reason: 'unsupported-schema-version',
          message: `Unsupported schema version: ${record.schemaVersion}.`,
          rawData: record,
        },
      };
    }

    return {
      ok: true,
      value: {
        kind: 'domain',
        envelope: record as unknown as RealtimeEnvelope<unknown>,
      },
    };
  }

  return {
    ok: false,
    error: {
      reason: 'invalid-envelope',
      message: 'Payload does not satisfy domain envelope schema.',
      rawData: record,
    },
  };
}
