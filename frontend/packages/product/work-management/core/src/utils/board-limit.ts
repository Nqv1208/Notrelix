/** The published board limit a 409 named (D78.8): `work-management.limit.*` with limit and max. */
export function limitOf(
  error: unknown,
): { name: string; max: number | null } | null {
  const code = (error as { code?: unknown } | null)?.code;
  if (typeof code !== "string" || !code.startsWith("work-management.limit."))
    return null;
  const body = (error as { details?: unknown } | null)?.details as
    Record<string, unknown> | null | undefined;
  return {
    name: typeof body?.limit === "string" ? body.limit : "other",
    max: typeof body?.max === "number" ? body.max : null,
  };
}
