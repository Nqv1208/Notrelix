/**
 * Sanitizes an internal return URL to prevent open redirect vulnerabilities.
 * Keeps pathname + search + hash if it is a valid internal relative URL starting with a single slash.
 */
export function sanitizeInternalReturnUrl(input: string): string {
  if (!input || typeof input !== 'string') return '/';
  // Must start with single slash and not double slash or javascript:
  if (!input.startsWith('/') || input.startsWith('//')) return '/';
  if (input.includes(':\\') || input.includes(':/')) return '/';
  return input;
}
