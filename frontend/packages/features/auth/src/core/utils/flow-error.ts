/** What a refused sign-in call says, read from the error body the producer sent. */
export interface FlowError {
  readonly status: number | null;
  readonly code: string | null;
  readonly retryAfterSeconds: number | null;
  readonly fieldErrors: Readonly<Record<string, string>>;
  /** The call never reached the server. */
  readonly network: boolean;
}

/**
 * The code a refusal carries: the Identity operations answer a dotted
 * `errorCode` (`identity.auth.invalid-credentials`); the pages still on a
 * mocked contract answer `code`.
 */
function errorCodeOf(details: Record<string, unknown>): string | null {
  if (typeof details.errorCode === "string") return details.errorCode;
  return typeof details.code === "string" ? details.code : null;
}

/** The dotted Identity codes the sign-in page tells apart. */
export const IDENTITY_ERROR = {
  invalidCredentials: "identity.auth.invalid-credentials",
  tooManyAttempts: "identity.auth.too-many-attempts",
  invalidCode: "identity.mfa.invalid-code",
  challengeInvalid: "identity.mfa.challenge-invalid",
  accountInactive: "identity.mfa.account-inactive",
  /** The provider sign-in is for an account with a second factor: no session was issued. */
  oauthMfaRequired: "identity.oauth.mfa-required",
} as const;

export function readFlowError(error: unknown): FlowError {
  const err = (error ?? {}) as {
    status?: number;
    kind?: string;
    details?: Record<string, unknown> | null;
    validationErrors?: Record<string, string[]>;
  };
  const details = (err.details ?? {}) as Record<string, unknown>;
  const num = (value: unknown) =>
    typeof value === "number" && Number.isFinite(value) ? value : null;
  const fieldErrors: Record<string, string> = {};
  const source =
    err.validationErrors ??
    (details.errors as Record<string, string[]> | undefined) ??
    {};
  // The producer names a field after its property (`Password`); the pages key on `password`.
  for (const [field, messages] of Object.entries(source))
    if (Array.isArray(messages) && messages[0])
      fieldErrors[field.charAt(0).toLowerCase() + field.slice(1)] = messages[0];
  return {
    status: typeof err.status === "number" ? err.status : null,
    code: errorCodeOf(details),
    retryAfterSeconds: num(details.retryAfterSeconds),
    fieldErrors,
    network: err.kind === "network",
  };
}
