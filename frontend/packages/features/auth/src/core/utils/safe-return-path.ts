/** Longest path the sign-in return accepts; the backend caps it the same way. */
export const RETURN_PATH_MAX_LENGTH = 512;

/** Control characters, including CR and LF, that could split a header or hide a character. */
function hasControlCharacter(value: string): boolean {
  for (let index = 0; index < value.length; index += 1) {
    const code = value.charCodeAt(index);
    if (code <= 0x1f || (code >= 0x7f && code <= 0x9f)) return true;
  }
  return false;
}

function isSafeLiteral(value: string): boolean {
  if (value.length === 0 || value[0] !== "/") return false;
  // "//host" and "/\host" are protocol-relative for a browser.
  if (value.length > 1 && (value[1] === "/" || value[1] === "\\")) return false;
  return !value.includes("\\") && !hasControlCharacter(value);
}

/**
 * The only place a sign-in may send a person back to: an internal, root-relative
 * path. Scheme URLs, protocol-relative paths, backslash tricks, control characters
 * and percent-encoded variants of those are refused. Mirrors the backend rule
 * (`SafeReturnPath` in the Identity application) so the page and the API agree.
 */
export function isSafeReturnPath(
  path: string | null | undefined,
): path is string {
  if (typeof path !== "string" || path.length > RETURN_PATH_MAX_LENGTH)
    return false;
  let current = path;
  // The value is only inspected, never decoded for use: a double-encoded variant is still refused.
  for (let pass = 0; pass < 3; pass += 1) {
    if (!isSafeLiteral(current)) return false;
    let decoded: string;
    try {
      decoded = decodeURIComponent(current);
    } catch {
      return false;
    }
    if (decoded === current) return true;
    current = decoded;
  }
  return false;
}

/** The path when it is safe to navigate to, otherwise `null`. */
export function safeReturnPath(path: string | null | undefined): string | null {
  return isSafeReturnPath(path) ? path : null;
}
