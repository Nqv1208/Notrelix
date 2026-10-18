/** What a refused sign-in call says, read from the error body the producer sent. */
export interface FlowError {
  readonly status: number | null;
  readonly code: string | null;
  readonly attemptsLeft: number | null;
  readonly retryAfterSeconds: number | null;
  readonly fieldErrors: Readonly<Record<string, string>>;
  /** The call never reached the server. */
  readonly network: boolean;
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
  for (const [field, messages] of Object.entries(source))
    if (Array.isArray(messages) && messages[0])
      fieldErrors[field] = messages[0];
  return {
    status: typeof err.status === "number" ? err.status : null,
    code: typeof details.code === "string" ? details.code : null,
    attemptsLeft: num(details.attemptsLeft),
    retryAfterSeconds: num(details.retryAfterSeconds),
    fieldErrors,
    network: err.kind === "network",
  };
}
