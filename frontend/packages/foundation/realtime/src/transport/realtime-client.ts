import { parseRealtimeMessage, type RealtimeEnvelope, type RealtimeControlMessage } from '../protocol';

export type RealtimeConnectionState =
  | 'idle'
  | 'connecting'
  | 'connected'
  | 'reconnecting'
  | 'offline'
  | 'closed'
  | 'failed';

export interface RealtimeConnectContext {
  readonly sessionGeneration: string;
}

export interface RealtimeConnectionDescriptor {
  readonly url: string;
  readonly protocols?: readonly string[];
}

export interface RealtimeConnectionDescriptorProvider {
  getDescriptor(context: RealtimeConnectContext): Promise<RealtimeConnectionDescriptor>;
}

export function createCookieConnectionDescriptorProvider(options: {
  realtimeUrl: string;
}): RealtimeConnectionDescriptorProvider {
  return {
    getDescriptor: async () => ({
      url: options.realtimeUrl,
    }),
  };
}

export interface WebSocketLike {
  readyState: number;
  send(data: string): void;
  close(code?: number, reason?: string): void;
  onopen: ((event: any) => void) | null;
  onmessage: ((event: { data: any }) => void) | null;
  onclose: ((event: { code: number; reason: string }) => void) | null;
  onerror: ((event: any) => void) | null;
}

export type WebSocketFactory = (descriptor: RealtimeConnectionDescriptor) => WebSocketLike;

export const defaultBrowserWebSocketFactory: WebSocketFactory = (descriptor) => {
  if (typeof WebSocket === 'undefined') {
    throw new Error('WebSocket is not supported in this environment.');
  }
  // Cast needed: browser WebSocket event handlers have stricter 'this' bindings
  // than our minimal WebSocketLike contract, but they are structurally compatible at runtime.
  return new WebSocket(descriptor.url, descriptor.protocols ? [...descriptor.protocols] : undefined) as unknown as WebSocketLike;
};

export interface RealtimeSubscriptionFilter {
  readonly workspaceId: string;
  readonly eventTypes?: readonly string[];
  readonly subscriptionId?: string;
}

export interface RealtimeSequenceGap {
  readonly workspaceId: string;
  readonly subscriptionId?: string;
  readonly expected: number;
  readonly received: number;
}

export type RealtimeEventListener = (envelope: RealtimeEnvelope<unknown>) => void;
export type RealtimeStateListener = (state: RealtimeConnectionState) => void;
export type RealtimeRecoveryListener = (gap: RealtimeSequenceGap) => void;

export interface RealtimeTelemetry {
  track(event: string, properties?: Record<string, unknown>): void;
  reportError(error: unknown, context?: Record<string, unknown>): void;
}

export interface RealtimeClientOptions {
  readonly connectionDescriptorProvider?: RealtimeConnectionDescriptorProvider;
  readonly socketFactory?: WebSocketFactory;
  readonly clock?: { now(): Date };
  readonly scheduler?: {
    setTimeout(fn: () => void, delayMs: number): unknown;
    clearTimeout(id: unknown): void;
    setInterval(fn: () => void, intervalMs: number): unknown;
    clearInterval(id: unknown): void;
  };
  readonly random?: () => number;
  readonly telemetry?: RealtimeTelemetry;

  readonly reconnect?: {
    readonly initialDelayMs?: number;
    readonly maximumDelayMs?: number;
    readonly maximumAttempts?: number | 'unlimited';
  };

  readonly heartbeat?: {
    readonly intervalMs?: number;
    readonly pongTimeoutMs?: number;
    readonly maximumMissedPongs?: number;
  };

  readonly deduplication?: {
    readonly maximumEntries?: number;
    readonly ttlMs?: number;
  };
}

export interface RealtimeTransport {
  connect(context: RealtimeConnectContext): Promise<void>;
  disconnect(reason?: string): void;
  subscribe(filter: RealtimeSubscriptionFilter, listener: RealtimeEventListener): () => void;
  subscribeState(listener: RealtimeStateListener): () => void;
  subscribeRecovery(listener: RealtimeRecoveryListener): () => void;
  getState(): RealtimeConnectionState;
  dispose(): void;
}

// Bounded TTL LRU Cache for deduplication
class DedupLruCache {
  private cache = new Map<string, number>();

  constructor(
    private readonly maxEntries: number,
    private readonly ttlMs: number
  ) {}

  public has(eventId: string, now: number): boolean {
    const timestamp = this.cache.get(eventId);
    if (timestamp === undefined) return false;
    if (now - timestamp > this.ttlMs) {
      this.cache.delete(eventId);
      return false;
    }
    return true;
  }

  public add(eventId: string, now: number): void {
    if (this.cache.size >= this.maxEntries) {
      const firstKey = this.cache.keys().next().value;
      if (firstKey !== undefined) {
        this.cache.delete(firstKey);
      }
    }
    this.cache.set(eventId, now);
  }

  public clear(): void {
    this.cache.clear();
  }
}

export type RealtimeStateEvent =
  | 'CONNECT_REQUESTED'
  | 'DESCRIPTOR_RESOLVED'
  | 'SOCKET_OPENED'
  | 'SOCKET_CLOSED'
  | 'SOCKET_FAILED'
  | 'OFFLINE'
  | 'ONLINE'
  | 'RECONNECT_SCHEDULED'
  | 'MANUAL_DISCONNECT'
  | 'DISPOSED';

export function transitionState(
  current: RealtimeConnectionState,
  event: RealtimeStateEvent
): RealtimeConnectionState {
  if (event === 'DISPOSED') return 'closed';
  if (event === 'MANUAL_DISCONNECT') return 'closed';

  switch (current) {
    case 'idle':
    case 'closed':
    case 'failed':
      if (event === 'CONNECT_REQUESTED') return 'connecting';
      if (event === 'OFFLINE') return 'offline';
      return current;
    case 'connecting':
      if (event === 'SOCKET_OPENED') return 'connected';
      if (event === 'SOCKET_CLOSED' || event === 'SOCKET_FAILED') return 'reconnecting';
      if (event === 'OFFLINE') return 'offline';
      return current;
    case 'connected':
      if (event === 'SOCKET_CLOSED' || event === 'SOCKET_FAILED') return 'reconnecting';
      if (event === 'OFFLINE') return 'offline';
      return current;
    case 'reconnecting':
      if (event === 'SOCKET_OPENED') return 'connected';
      if (event === 'SOCKET_FAILED') return 'failed';
      if (event === 'OFFLINE') return 'offline';
      return current;
    case 'offline':
      if (event === 'ONLINE') return 'reconnecting';
      return current;
    default:
      return current;
  }
}

export class RealtimeClient implements RealtimeTransport {
  private state: RealtimeConnectionState = 'idle';
  private socket: WebSocketLike | null = null;
  private currentContext: RealtimeConnectContext | null = null;

  private reconnectAttempt = 0;
  private reconnectTimer: unknown = null;

  private heartbeatIntervalTimer: unknown = null;
  private pongTimeoutTimer: unknown = null;
  private missedPongs = 0;

  private readonly descriptorProvider: RealtimeConnectionDescriptorProvider;
  private readonly socketFactory: WebSocketFactory;
  private readonly clock: { now(): Date };
  private readonly scheduler: {
    setTimeout(fn: () => void, delayMs: number): unknown;
    clearTimeout(id: unknown): void;
    setInterval(fn: () => void, intervalMs: number): unknown;
    clearInterval(id: unknown): void;
  };
  private readonly random: () => number;
  private readonly telemetry?: RealtimeTelemetry;

  private readonly initialDelayMs: number;
  private readonly maximumDelayMs: number;
  private readonly maximumAttempts: number | 'unlimited';

  private readonly heartbeatIntervalMs: number;
  private readonly pongTimeoutMs: number;
  private readonly maximumMissedPongs: number;

  private readonly dedupCache: DedupLruCache;

  private readonly subscribers = new Set<{
    filter: RealtimeSubscriptionFilter;
    listener: RealtimeEventListener;
  }>();

  private readonly stateListeners = new Set<RealtimeStateListener>();
  private readonly recoveryListeners = new Set<RealtimeRecoveryListener>();

  private readonly sequenceTracker = new Map<string, number>();

  private isDisposed = false;
  private isManualClose = false;

  constructor(
    realtimeUrlOrOptions: string | RealtimeClientOptions,
    options?: RealtimeClientOptions
  ) {
    const opts: RealtimeClientOptions =
      typeof realtimeUrlOrOptions === 'string'
        ? { ...options, connectionDescriptorProvider: createCookieConnectionDescriptorProvider({ realtimeUrl: realtimeUrlOrOptions }) }
        : realtimeUrlOrOptions;

    this.descriptorProvider = opts.connectionDescriptorProvider ?? createCookieConnectionDescriptorProvider({ realtimeUrl: 'ws://localhost:5000/realtime' });
    this.socketFactory = opts.socketFactory ?? defaultBrowserWebSocketFactory;
    this.clock = opts.clock ?? { now: () => new Date() };
    this.scheduler = opts.scheduler ?? {
      setTimeout: (fn, ms) => setTimeout(fn, ms),
      clearTimeout: (id) => clearTimeout(id as any),
      setInterval: (fn, ms) => setInterval(fn, ms),
      clearInterval: (id) => clearInterval(id as any),
    };
    this.random = opts.random ?? Math.random;
    this.telemetry = opts.telemetry;

    this.initialDelayMs = opts.reconnect?.initialDelayMs ?? 1000;
    this.maximumDelayMs = opts.reconnect?.maximumDelayMs ?? 30_000;
    this.maximumAttempts = opts.reconnect?.maximumAttempts ?? 'unlimited';

    this.heartbeatIntervalMs = opts.heartbeat?.intervalMs ?? 15_000;
    this.pongTimeoutMs = opts.heartbeat?.pongTimeoutMs ?? 5_000;
    this.maximumMissedPongs = opts.heartbeat?.maximumMissedPongs ?? 2;

    const maxEntries = opts.deduplication?.maximumEntries ?? 1000;
    const ttlMs = opts.deduplication?.ttlMs ?? 60_000;
    this.dedupCache = new DedupLruCache(maxEntries, ttlMs);
  }

  public getState(): RealtimeConnectionState {
    return this.state;
  }

  private setState(newState: RealtimeConnectionState): void {
    if (this.state !== newState) {
      this.state = newState;
      this.stateListeners.forEach((listener) => {
        try {
          listener(newState);
        } catch (err) {
          this.telemetry?.reportError(err, { context: 'stateListener' });
        }
      });
    }
  }

  public async connect(context: RealtimeConnectContext): Promise<void> {
    if (this.isDisposed) return;
    if (this.state === 'connected' || this.state === 'connecting') return;

    this.isManualClose = false;
    this.currentContext = context;
    this.setState(transitionState(this.state, 'CONNECT_REQUESTED'));

    try {
      const descriptor = await this.descriptorProvider.getDescriptor(context);
      if (this.isDisposed || this.isManualClose) return;

      this.socket = this.socketFactory(descriptor);
      this.bindSocketEvents();
    } catch (err) {
      this.telemetry?.reportError(err, { context: 'descriptorResolution' });
      this.handleSocketFailure();
    }
  }

  private bindSocketEvents(): void {
    if (!this.socket) return;

    this.socket.onopen = () => {
      if (this.isDisposed || this.isManualClose) return;
      this.reconnectAttempt = 0;
      this.setState(transitionState(this.state, 'SOCKET_OPENED'));
      this.startHeartbeat();
    };

    this.socket.onmessage = (event) => {
      if (this.isDisposed) return;
      this.handleIncomingMessage(event.data);
    };

    this.socket.onclose = () => {
      this.stopHeartbeat();
      this.socket = null;
      if (this.isManualClose || this.isDisposed) {
        this.setState(transitionState(this.state, 'MANUAL_DISCONNECT'));
      } else {
        this.setState(transitionState(this.state, 'SOCKET_CLOSED'));
        this.scheduleReconnect();
      }
    };

    this.socket.onerror = (err) => {
      this.telemetry?.reportError(err, { context: 'socketError' });
    };
  }

  private handleIncomingMessage(data: unknown): void {
    const parseResult = parseRealtimeMessage(data);

    if (!parseResult.ok) {
      this.telemetry?.track('realtime.parse_error', { reason: parseResult.error.reason });
      return;
    }

    const { value } = parseResult;

    if (value.kind === 'control') {
      this.handleControlMessage(value.message);
      return;
    }

    if (value.kind === 'domain') {
      this.handleDomainEnvelope(value.envelope);
    }
  }

  private handleControlMessage(message: RealtimeControlMessage): void {
    if (message.type === 'pong') {
      this.missedPongs = 0;
      if (this.pongTimeoutTimer) {
        this.scheduler.clearTimeout(this.pongTimeoutTimer);
        this.pongTimeoutTimer = null;
      }
    }
  }

  private handleDomainEnvelope(envelope: RealtimeEnvelope<unknown>): void {
    const now = this.clock.now().getTime();

    // Deduplication check
    if (this.dedupCache.has(envelope.eventId, now)) {
      this.telemetry?.track('realtime.duplicate_ignored', { eventId: envelope.eventId });
      return;
    }
    this.dedupCache.add(envelope.eventId, now);

    // Sequence tracking and gap detection
    if (envelope.sequence !== undefined) {
      const channelKey = `${envelope.workspaceId}:${envelope.subscriptionId || 'default'}`;
      const previousSeq = this.sequenceTracker.get(channelKey);

      if (previousSeq !== undefined && envelope.sequence > previousSeq + 1) {
        const gap: RealtimeSequenceGap = {
          workspaceId: envelope.workspaceId,
          subscriptionId: envelope.subscriptionId,
          expected: previousSeq + 1,
          received: envelope.sequence,
        };
        this.recoveryListeners.forEach((listener) => {
          try {
            listener(gap);
          } catch (err) {
            this.telemetry?.reportError(err, { context: 'recoveryListener' });
          }
        });
      }

      this.sequenceTracker.set(channelKey, envelope.sequence);
    }

    // Workspace and subscription filtering
    this.subscribers.forEach(({ filter, listener }) => {
      if (filter.workspaceId !== envelope.workspaceId) return;
      if (filter.subscriptionId && filter.subscriptionId !== envelope.subscriptionId) return;
      if (filter.eventTypes && filter.eventTypes.length > 0 && !filter.eventTypes.includes(envelope.eventType)) return;

      try {
        listener(envelope);
      } catch (err) {
        this.telemetry?.reportError(err, { context: 'eventListener', eventId: envelope.eventId });
      }
    });
  }

  private startHeartbeat(): void {
    this.stopHeartbeat();
    this.missedPongs = 0;

    this.heartbeatIntervalTimer = this.scheduler.setInterval(() => {
      if (!this.socket || this.socket.readyState !== 1) return; // 1 = OPEN

      try {
        this.socket.send(JSON.stringify({ type: 'ping', sentAt: this.clock.now().toISOString() }));
      } catch (err) {
        this.telemetry?.reportError(err, { context: 'pingSend' });
      }

      // Start pong timeout timer
      this.pongTimeoutTimer = this.scheduler.setTimeout(() => {
        this.missedPongs++;
        if (this.missedPongs >= this.maximumMissedPongs) {
          this.telemetry?.track('realtime.heartbeat_timeout', { missedPongs: this.missedPongs });
          this.socket?.close(4000, 'Heartbeat timeout');
        }
      }, this.pongTimeoutMs);

    }, this.heartbeatIntervalMs);
  }

  private stopHeartbeat(): void {
    if (this.heartbeatIntervalTimer) {
      this.scheduler.clearInterval(this.heartbeatIntervalTimer);
      this.heartbeatIntervalTimer = null;
    }
    if (this.pongTimeoutTimer) {
      this.scheduler.clearTimeout(this.pongTimeoutTimer);
      this.pongTimeoutTimer = null;
    }
  }

  private handleSocketFailure(): void {
    this.setState(transitionState(this.state, 'SOCKET_FAILED'));
    this.scheduleReconnect();
  }

  private scheduleReconnect(): void {
    if (this.isManualClose || this.isDisposed) return;

    if (typeof this.maximumAttempts === 'number' && this.reconnectAttempt >= this.maximumAttempts) {
      this.setState('failed');
      return;
    }

    this.reconnectAttempt++;
    const baseDelay = Math.min(
      this.maximumDelayMs,
      this.initialDelayMs * Math.pow(2, this.reconnectAttempt - 1)
    );
    const jitteredDelay = baseDelay * (0.5 + this.random());

    this.setState(transitionState(this.state, 'RECONNECT_SCHEDULED'));

    if (this.reconnectTimer) {
      this.scheduler.clearTimeout(this.reconnectTimer);
    }

    this.reconnectTimer = this.scheduler.setTimeout(() => {
      if (this.currentContext && !this.isManualClose && !this.isDisposed) {
        void this.connect(this.currentContext);
      }
    }, Math.max(jitteredDelay, 0));
  }

  public disconnect(reason?: string): void {
    this.isManualClose = true;
    this.stopHeartbeat();

    if (this.reconnectTimer) {
      this.scheduler.clearTimeout(this.reconnectTimer);
      this.reconnectTimer = null;
    }

    if (this.socket) {
      this.socket.close(1000, reason || 'Manual disconnect');
      this.socket = null;
    }

    this.setState(transitionState(this.state, 'MANUAL_DISCONNECT'));
  }

  public subscribe(filter: RealtimeSubscriptionFilter, listener: RealtimeEventListener): () => void {
    const entry = { filter, listener };
    this.subscribers.add(entry);
    return () => {
      this.subscribers.delete(entry);
    };
  }

  public subscribeState(listener: RealtimeStateListener): () => void {
    this.stateListeners.add(listener);
    return () => {
      this.stateListeners.delete(listener);
    };
  }

  public subscribeRecovery(listener: RealtimeRecoveryListener): () => void {
    this.recoveryListeners.add(listener);
    return () => {
      this.recoveryListeners.delete(listener);
    };
  }

  public dispose(): void {
    if (this.isDisposed) return;
    this.isDisposed = true;

    this.disconnect('Disposed');
    this.subscribers.clear();
    this.stateListeners.clear();
    this.recoveryListeners.clear();
    this.sequenceTracker.clear();
    this.dedupCache.clear();
    this.setState(transitionState(this.state, 'DISPOSED'));
  }
}
