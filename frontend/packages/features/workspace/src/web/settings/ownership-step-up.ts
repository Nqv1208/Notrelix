/**
 * What the workspace settings need from the host to prove the Owner is present before the workspace is handed
 * over. The host owns the identity step-up (it knows the account feature and the API); the workspace feature
 * only asks and shows the steps, so it never imports another feature.
 */

/** What proves presence: an authenticator or recovery code, the current password, or a sign-in provider. */
export type OwnershipProofFactor = "code" | "password" | "provider";

export interface OwnershipProofRequest {
  readonly factor: OwnershipProofFactor;
  /** The challenge a code answer quotes (only with the `code` factor). */
  readonly challengeToken: string | null;
}

export interface OwnershipStepUp {
  /** Asks which factor to satisfy; rejects when the host cannot be reached. */
  request(): Promise<OwnershipProofRequest>;
  /** Resolves the single-use proof of a code answer; rejects with the host's error when it is refused. */
  withCode(challengeToken: string, code: string): Promise<string>;
  /** Resolves the single-use proof of a password answer. */
  withPassword(password: string): Promise<string>;
}

/** Why a proof was not given. */
export type OwnershipProofRefusal =
  | { readonly kind: "wrong" }
  | { readonly kind: "expired" }
  | { readonly kind: "wait"; readonly seconds: number }
  | { readonly kind: "network" }
  | { readonly kind: "failed" };

/** Reads the host's error: the producer's stable codes, the throttle's 429, or no answer at all. */
export function describeProofRefusal(error: unknown): OwnershipProofRefusal {
  const e = (error ?? {}) as {
    status?: number;
    code?: string;
    kind?: string;
    details?: { retryAfterSeconds?: unknown };
  };
  if (e.status === 429) {
    const seconds = e.details?.retryAfterSeconds;
    return {
      kind: "wait",
      seconds: typeof seconds === "number" && seconds > 0 ? seconds : 1,
    };
  }
  if (e.code === "identity.security.step-up-invalid")
    return { kind: "expired" };
  if (
    e.code === "identity.security.step-up-invalid-code" ||
    e.code === "identity.security.step-up-invalid-password"
  )
    return { kind: "wrong" };
  if (e.kind === "network" || e.kind === "aborted") return { kind: "network" };
  return { kind: "failed" };
}
