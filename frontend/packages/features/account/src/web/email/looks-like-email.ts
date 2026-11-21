/**
 * Whether a trimmed address has the shape local@domain.tld: no whitespace,
 * exactly one "@", and a "." inside the domain that is neither first nor last.
 * Plain string code so the cost stays linear on any input.
 */
export function looksLikeEmail(email: string): boolean {
  if (/\s/.test(email)) return false;
  const at = email.indexOf("@");
  if (at < 1 || email.indexOf("@", at + 1) >= 0) return false;
  const domain = email.slice(at + 1);
  const dot = domain.indexOf(".", 1);
  return dot >= 0 && dot < domain.length - 1;
}
