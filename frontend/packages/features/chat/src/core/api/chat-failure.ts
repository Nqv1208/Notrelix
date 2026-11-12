/**
 * How a failed Chat request reads to the UI. The error is the AppError of the
 * API client (status, code, kind); the stable codes are the backend's
 * (`ChatRuleCodes`, the ProblemDetails `errorCode`).
 */

export type ChatFailureKind =
  /** 412: the item changed since it was read; reload it and try again. */
  | "stale"
  /** 409 channel-archived: the channel is read-only. */
  | "archived"
  /** 409 name-conflict: the name is taken by a live channel. */
  | "name-conflict"
  /** 422 last-manager: the last manager cannot leave. */
  | "last-manager"
  /** 409 message-deleted or system-message-immutable: the message cannot change. */
  | "message-locked"
  /** 404: a missing channel, a private one the caller is not in, or no access. */
  | "not-found"
  /** 409 broadcast-confirmation-required: @channel or @here in a large channel needs the confirmation. */
  | "broadcast-confirmation"
  /** 409 without a stable code (for example a reused Idempotency-Key with another payload). */
  | "conflict"
  /** 422 permission-lockout: a channel manager cannot be denied inviting people. */
  | "permission-lockout"
  /** 422 permission-exceeds-role: an allow above what the role (or the caller) holds. */
  | "permission-exceeds-role"
  /** 403: the caller may not do this. */
  | "forbidden"
  /** 400: the input was refused. */
  | "invalid"
  /** 429: slow down; a retry is allowed later. */
  | "rate-limited"
  /** No response: the connection dropped. A retry is allowed. */
  | "network"
  /** 5xx or anything unforeseen. A retry is allowed. */
  | "server";

export interface ChatFailure {
  readonly kind: ChatFailureKind;
  /** A retry with the same input can succeed (network, 429, 5xx). */
  readonly retryable: boolean;
  readonly status: number | undefined;
  readonly code: string | undefined;
}

interface FailureLike {
  readonly kind?: unknown;
  readonly status?: unknown;
  readonly code?: unknown;
}

const CODE_KINDS: Readonly<Record<string, ChatFailureKind>> = {
  "common.precondition-failed": "stale",
  "channel-archived": "archived",
  "name-conflict": "name-conflict",
  "last-manager": "last-manager",
  "message-deleted": "message-locked",
  "system-message-immutable": "message-locked",
  "resource.not_found": "not-found",
  "broadcast-confirmation-required": "broadcast-confirmation",
  "permission-lockout": "permission-lockout",
  "permission-exceeds-role": "permission-exceeds-role",
};

/** Classifies a thrown value from the Chat API (anything that is not an AppError is `server`). */
export function chatFailureOf(error: unknown): ChatFailure {
  const like: FailureLike =
    error && typeof error === "object" ? (error as FailureLike) : {};
  const status = typeof like.status === "number" ? like.status : undefined;
  const code = typeof like.code === "string" ? like.code : undefined;
  const kind = classify(status, code, like.kind);
  return {
    kind,
    retryable:
      kind === "network" || kind === "rate-limited" || kind === "server",
    status,
    code,
  };
}

function classify(
  status: number | undefined,
  code: string | undefined,
  appKind: unknown,
): ChatFailureKind {
  if (code !== undefined && CODE_KINDS[code]) return CODE_KINDS[code];
  if (status === undefined) {
    return appKind === "aborted" || appKind === "network"
      ? "network"
      : "server";
  }
  if (status === 412) return "stale";
  if (status === 404) return "not-found";
  if (status === 403) return "forbidden";
  if (status === 400 || status === 422) return "invalid";
  if (status === 429) return "rate-limited";
  if (status === 409) return "conflict";
  return "server";
}

/** The 412 of a stale expectedVersion. */
export function isStaleFailure(failure: ChatFailure): boolean {
  return failure.kind === "stale";
}
