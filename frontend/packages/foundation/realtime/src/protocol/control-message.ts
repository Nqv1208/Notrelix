/**
 * Control frames of the realtime gateway protocol v1 (hand-written; see
 * `envelope.ts`). `ping`/`pong` flow in both directions.
 */

export type RealtimeSubscriptionScopeKind = "workspace" | "board";

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

export type RealtimeClientMessage =
  | {
      readonly type: "subscribe";
      readonly subscriptionId: string;
      readonly scope: RealtimeSubscriptionScope;
    }
  | { readonly type: "unsubscribe"; readonly subscriptionId: string }
  | { readonly type: "ping"; readonly sentAt: string }
  | { readonly type: "pong" };
