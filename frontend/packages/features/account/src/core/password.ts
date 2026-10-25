/**
 * The new password of a change, checked before it is sent. The producer owns
 * the rule (`PasswordPolicy.MinimumLength` and "different from the current
 * password") and checks it again; this mirror only saves a round trip, so it
 * must not be stricter than the producer.
 */
export const PASSWORD_MIN_LENGTH = 8;

export type NewPasswordProblem = "too-short" | "same-as-current" | "mismatch";

export function checkNewPassword(input: {
  readonly current: string;
  readonly next: string;
  readonly confirm: string;
}): NewPasswordProblem | null {
  if (input.next.length < PASSWORD_MIN_LENGTH) return "too-short";
  if (input.next === input.current) return "same-as-current";
  if (input.confirm !== input.next) return "mismatch";
  return null;
}

/** What went wrong with a refused change of password, as the page tells it. */
export type PasswordChangeFailure =
  | { readonly kind: "wrong-current" }
  | { readonly kind: "wrong-code" }
  | { readonly kind: "rejected" }
  | { readonly kind: "wait"; readonly seconds: number }
  | { readonly kind: "failed" };

interface RefusalLike {
  readonly status?: number;
  readonly code?: unknown;
  readonly details?: {
    readonly retryAfterSeconds?: unknown;
    readonly errors?: unknown;
  } | null;
  readonly validationErrors?: Record<string, unknown>;
}

/**
 * Reads a refusal of the step-up proof (`stage: "proof"`) or of the change
 * itself (`stage: "change"`). Dotted `errorCode`s: the change answers
 * `identity.auth.invalid-current-password` (400) for a wrong current password;
 * the proof answers `identity.security.step-up-invalid-code` or
 * `...-invalid-password`; a throttled call answers 429 with
 * `retryAfterSeconds`. A 400 naming the new password is a refused new password.
 */
export function passwordChangeFailureOf(
  error: unknown,
  stage: "proof" | "change",
): PasswordChangeFailure {
  const refusal = (error ?? {}) as RefusalLike;
  if (refusal.status === 429) {
    const seconds = refusal.details?.retryAfterSeconds;
    return {
      kind: "wait",
      seconds: typeof seconds === "number" && seconds > 0 ? seconds : 1,
    };
  }
  const code = typeof refusal.code === "string" ? refusal.code : "";
  if (code === "identity.auth.invalid-current-password")
    return { kind: "wrong-current" };
  if (code === "identity.security.step-up-invalid-code")
    return { kind: "wrong-code" };
  if (code === "identity.security.step-up-invalid-password")
    return { kind: "wrong-current" };
  if (stage === "change" && refusal.status === 400) {
    const fields = Object.keys(
      refusal.validationErrors ??
        (refusal.details?.errors as Record<string, unknown> | undefined) ??
        {},
    ).map((field) => field.toLowerCase());
    if (fields.includes("newpassword")) return { kind: "rejected" };
  }
  return { kind: "failed" };
}
