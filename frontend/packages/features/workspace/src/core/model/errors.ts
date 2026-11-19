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

/** The producer's stable code for a Guest invitation refused because the workspace setting `AllowGuestInvites` is off. */
export const GUEST_INVITES_OFF_CODE = "workspace.guest_invites_off";

/** The producer's `ruleCode` for the 51st pin of a member in a workspace (the cap is 50). */
export const PIN_LIMIT_REACHED_RULE = "Workspaces_Navigation_PinLimitReached";

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
  /** The stable rule that refused the request (`ruleCode` extension), when the producer sent one. */
  readonly ruleCode: string | null;
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
  const ruleCode = typeof body?.ruleCode === "string" ? body.ruleCode : null;
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

  return { kind, reason, errorCode, ruleCode, detail, fieldErrors };
}

/** The producer's stable code for deleting a Team that Spaces still list (LD-16). */
export const TEAM_STILL_REFERENCED_CODE = "workspaces.team.still-referenced";

/** A Space that still lists the Team, as the caller may see it. */
export interface TeamReference {
  readonly kind: "space";
  readonly id: string;
  readonly name: string;
  readonly spaceType: string;
}

/**
 * Why a Team could not be deleted: the Spaces still listing it that the caller can see, and whether others
 * exist that they cannot (no number, so no count of hidden Spaces reaches the client).
 */
export interface TeamReferencesRefusal {
  readonly references: readonly TeamReference[];
  readonly moreHidden: boolean;
}

/** Reads the refusal of a Team delete; `null` for any other failure. */
export function parseTeamReferencesRefusal(
  error: unknown,
): TeamReferencesRefusal | null {
  const candidate = (asRecord(error) ?? {}) as ErrorLike;
  const body = asRecord(candidate.details);
  if (body?.errorCode !== TEAM_STILL_REFERENCED_CODE) return null;
  const rows = Array.isArray(body.references) ? body.references : [];
  const references = rows.flatMap((row): TeamReference[] => {
    const record = asRecord(row);
    return record &&
      typeof record.id === "string" &&
      typeof record.name === "string"
      ? [
          {
            kind: "space",
            id: record.id,
            name: record.name,
            spaceType:
              typeof record.spaceType === "string" ? record.spaceType : "",
          },
        ]
      : [];
  });
  return { references, moreHidden: body.moreReferencesHidden === true };
}

/** Why an initial Team or Space entry of an invitation was refused (the producer's `entries` extension). */
export type InvitationEntryRefusalReason =
  "not-active" | "guest-too-privileged";

export interface InvitationEntryRefusal {
  readonly kind: "team" | "space";
  readonly targetId: string;
  readonly reason: InvitationEntryRefusalReason;
}

/** Reads the refused entries of an invitation (additive `entries` on the problem); empty for any other failure. */
export function parseInvitationEntryRefusals(
  error: unknown,
): readonly InvitationEntryRefusal[] {
  const candidate = (asRecord(error) ?? {}) as ErrorLike;
  const body = asRecord(candidate.details);
  const rows = Array.isArray(body?.entries) ? body.entries : [];
  return rows.flatMap((row): InvitationEntryRefusal[] => {
    const record = asRecord(row);
    if (
      !record ||
      (record.kind !== "team" && record.kind !== "space") ||
      typeof record.targetId !== "string" ||
      (record.reason !== "not-active" &&
        record.reason !== "guest-too-privileged")
    )
      return [];
    return [
      { kind: record.kind, targetId: record.targetId, reason: record.reason },
    ];
  });
}
