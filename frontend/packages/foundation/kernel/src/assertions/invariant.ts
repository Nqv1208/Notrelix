/**
 * Invariant assertion helper
 * Throws an error if the condition is false
 */
export function invariant(
  condition: unknown,
  message: string,
): asserts condition {
  if (!condition) {
    throw new Error(`Invariant violation: ${message}`);
  }
}

export function assertNonNull<T>(
  value: T | null | undefined,
  message: string,
): T {
  if (value === null || value === undefined) {
    throw new Error(`Assertion failed: ${message}`);
  }
  return value;
}
