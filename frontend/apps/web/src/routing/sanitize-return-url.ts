/**
 * Sanitizes an internal return URL to prevent open redirect vulnerabilities.
 * Keeps pathname + search + hash if it is a valid internal relative URL starting with a single slash.
 */
export function sanitizeInternalReturnUrl(input: string): string {
  if (!input || typeof input !== "string") return "/";
  // Must start with single slash and not double slash or javascript:
  if (!input.startsWith("/") || input.startsWith("//")) return "/";
  if (input.includes(":\\") || input.includes(":/")) return "/";
  return input;
}

/** Pages a person without a session opens on purpose: a 401 there must not bounce them to sign-in. */
/**
 * Pages a visitor with no session opens directly (a reload, a link from an email, a new tab).
 * Mirrors the public routes in `router/routes/base.routes.tsx`; a path matches itself or a
 * child segment (`/invite/abc`), never a longer name (`/sign-in-evil`).
 */
const signedOutRoutes = [
  "/sign-in",
  "/sign-up",
  "/forgot-password",
  "/reset-password",
  "/verify-email",
  "/auth/callback",
  "/invite",
];

export function isSignedOutRoute(pathname: string): boolean {
  return signedOutRoutes.some(
    (route) => pathname === route || pathname.startsWith(`${route}/`),
  );
}
