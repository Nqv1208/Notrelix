/**
 * Control frames of the realtime gateway protocol v1 (hand-written; see
 * `envelope.ts`). `ping`/`pong` flow in both directions.
 */

/**
 * Scope kinds of the gateway protocol v1.1. `channel` is a Chat space (stream
 * `chat:Channel:<id>`); `user` is the caller's own per-user stream (`user:notifications:<id>`).
 */
export type RealtimeSubscriptionScopeKind =
  "workspace" | "board" | "channel" | "user";

export interface RealtimeSubscriptionScope {
  readonly kind: RealtimeSubscriptionScopeKind;
  readonly id: string;
}

/** Codes of `subscription-error` frames. Unknown codes are kept as strings. */
export type RealtimeSubscriptionErrorCode =
  | "invalid_message"
  | "unsupported_scope"
  | "subscription_denied"
  | "subscription_limit"
  | "subscription_conflict"
  | "subscription_revoked"
  | "authorization_unavailable"
  | "unknown_subscription";

export type RealtimeControlMessage =
  | {
      readonly type: "ready";
      readonly schemaVersion: number;
      readonly connectionId: string;
      readonly pingIntervalMs?: number;
      readonly idleTimeoutMs?: number;
      readonly maxSubscriptions?: number;
    }
  | {
      readonly type: "subscribed";
      readonly subscriptionId: string;
      readonly scope: RealtimeSubscriptionScope;
      readonly streamKey: string;
    }
  | { readonly type: "unsubscribed"; readonly subscriptionId: string }
  /** The signed-in person's own access in a workspace changed; no content beyond that. */
  | { readonly type: "access-changed"; readonly workspaceId: string }
  | {
      readonly type: "subscription-error";
      readonly subscriptionId?: string;
      readonly code: RealtimeSubscriptionErrorCode | (string & {});
    }
  | { readonly type: "ping"; readonly sentAt?: string }
  | { readonly type: "pong"; readonly sentAt?: string };

/** Presence states a client reports and receives (protocol v1.1, ephemeral). */
export type RealtimePresenceState = "online" | "idle" | "offline";

/**
 * Ephemeral server frames of a Chat channel subscription: another person is
 * typing (the indicator lasts `expiresInMs`), or another person's presence
 * (after `ttlMs` without a newer frame the person counts as offline). Never
 * stored, never replayed, never used for authorization.
 */
export type RealtimeEphemeralFrame =
  | {
      readonly type: "typing";
      readonly subscriptionId: string;
      readonly userId: string;
      readonly expiresInMs: number;
    }
  | {
      readonly type: "presence";
      readonly subscriptionId: string;
      readonly userId: string;
      readonly state: RealtimePresenceState;
      readonly ttlMs: number;
    };

/** What a listener of the ephemeral signals of a channel receives. */
export type RealtimeEphemeralEvent =
  | {
      readonly kind: "typing";
      readonly channelId: string;
      readonly userId: string;
      readonly expiresInMs: number;
    }
  | {
      readonly kind: "presence";
      readonly channelId: string;
      readonly userId: string;
      readonly state: RealtimePresenceState;
      readonly ttlMs: number;
    };

/** Limits of the ephemeral frames, shared by the client, the hooks and the mock. */
export const REALTIME_TYPING_THROTTLE_MS = 3_000;
export const REALTIME_TYPING_EXPIRY_MS = 5_000;
/** The gateway drops a typing frame of one subscription that comes sooner than this. */
export const REALTIME_TYPING_MIN_INTERVAL_MS = 2_000;
export const REALTIME_PRESENCE_HEARTBEAT_MS = 30_000;
export const REALTIME_PRESENCE_TTL_MS = 75_000;
/** The gateway drops a presence frame with an unchanged state that comes sooner than this. */
export const REALTIME_PRESENCE_MIN_INTERVAL_MS = 5_000;

export type RealtimeClientMessage =
  | {
      readonly type: "subscribe";
      readonly subscriptionId: string;
      readonly scope: RealtimeSubscriptionScope;
    }
  | { readonly type: "unsubscribe"; readonly subscriptionId: string }
  | { readonly type: "ping"; readonly sentAt: string }
  | { readonly type: "pong" }
  /** Ephemeral: the caller is typing in the channel of this subscription. At most one per 3 s. */
  | { readonly type: "typing"; readonly subscriptionId: string }
  /** Ephemeral: the caller's presence heartbeat. */
  | { readonly type: "presence"; readonly state: RealtimePresenceState };
