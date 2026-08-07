const sensitiveKeyPattern = /token|secret|password|authorization|cookie|email|body|content|payload/i;

export function redactTelemetryProperties(
  properties: Record<string, unknown> | undefined
): Record<string, unknown> | undefined {
  if (!properties) return undefined;

  return Object.fromEntries(
    Object.entries(properties).map(([key, value]) => [
      key,
      sensitiveKeyPattern.test(key) ? '[redacted]' : value,
    ])
  );
}
