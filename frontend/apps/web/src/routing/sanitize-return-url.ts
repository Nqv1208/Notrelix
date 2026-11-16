import { safeReturnPath } from "@notrelix/features-auth";

/**
 * The internal path to return to after sign-in, or "/" when the input is not one.
 * Uses the same rule as the sign-in page and the backend, so a path that one of
 * them accepts is not dropped by another (a query that carries a link, say).
 */
export function sanitizeInternalReturnUrl(input: string): string {
  return safeReturnPath(input) ?? "/";
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
