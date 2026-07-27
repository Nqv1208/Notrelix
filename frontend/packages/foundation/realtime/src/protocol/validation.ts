import type { RealtimeEnvelope } from './envelope';
import type { RealtimeControlMessage } from './control-message';

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

  return (
    hasSchemaVersion &&
    hasEventId &&
    hasEventType &&
    hasWorkspaceId &&
    hasCorrelationId &&
    hasValidTimestamp &&
    hasPayload
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
