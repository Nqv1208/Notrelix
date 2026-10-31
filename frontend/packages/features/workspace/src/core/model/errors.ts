/**
 * Workspace request failures, normalized for presentation (FE-AGENTS §27).
 *
 * Branches only on stable fields: the client error `kind`, the HTTP status
 * and the producer's `errorCode` extension (ProblemDetails). The producer's
 * `detail` is kept as text to show, never to branch on.
 */

export type WorkspaceFailureKind =
  | "validation"
  | "rule"
  | "conflict"
  | "forbidden"
  | "not-found"
  | "network"
  | "unknown";

/** The producer's stable code for a denial because the member's email is not confirmed. */
export const EMAIL_NOT_CONFIRMED_CODE = "auth.email_not_confirmed";

/**
 * A reason that refines a failure without changing its kind: the request is
 * `forbidden`, and the recovery is not "ask an owner" but "confirm your email".
 */
export type WorkspaceFailureReason = "email-not-confirmed";

export interface WorkspaceFailure {
  readonly kind: WorkspaceFailureKind;
  /** Why the producer refused, when it gave a stable code for it. */
  readonly reason: WorkspaceFailureReason | null;
  /** The producer's `errorCode` extension, when present. */
  readonly errorCode: string | null;
  /** The rule a business-rule failure names (`ruleCode` extension), when present. */
  readonly ruleCode?: string | null;
  /** The producer's human-readable `detail`, when present. */
  readonly detail: string | null;
  /** Field errors from a validation failure (`errors` extension). */
  readonly fieldErrors: Readonly<Record<string, readonly string[]>>;
}

interface ErrorLike {
  readonly kind?: unknown;
  readonly status?: unknown;
  readonly details?: unknown;
  readonly validationErrors?: unknown;
}

function asRecord(value: unknown): Record<string, unknown> | null {
  return value && typeof value === "object"
    ? (value as Record<string, unknown>)
    : null;
}

function readFieldErrors(value: unknown): Record<string, readonly string[]> {
  const record = asRecord(value);
  if (!record) return {};
  const out: Record<string, readonly string[]> = {};
  for (const [field, messages] of Object.entries(record)) {
    if (Array.isArray(messages)) {
      out[field] = messages.filter(
        (message): message is string => typeof message === "string",
      );
    }
  }
  return out;
}

/** Maps any thrown value (usually an `AppError`) to a workspace failure. */
export function describeWorkspaceFailure(error: unknown): WorkspaceFailure {
  const candidate = (asRecord(error) ?? {}) as ErrorLike;
  const body = asRecord(candidate.details);
  const errorCode = typeof body?.errorCode === "string" ? body.errorCode : null;
  const detail = typeof body?.detail === "string" ? body.detail : null;
  const fieldErrors = readFieldErrors(
    candidate.validationErrors ?? body?.errors,
  );
  const status = typeof candidate.status === "number" ? candidate.status : 0;
  const clientKind = typeof candidate.kind === "string" ? candidate.kind : "";

  let kind: WorkspaceFailureKind;
  if (errorCode === "business_rule.violation") kind = "rule";
  else if (errorCode === "validation.failed" || clientKind === "validation")
    kind = "validation";
  else if (
    clientKind === "conflict" ||
    status === 409 ||
    status === 412 ||
    errorCode === "concurrency.conflict"
  )
    kind = "conflict";
  else if (clientKind === "forbidden" || status === 403) kind = "forbidden";
  else if (clientKind === "not_found" || status === 404) kind = "not-found";
  else if (clientKind === "network" || clientKind === "aborted")
    kind = "network";
  else kind = "unknown";

  const reason: WorkspaceFailureReason | null =
    kind === "forbidden" && errorCode === EMAIL_NOT_CONFIRMED_CODE
      ? "email-not-confirmed"
      : null;

  const ruleCode = typeof body?.ruleCode === "string" ? body.ruleCode : null;
  return { kind, reason, errorCode, ruleCode, detail, fieldErrors };
}
