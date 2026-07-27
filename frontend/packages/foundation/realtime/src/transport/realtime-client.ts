/**
 * RealtimeClient - Enterprise WebSocket transport
 *
 * Key design decisions:
 * - isManualClose flag prevents reconnect loops on intentional disconnects (logout, unmount)
 * - Exponential backoff with jitter avoids thundering herd reconnect storms
 * - Envelope validation rejects malformed messages before dispatching to listeners
 * - Event deduplication LRU cache prevents processing duplicate eventId deliveries
 * - Heartbeat (30s ping) keeps connection alive across proxies/load balancers
 * - No singleton exported - instantiate via AppRuntime composition root
 */

export type RealtimeConnectionState =
  | 'connecting'
  | 'connected'
  | 'reconnecting'
  | 'disconnected'
  | 'failed';

export interface RealtimeEnvelope<TPayload = unknown> {
  /** Unique event identifier for deduplication */
  eventId: string;
  /** Domain event type (e.g. 'board.item.moved') */
  eventType: string;
  tenantId?: string;
  workspaceId: string;
  aggregateId?: string;
  /** Schema version for forward compatibility */
  schemaVersion: number;
  /** Aggregate version for optimistic concurrency */
  aggregateVersion?: number;
  /** Global monotonic sequence number for gap detection */
  sequence?: number;
  /** Tracing correlation ID matching the originating HTTP request */
  correlationId: string;
  /** ID of the event that caused this event */
  causationId?: string;
  /** Subscription channel identifier */
  subscriptionId?: string;
  /** ISO-8601 timestamp */
  timestamp: string;
  payload: TPayload;
}

export type RealtimeEvent = RealtimeEnvelope<unknown>;

export type ConnectionStateListener = (state: RealtimeConnectionState) => void;
export type EventListener<T = unknown> = (envelope: RealtimeEnvelope<T>) => void;

function isValidEnvelope(data: unknown): data is RealtimeEnvelope {
  if (!data || typeof data !== 'object') return false;
  const e = data as Record<string, unknown>;
  return (
    typeof e.eventId === 'string' &&
    typeof e.eventType === 'string' &&
    typeof e.workspaceId === 'string' &&
    typeof e.correlationId === 'string' &&
    typeof e.timestamp === 'string'
  );
}

export class RealtimeClient {
  private socket: WebSocket | null = null;
  private state: RealtimeConnectionState = 'disconnected';
  private eventListeners: Set<EventListener> = new Set();
  private stateListeners: Set<ConnectionStateListener> = new Set();
  private reconnectAttempts = 0;
  private readonly maxReconnectAttempts = 10;
  private readonly baseReconnectDelayMs = 1000;
  private readonly maxReconnectDelayMs = 30_000;
  private reconnectTimer: ReturnType<typeof setTimeout> | null = null;

  /** Prevents onclose from scheduling a reconnect after intentional disconnect() */
  private isManualClose = false;

  /** Heartbeat timer (30s interval) */
  private heartbeatTimer: ReturnType<typeof setInterval> | null = null;
  private readonly heartbeatIntervalMs = 30_000;

  /** LRU cache for eventId deduplication */
  private readonly seenEventIds: Set<string> = new Set();
  private readonly maxDeduplicationCacheSize = 1000;

  constructor(private readonly url: string) {}

  public getState(): RealtimeConnectionState {
    return this.state;
  }

  private setState(newState: RealtimeConnectionState): void {
    if (this.state !== newState) {
      this.state = newState;
      this.stateListeners.forEach((listener) => listener(newState));
    }
  }

  public connect(): void {
    if (this.state === 'connected' || this.state === 'connecting') return;

    // Reset manual-close flag on an explicit connect() call
    this.isManualClose = false;

    this.setState(this.reconnectAttempts > 0 ? 'reconnecting' : 'connecting');

    try {
      this.socket = new WebSocket(this.url);

      this.socket.onopen = () => {
        this.reconnectAttempts = 0;
        this.setState('connected');
        this.startHeartbeat();
      };

      this.socket.onmessage = (event) => {
        let parsed: unknown;
        try {
          parsed = JSON.parse(event.data as string);
        } catch {
          console.warn('[RealtimeClient] Received non-JSON message, ignoring.');
          return;
        }

        if (!isValidEnvelope(parsed)) {
          console.warn('[RealtimeClient] Received invalid envelope, ignoring:', parsed);
          return;
        }

        // Deduplication check
        if (this.isDuplicateEvent(parsed.eventId)) {
          console.debug('[RealtimeClient] Duplicate eventId ignored:', parsed.eventId);
          return;
        }

        this.recordEventId(parsed.eventId);
        this.eventListeners.forEach((listener) => listener(parsed as RealtimeEnvelope));
      };

      this.socket.onclose = () => {
        this.stopHeartbeat();
        this.socket = null;
        if (this.isManualClose) {
          // Intentional disconnect - do NOT schedule reconnect
          this.setState('disconnected');
        } else {
          // Unexpected close (network drop, server restart, etc.) - reconnect
          this.scheduleReconnect();
        }
      };

      this.socket.onerror = () => {
        // onerror is always followed by onclose; let onclose drive state transitions
        console.warn('[RealtimeClient] WebSocket error encountered.');
      };
    } catch (e) {
      console.error('[RealtimeClient] Failed to create WebSocket:', e);
      this.socket = null;
      this.scheduleReconnect();
    }
  }

  private isDuplicateEvent(eventId: string): boolean {
    return this.seenEventIds.has(eventId);
  }

  private recordEventId(eventId: string): void {
    if (this.seenEventIds.size >= this.maxDeduplicationCacheSize) {
      // Evict oldest inserted eventId
      const oldestKey = this.seenEventIds.values().next().value;
      if (oldestKey !== undefined) {
        this.seenEventIds.delete(oldestKey);
      }
    }
    this.seenEventIds.add(eventId);
  }

  private startHeartbeat(): void {
    this.stopHeartbeat();
    this.heartbeatTimer = setInterval(() => {
      if (this.socket && this.socket.readyState === WebSocket.OPEN) {
        try {
          this.socket.send(JSON.stringify({ type: 'ping', timestamp: new Date().toISOString() }));
        } catch (err) {
          console.warn('[RealtimeClient] Failed to send heartbeat ping:', err);
        }
      }
    }, this.heartbeatIntervalMs);
  }

  private stopHeartbeat(): void {
    if (this.heartbeatTimer) {
      clearInterval(this.heartbeatTimer);
      this.heartbeatTimer = null;
    }
  }

  private scheduleReconnect(): void {
    if (this.reconnectAttempts >= this.maxReconnectAttempts) {
      this.setState('failed');
      return;
    }

    this.reconnectAttempts++;
    const exponentialDelay = this.baseReconnectDelayMs * Math.pow(2, this.reconnectAttempts - 1);
    // Add +-25% jitter to prevent thundering herd reconnect storms
    const jitter = exponentialDelay * 0.25 * (Math.random() * 2 - 1);
    const delay = Math.min(exponentialDelay + jitter, this.maxReconnectDelayMs);

    this.setState('reconnecting');
    if (this.reconnectTimer) clearTimeout(this.reconnectTimer);
    this.reconnectTimer = setTimeout(() => this.connect(), Math.max(delay, 0));
  }

  public subscribe<T = unknown>(listener: EventListener<T>): () => void {
    this.eventListeners.add(listener as EventListener);
    return () => {
      this.eventListeners.delete(listener as EventListener);
    };
  }

  public onStateChange(listener: ConnectionStateListener): () => void {
    this.stateListeners.add(listener);
    return () => {
      this.stateListeners.delete(listener);
    };
  }

  /**
   * Intentionally close the WebSocket and prevent automatic reconnection.
   * Call this on logout, component unmount, or explicit user action.
   */
  public disconnect(): void {
    // Mark as intentional BEFORE closing the socket so that the onclose
    // handler knows not to schedule a reconnect.
    this.isManualClose = true;

    this.stopHeartbeat();

    if (this.reconnectTimer) {
      clearTimeout(this.reconnectTimer);
      this.reconnectTimer = null;
    }
    this.reconnectAttempts = 0;

    if (this.socket) {
      this.socket.close();
      // onclose will fire and call setState('disconnected') since isManualClose === true
    } else {
      this.setState('disconnected');
    }
  }
}
