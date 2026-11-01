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
  /** The provider is unknown, switched off or not set up: the start made no redirect to it. */
  oauthProviderUnavailable: "identity.oauth.provider-unavailable",
  /** The sign-in took too long or was already used: the state is gone. */
  oauthStateInvalid: "identity.oauth.state-invalid",
  /** The person stopped at the provider. */
  oauthDenied: "identity.oauth.denied",
  oauthEmailUnverified: "identity.oauth.email-unverified",
  /** The provider's verified email belongs to an account that is not linked to it. */
  oauthEmailInUse: "identity.oauth.email-in-use",
  /** A confirmation link that was valid and has run out (an unknown or used one answers no code). */
  emailVerificationExpired: "identity.email-verification.token-expired",
} as const;

/** The API's volume limit (HTTP 429): the answer carries how long to wait. */
export const RATE_LIMIT_ERROR_CODE = "rate_limit.exceeded";

/** A refusal that says "wait": the account throttle or the API's volume limit. */
export function isThrottled(failure: FlowError): boolean {
  return (
    failure.status === 429 ||
    failure.code === IDENTITY_ERROR.tooManyAttempts ||
    failure.code === RATE_LIMIT_ERROR_CODE
  );
}

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
    retryAfterSeconds:
      num(details.retryAfterSeconds) ?? num(details.retryAfter),
    fieldErrors,
    network: err.kind === "network",
  };
}
