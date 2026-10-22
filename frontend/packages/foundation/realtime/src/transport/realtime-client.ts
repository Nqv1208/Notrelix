import {
  isValidSubscriptionId,
  parseRealtimeMessage,
  type RealtimeClientMessage,
  type RealtimeControlMessage,
  type RealtimeEnvelope,
  type RealtimeSubscriptionFilter,
  type RealtimeSubscriptionScope,
} from "../protocol";
import { StreamCheckpointTracker } from "./stream-checkpoint";

export type RealtimeConnectionState =
  | "idle"
  | "connecting"
  | "connected"
  | "reconnecting"
  | "offline"
  | "closed"
  | "failed";

export interface RealtimeConnectContext {
  readonly sessionGeneration: string;
}

export interface RealtimeConnectionDescriptor {
  readonly url: string;
  readonly protocols?: readonly string[];
}

export interface RealtimeConnectionDescriptorProvider {
  getDescriptor(
    context: RealtimeConnectContext,
  ): Promise<RealtimeConnectionDescriptor>;
}

/** Gateway endpoint (`GET /api/v1/realtime`, upgraded to WebSocket). */
export const REALTIME_ENDPOINT_PATH = "/api/v1/realtime";

/**
 * A configured URL that is only an origin (no path, or a bare slash) gets the
 * gateway endpoint path appended. A URL that already has a path is
 * an explicit endpoint chosen by the runtime environment and is kept as is.
 */
export function resolveRealtimeEndpointUrl(configuredUrl: string): string {
  try {
    const url = new URL(configuredUrl);
    if (url.pathname === "" || url.pathname === "/") {
      url.pathname = REALTIME_ENDPOINT_PATH;
      return url.toString();
    }
  } catch {
    // Not an absolute URL; leave the value to the socket factory to reject.
  }
  return configuredUrl;
}

export function createCookieConnectionDescriptorProvider(options: {
  realtimeUrl: string;
}): RealtimeConnectionDescriptorProvider {
  return {
    getDescriptor: async () => ({
      url: resolveRealtimeEndpointUrl(options.realtimeUrl),
    }),
  };
}

export interface WebSocketLike {
  readyState: number;
  send(data: string): void;
  close(code?: number, reason?: string): void;
  onopen: ((event: unknown) => void) | null;
  onmessage: ((event: { data: unknown }) => void) | null;
  onclose: ((event: { code: number; reason: string }) => void) | null;
  onerror: ((event: unknown) => void) | null;
}

export type WebSocketFactory = (
  descriptor: RealtimeConnectionDescriptor,
) => WebSocketLike;

export type RealtimeRecoveryReason =
  /** Subscription acknowledged: fetch authoritative REST state now. */
  | "subscribed"
  /** `streamVersion` skipped ahead of `last + 1`. */
  | "gap"
  /** A different event re-used the checkpoint version. */
  | "duplicate"
  /** `streamVersion` went backwards (stream reset). */
  | "reset"
  /** Server revoked/denied the subscription: permission was lost. */
  | "permission-lost"
  /** Server rejected the subscription for a non-permission reason. */
  | "subscription-failed";

/**
 * Tells the host that incremental history for a scope is unreliable and that
 * authoritative state must be re-established over REST (FE-RT-024/025).
 */
export interface RealtimeRecoverySignal {
  readonly reason: RealtimeRecoveryReason;
  readonly workspaceId: string;
  readonly scope: RealtimeSubscriptionScope;
  readonly subscriptionId: string;
  readonly streamKey?: string;
  readonly expected?: number;
  readonly received?: number;
  /** Gateway `subscription-error` code for permission/failure reasons. */
  readonly code?: string;
}

export type RealtimeEventListener = (envelope: RealtimeEnvelope) => void;
export type RealtimeStateListener = (state: RealtimeConnectionState) => void;
export type RealtimeRecoveryListener = (signal: RealtimeRecoverySignal) => void;

export interface RealtimeTelemetry {
  track(event: string, properties?: Record<string, unknown>): void;
  reportError(error: unknown, context?: Record<string, unknown>): void;
}

export interface RealtimeClientOptions {
  readonly connectionDescriptorProvider?: RealtimeConnectionDescriptorProvider;
  readonly socketFactory: WebSocketFactory;
  readonly clock?: { now(): Date };
  readonly scheduler?: {
    setTimeout(fn: () => void, delayMs: number): unknown;
    clearTimeout(id: unknown): void;
    setInterval(fn: () => void, intervalMs: number): unknown;
    clearInterval(id: unknown): void;
  };
  readonly telemetry?: RealtimeTelemetry;
  /** Random source in [0, 1) for backoff jitter. Defaults to Math.random. */
  readonly random?: () => number;
  /**
   * Refreshes the session after close code 4401 (credentials expired/revoked).
   * Resolves when fresh credentials are in place; rejects when the session
   * cannot be refreshed. Supplied by the runtime composition root, which owns
   * the auth mechanism (FE-RT-006).
   */
  readonly refreshSession?: () => Promise<void>;

  readonly reconnect?: {
    readonly initialDelayMs?: number;
    readonly maximumDelayMs?: number;
    readonly maximumAttempts?: number | "unlimited";
  };

  readonly heartbeat?: {
    /** Client ping period. The gateway closes after 75s without inbound frames. */
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
  subscribe(
    filter: RealtimeSubscriptionFilter,
    listener: RealtimeEventListener,
  ): () => void;
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
    private readonly ttlMs: number,
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
  | "CONNECT_REQUESTED"
  | "DESCRIPTOR_RESOLVED"
  | "SOCKET_OPENED"
  | "SOCKET_CLOSED"
  | "SOCKET_FAILED"
  | "OFFLINE"
  | "ONLINE"
  | "RECONNECT_SCHEDULED"
  | "MANUAL_DISCONNECT"
  | "DISPOSED";

export function transitionState(
  current: RealtimeConnectionState,
  event: RealtimeStateEvent,
): RealtimeConnectionState {
  if (event === "DISPOSED") return "closed";
  if (event === "MANUAL_DISCONNECT") return "closed";

  switch (current) {
    case "idle":
    case "closed":
    case "failed":
      if (event === "CONNECT_REQUESTED") return "connecting";
      if (event === "OFFLINE") return "offline";
      return current;
    case "connecting":
      if (event === "SOCKET_OPENED") return "connected";
      if (event === "SOCKET_CLOSED" || event === "SOCKET_FAILED")
        return "reconnecting";
      if (event === "OFFLINE") return "offline";
      return current;
    case "connected":
      if (event === "SOCKET_CLOSED" || event === "SOCKET_FAILED")
        return "reconnecting";
      if (event === "OFFLINE") return "offline";
      return current;
    case "reconnecting":
      if (event === "SOCKET_OPENED") return "connected";
      if (event === "SOCKET_FAILED") return "failed";
      if (event === "OFFLINE") return "offline";
      return current;
    case "offline":
      if (event === "ONLINE") return "reconnecting";
      return current;
    default:
      return current;
  }
}

/** Close codes after which retrying cannot help (client protocol faults). */
const NON_RETRYABLE_CLOSE_CODES = new Set([
  1003, // binary frame
  1008, // inbound flood
  1009, // message too large
]);
/** Credentials expired or revoked: refresh the session, then reconnect. */
const CLOSE_CREDENTIALS_EXPIRED = 4401;
/** Consecutive 4401 closes (without reaching `ready`) before giving up. */
const MAX_CREDENTIAL_REFRESH_ATTEMPTS = 2;
/** `authorization_unavailable` is transient: re-subscribe a bounded number of times. */
const MAX_SUBSCRIPTION_RETRIES = 5;
const DEFAULT_HEARTBEAT_INTERVAL_MS = 25_000;
const SOCKET_OPEN = 1;

type WireSubscriptionStatus =
  "pending" | "active" | "permission-lost" | "failed";

interface WireSubscription {
  readonly id: string;
  readonly scope: RealtimeSubscriptionScope;
  readonly workspaceId: string;
  refCount: number;
  status: WireSubscriptionStatus;
  streamKey?: string;
  retryAttempt: number;
  retryTimer: unknown;
}

interface ListenerEntry {
  readonly filter: RealtimeSubscriptionFilter;
  readonly listener: RealtimeEventListener;
  readonly wireId: string;
}

export function deriveSubscriptionScope(
  filter: RealtimeSubscriptionFilter,
): RealtimeSubscriptionScope {
  return filter.boardId
    ? { kind: "board", id: filter.boardId }
    : { kind: "workspace", id: filter.workspaceId };
}

export class RealtimeClient implements RealtimeTransport {
  private state: RealtimeConnectionState = "idle";
  private socket: WebSocketLike | null = null;
  private currentContext: RealtimeConnectContext | null = null;
  /** True once the gateway `ready` frame arrived on the current socket. */
  private ready = false;
  /** Invalidates async work (refresh, descriptor) of an earlier connection. */
  private connectionEpoch = 0;

  private reconnectAttempt = 0;
  private reconnectTimer: unknown = null;
  private credentialRefreshAttempts = 0;

  private heartbeatIntervalTimer: unknown = null;
  private pongTimeoutTimer: unknown = null;
  private missedPongs = 0;
  private heartbeatIntervalMs: number;

  private readonly descriptorProvider: RealtimeConnectionDescriptorProvider;
  private readonly socketFactory: WebSocketFactory;
  private readonly clock: { now(): Date };
  private readonly scheduler: {
    setTimeout(fn: () => void, delayMs: number): unknown;
    clearTimeout(id: unknown): void;
    setInterval(fn: () => void, intervalMs: number): unknown;
    clearInterval(id: unknown): void;
  };
  private readonly telemetry?: RealtimeTelemetry;
  private readonly random: () => number;
  private readonly refreshSession?: () => Promise<void>;

  private readonly initialDelayMs: number;
  private readonly maximumDelayMs: number;
  private readonly maximumAttempts: number | "unlimited";

  private readonly configuredHeartbeatIntervalMs: number;
  private readonly pongTimeoutMs: number;
  private readonly maximumMissedPongs: number;

  private readonly dedupCache: DedupLruCache;

  private readonly listeners = new Set<ListenerEntry>();
  private readonly wireSubscriptions = new Map<string, WireSubscription>();
  private readonly checkpoints = new StreamCheckpointTracker();

  private readonly stateListeners = new Set<RealtimeStateListener>();
  private readonly recoveryListeners = new Set<RealtimeRecoveryListener>();

  private isDisposed = false;
  private isManualClose = false;

  constructor(realtimeUrl: string, options: RealtimeClientOptions) {
    if (typeof realtimeUrl !== "string" || realtimeUrl.length === 0) {
      throw new Error(
        "RealtimeClient requires an explicit realtime URL from the runtime composition root.",
      );
    }
    if (!options?.socketFactory) {
      throw new Error(
        "RealtimeClient requires a socketFactory from the runtime composition root.",
      );
    }

    this.descriptorProvider =
      options.connectionDescriptorProvider ??
      createCookieConnectionDescriptorProvider({ realtimeUrl });
    this.socketFactory = options.socketFactory;
    this.clock = options.clock ?? { now: () => new Date() };
    this.scheduler = options.scheduler ?? {
      setTimeout: (fn, ms) => setTimeout(fn, ms),
      clearTimeout: (id) => clearTimeout(id as ReturnType<typeof setTimeout>),
      setInterval: (fn, ms) => setInterval(fn, ms),
      clearInterval: (id) =>
        clearInterval(id as ReturnType<typeof setInterval>),
    };
    this.telemetry = options.telemetry;
    this.random = options.random ?? Math.random;
    this.refreshSession = options.refreshSession;

    this.initialDelayMs = options.reconnect?.initialDelayMs ?? 1000;
    this.maximumDelayMs = options.reconnect?.maximumDelayMs ?? 30_000;
    this.maximumAttempts = options.reconnect?.maximumAttempts ?? "unlimited";

    this.configuredHeartbeatIntervalMs =
      options.heartbeat?.intervalMs ?? DEFAULT_HEARTBEAT_INTERVAL_MS;
    this.heartbeatIntervalMs = this.configuredHeartbeatIntervalMs;
    this.pongTimeoutMs = options.heartbeat?.pongTimeoutMs ?? 5_000;
    this.maximumMissedPongs = options.heartbeat?.maximumMissedPongs ?? 2;

    const maxEntries = options.deduplication?.maximumEntries ?? 1000;
    const ttlMs = options.deduplication?.ttlMs ?? 60_000;
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
          this.telemetry?.reportError(err, { context: "stateListener" });
        }
      });
    }
  }

  public async connect(context: RealtimeConnectContext): Promise<void> {
    if (this.isDisposed) return;

    const isActive = this.state === "connected" || this.state === "connecting";
    const sessionChanged =
      this.currentContext !== null &&
      this.currentContext.sessionGeneration !== context.sessionGeneration;

    if (isActive && !sessionChanged) return;
    if (isActive && sessionChanged) {
      // A new principal must not reuse a socket authenticated for the old one.
      this.disconnect("session-changed");
    }
    if (sessionChanged) {
      // Permission loss / failures belong to the previous principal.
      this.wireSubscriptions.forEach((wire) => {
        if (wire.status === "permission-lost" || wire.status === "failed") {
          wire.status = "pending";
          wire.retryAttempt = 0;
        }
      });
    }

    this.isManualClose = false;
    this.currentContext = context;
    const epoch = ++this.connectionEpoch;
    this.setState(transitionState(this.state, "CONNECT_REQUESTED"));

    try {
      const descriptor = await this.descriptorProvider.getDescriptor(context);
      if (
        this.isDisposed ||
        this.isManualClose ||
        epoch !== this.connectionEpoch
      )
        return;

      const socket = this.socketFactory(descriptor);
      this.socket = socket;
      this.bindSocketEvents(socket);
    } catch (err) {
      if (epoch !== this.connectionEpoch) return;
      this.telemetry?.reportError(err, { context: "descriptorResolution" });
      this.handleSocketFailure();
    }
  }

  private bindSocketEvents(socket: WebSocketLike): void {
    // Events of a socket that is no longer the current one are ignored, so a
    // late callback of a replaced/closed socket cannot touch the new state.
    const isCurrent = () => this.socket === socket;

    socket.onopen = () => {
      if (!isCurrent() || this.isDisposed || this.isManualClose) return;
      this.setState(transitionState(this.state, "SOCKET_OPENED"));
      this.startHeartbeat(this.configuredHeartbeatIntervalMs);
    };

    socket.onmessage = (event) => {
      if (!isCurrent() || this.isDisposed) return;
      this.handleIncomingMessage(event.data);
    };

    socket.onclose = (event) => {
      if (!isCurrent()) return;
      this.handleSocketClosed(event?.code ?? 1006);
    };

    socket.onerror = (err) => {
      if (!isCurrent()) return;
      this.telemetry?.reportError(err, { context: "socketError" });
    };
  }

  private handleSocketClosed(code: number): void {
    this.stopHeartbeat();
    this.socket = null;
    this.ready = false;
    // Checkpoints are only valid for one subscription epoch: the first event
    // after the next (re)subscribe sets a new one.
    this.checkpoints.clear();
    this.wireSubscriptions.forEach((wire) => {
      this.clearSubscriptionRetry(wire);
      if (wire.status === "active") wire.status = "pending";
    });

    if (this.isManualClose || this.isDisposed) {
      this.setState(transitionState(this.state, "MANUAL_DISCONNECT"));
      return;
    }

    this.telemetry?.track("realtime.socket_closed", { code });

    if (code === CLOSE_CREDENTIALS_EXPIRED) {
      this.handleCredentialsExpired();
      return;
    }

    if (NON_RETRYABLE_CLOSE_CODES.has(code)) {
      this.telemetry?.reportError(
        new Error(`Realtime socket closed with non-retryable code ${code}`),
        { context: "nonRetryableClose", code },
      );
      this.setState("failed");
      return;
    }

    // 1000/1001/1006/1013/4408 and any other code: reconnect with backoff.
    this.setState(transitionState(this.state, "SOCKET_CLOSED"));
    this.scheduleReconnect();
  }

  private handleCredentialsExpired(): void {
    this.setState(transitionState(this.state, "SOCKET_CLOSED"));

    const context = this.currentContext;
    if (
      !this.refreshSession ||
      !context ||
      this.credentialRefreshAttempts >= MAX_CREDENTIAL_REFRESH_ATTEMPTS
    ) {
      this.telemetry?.track("realtime.credentials_unrecoverable", {
        hasRefresh: Boolean(this.refreshSession),
      });
      this.setState("failed");
      return;
    }

    this.credentialRefreshAttempts++;
    const epoch = this.connectionEpoch;
    this.refreshSession().then(
      () => {
        if (this.isDisposed || this.isManualClose) return;
        if (epoch !== this.connectionEpoch) return;
        // State is still `reconnecting`; reconnect now with the fresh session.
        void this.connect(context);
      },
      (err) => {
        if (this.isDisposed || this.isManualClose) return;
        if (epoch !== this.connectionEpoch) return;
        this.telemetry?.reportError(err, { context: "sessionRefresh" });
        this.setState("failed");
      },
    );
  }

  private handleIncomingMessage(data: unknown): void {
    const parseResult = parseRealtimeMessage(data);

    if (!parseResult.ok) {
      this.telemetry?.reportError(
        new Error(`Unparseable realtime message: ${parseResult.error.reason}`),
        { context: "messageParse", reason: parseResult.error.reason },
      );
      return;
    }

    const { value } = parseResult;

    if (value.kind === "control") {
      this.handleControlMessage(value.message);
      return;
    }

    this.handleDomainEnvelope(value.envelope);
  }

  private handleControlMessage(message: RealtimeControlMessage): void {
    switch (message.type) {
      case "ready":
        this.handleReady(message);
        return;
      case "subscribed":
        this.handleSubscribed(message);
        return;
      case "unsubscribed":
        return;
      case "subscription-error":
        this.handleSubscriptionError(message);
        return;
      case "ping":
        this.sendFrame({ type: "pong" });
        return;
      case "pong":
        this.missedPongs = 0;
        if (this.pongTimeoutTimer) {
          this.scheduler.clearTimeout(this.pongTimeoutTimer);
          this.pongTimeoutTimer = null;
        }
        return;
    }
  }

  private handleReady(
    message: Extract<RealtimeControlMessage, { type: "ready" }>,
  ): void {
    this.ready = true;
    this.reconnectAttempt = 0;
    this.credentialRefreshAttempts = 0;

    // Keep the client ping period well inside the server idle timeout.
    if (message.idleTimeoutMs && message.idleTimeoutMs > 0) {
      const bounded = Math.min(
        this.configuredHeartbeatIntervalMs,
        Math.floor(message.idleTimeoutMs / 3),
      );
      if (bounded > 0 && bounded !== this.heartbeatIntervalMs) {
        this.startHeartbeat(bounded);
      }
    }

    this.wireSubscriptions.forEach((wire) => {
      if (wire.status === "permission-lost" || wire.status === "failed") return;
      wire.status = "pending";
      this.sendSubscribe(wire);
    });
  }

  private handleSubscribed(
    message: Extract<RealtimeControlMessage, { type: "subscribed" }>,
  ): void {
    const wire = this.wireSubscriptions.get(message.subscriptionId);
    if (
      !wire ||
      wire.scope.kind !== message.scope.kind ||
      wire.scope.id !== message.scope.id
    ) {
      this.telemetry?.track("realtime.subscribed_unknown", {
        subscriptionId: message.subscriptionId,
      });
      return;
    }

    wire.status = "active";
    wire.streamKey = message.streamKey;
    wire.retryAttempt = 0;
    this.clearSubscriptionRetry(wire);
    // The first event after (re)subscribe sets the checkpoint.
    this.checkpoints.resetByPrefix(checkpointPrefix(wire.id));

    // Events arrive strictly after this ack; state fetched now is the base.
    this.emitRecovery(wire, "subscribed");
  }

  private handleSubscriptionError(
    message: Extract<RealtimeControlMessage, { type: "subscription-error" }>,
  ): void {
    const wire = message.subscriptionId
      ? this.wireSubscriptions.get(message.subscriptionId)
      : undefined;

    this.telemetry?.track("realtime.subscription_error", {
      code: message.code,
      subscriptionId: message.subscriptionId,
    });

    if (!wire) return;

    switch (message.code) {
      case "subscription_revoked":
      case "subscription_denied":
        // Permission loss: no silent retry loop. The host revalidates over
        // REST, which surfaces the authoritative forbidden state.
        wire.status = "permission-lost";
        this.clearSubscriptionRetry(wire);
        this.checkpoints.resetByPrefix(checkpointPrefix(wire.id));
        this.emitRecovery(wire, "permission-lost", message.code);
        return;
      case "authorization_unavailable":
        this.scheduleSubscriptionRetry(wire, message.code);
        return;
      case "unknown_subscription":
        return;
      default:
        wire.status = "failed";
        this.clearSubscriptionRetry(wire);
        this.emitRecovery(wire, "subscription-failed", message.code);
    }
  }

  private handleDomainEnvelope(envelope: RealtimeEnvelope): void {
    const wire = this.wireSubscriptions.get(envelope.subscriptionId);
    if (!wire || wire.status !== "active") {
      // Late event of a removed scope, or one not preceded by its ack.
      this.telemetry?.track("realtime.event_for_inactive_subscription", {
        eventId: envelope.eventId,
        subscriptionId: envelope.subscriptionId,
      });
      return;
    }

    const now = this.clock.now().getTime();

    // Duplicate frames are ignored by eventId.
    if (this.dedupCache.has(envelope.eventId, now)) {
      this.telemetry?.track("realtime.duplicate_ignored", {
        eventId: envelope.eventId,
      });
      return;
    }
    this.dedupCache.add(envelope.eventId, now);

    const observation = this.checkpoints.observe(
      `${checkpointPrefix(wire.id)}${envelope.streamKey}`,
      envelope.streamVersion,
    );

    if (observation.kind === "recover") {
      // Local incremental history is unreliable: refetch authoritative state.
      // The checkpoint was rebased to the received version, so the next +1
      // event continues normally. The frame itself is covered by the refetch.
      this.telemetry?.track("realtime.stream_recovery", {
        reason: observation.reason,
        streamKey: envelope.streamKey,
        expected: observation.expected,
        received: observation.received,
      });
      this.emitRecovery(wire, observation.reason, undefined, {
        streamKey: envelope.streamKey,
        expected: observation.expected,
        received: observation.received,
      });
      return;
    }

    this.listeners.forEach(({ filter, listener, wireId }) => {
      if (wireId !== wire.id) return;
      if (filter.workspaceId !== envelope.workspaceId) return;
      if (
        filter.eventTypes &&
        filter.eventTypes.length > 0 &&
        !filter.eventTypes.includes(envelope.eventType)
      )
        return;

      try {
        listener(envelope);
      } catch (err) {
        this.telemetry?.reportError(err, {
          context: "eventListener",
          eventId: envelope.eventId,
        });
      }
    });
  }

  private emitRecovery(
    wire: WireSubscription,
    reason: RealtimeRecoveryReason,
    code?: string,
    detail?: {
      streamKey?: string;
      expected?: number;
      received?: number;
    },
  ): void {
    const signal: RealtimeRecoverySignal = {
      reason,
      workspaceId: wire.workspaceId,
      scope: wire.scope,
      subscriptionId: wire.id,
      streamKey: detail?.streamKey ?? wire.streamKey,
      expected: detail?.expected,
      received: detail?.received,
      code,
    };
    this.recoveryListeners.forEach((listener) => {
      try {
        listener(signal);
      } catch (err) {
        this.telemetry?.reportError(err, { context: "recoveryListener" });
      }
    });
  }

  private sendFrame(message: RealtimeClientMessage): boolean {
    const socket = this.socket;
    if (!socket || socket.readyState !== SOCKET_OPEN) return false;
    try {
      socket.send(JSON.stringify(message));
      return true;
    } catch (err) {
      this.telemetry?.reportError(err, {
        context: "send",
        type: message.type,
      });
      return false;
    }
  }

  private sendSubscribe(wire: WireSubscription): void {
    this.sendFrame({
      type: "subscribe",
      subscriptionId: wire.id,
      scope: wire.scope,
    });
  }

  private scheduleSubscriptionRetry(
    wire: WireSubscription,
    code: string,
  ): void {
    this.clearSubscriptionRetry(wire);
    wire.status = "pending";

    if (wire.retryAttempt >= MAX_SUBSCRIPTION_RETRIES) {
      wire.status = "failed";
      this.emitRecovery(wire, "subscription-failed", code);
      return;
    }

    wire.retryAttempt++;
    const delay = this.backoffDelay(wire.retryAttempt);
    wire.retryTimer = this.scheduler.setTimeout(() => {
      wire.retryTimer = null;
      if (
        this.wireSubscriptions.get(wire.id) !== wire ||
        wire.status !== "pending" ||
        !this.ready
      ) {
        return;
      }
      this.sendSubscribe(wire);
    }, delay);
  }

  private clearSubscriptionRetry(wire: WireSubscription): void {
    if (wire.retryTimer) {
      this.scheduler.clearTimeout(wire.retryTimer);
      wire.retryTimer = null;
    }
  }

  private startHeartbeat(intervalMs: number): void {
    this.stopHeartbeat();
    this.missedPongs = 0;
    this.heartbeatIntervalMs = intervalMs;

    this.heartbeatIntervalTimer = this.scheduler.setInterval(() => {
      const socket = this.socket;
      if (!socket || socket.readyState !== SOCKET_OPEN) return;

      this.sendFrame({ type: "ping", sentAt: this.clock.now().toISOString() });

      if (this.pongTimeoutTimer) return;
      this.pongTimeoutTimer = this.scheduler.setTimeout(() => {
        this.pongTimeoutTimer = null;
        this.missedPongs++;
        if (this.missedPongs >= this.maximumMissedPongs) {
          this.telemetry?.track("realtime.heartbeat_timeout", {
            missedPongs: this.missedPongs,
          });
          this.socket?.close(4000, "Heartbeat timeout");
        }
      }, this.pongTimeoutMs);
    }, intervalMs);
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
    this.setState(transitionState(this.state, "SOCKET_FAILED"));
    this.scheduleReconnect();
  }

  /** Exponential backoff with equal jitter: delay in [base/2, base]. */
  private backoffDelay(attempt: number): number {
    const base = Math.min(
      this.maximumDelayMs,
      this.initialDelayMs * Math.pow(2, attempt - 1),
    );
    return Math.max(0, Math.floor(base / 2 + (this.random() * base) / 2));
  }

  private scheduleReconnect(): void {
    if (this.isManualClose || this.isDisposed) return;

    if (
      typeof this.maximumAttempts === "number" &&
      this.reconnectAttempt >= this.maximumAttempts
    ) {
      this.setState("failed");
      return;
    }

    this.reconnectAttempt++;
    const delay = this.backoffDelay(this.reconnectAttempt);

    this.setState(transitionState(this.state, "RECONNECT_SCHEDULED"));

    if (this.reconnectTimer) {
      this.scheduler.clearTimeout(this.reconnectTimer);
    }

    this.reconnectTimer = this.scheduler.setTimeout(() => {
      this.reconnectTimer = null;
      if (this.currentContext && !this.isManualClose && !this.isDisposed) {
        void this.connect(this.currentContext);
      }
    }, delay);
  }

  public disconnect(reason?: string): void {
    this.isManualClose = true;
    this.connectionEpoch++;
    this.stopHeartbeat();
    this.ready = false;
    this.checkpoints.clear();
    this.wireSubscriptions.forEach((wire) => {
      this.clearSubscriptionRetry(wire);
      if (wire.status === "active") wire.status = "pending";
    });

    if (this.reconnectTimer) {
      this.scheduler.clearTimeout(this.reconnectTimer);
      this.reconnectTimer = null;
    }

    if (this.socket) {
      const socket = this.socket;
      this.socket = null;
      socket.close(1000, reason || "Manual disconnect");
    }

    this.setState(transitionState(this.state, "MANUAL_DISCONNECT"));
  }

  public subscribe(
    filter: RealtimeSubscriptionFilter,
    listener: RealtimeEventListener,
  ): () => void {
    const scope = deriveSubscriptionScope(filter);
    const wireId = `${scope.kind}:${scope.id}`;

    if (!isValidSubscriptionId(wireId) || !filter.workspaceId) {
      this.telemetry?.reportError(
        new Error("Invalid realtime subscription scope"),
        { context: "subscribe", scopeKind: scope.kind },
      );
      return () => {};
    }

    const entry: ListenerEntry = { filter, listener, wireId };
    this.listeners.add(entry);

    let wire = this.wireSubscriptions.get(wireId);
    if (!wire) {
      wire = {
        id: wireId,
        scope,
        workspaceId: filter.workspaceId,
        refCount: 0,
        status: "pending",
        retryAttempt: 0,
        retryTimer: null,
      };
      this.wireSubscriptions.set(wireId, wire);
      wire.refCount++;
      if (this.ready) this.sendSubscribe(wire);
    } else {
      wire.refCount++;
    }

    let released = false;
    return () => {
      if (released) return;
      released = true;
      this.listeners.delete(entry);
      this.releaseWireSubscription(wireId);
    };
  }

  private releaseWireSubscription(wireId: string): void {
    const wire = this.wireSubscriptions.get(wireId);
    if (!wire) return;
    wire.refCount--;
    if (wire.refCount > 0) return;

    this.clearSubscriptionRetry(wire);
    this.wireSubscriptions.delete(wireId);
    this.checkpoints.resetByPrefix(checkpointPrefix(wireId));
    if (this.ready && wire.status !== "failed") {
      this.sendFrame({ type: "unsubscribe", subscriptionId: wireId });
    }
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

    this.disconnect("Disposed");
    this.listeners.clear();
    this.wireSubscriptions.clear();
    this.stateListeners.clear();
    this.recoveryListeners.clear();
    this.checkpoints.clear();
    this.dedupCache.clear();
    this.setState(transitionState(this.state, "DISPOSED"));
  }
}

function checkpointPrefix(subscriptionId: string): string {
  return `${subscriptionId}|`;
}
