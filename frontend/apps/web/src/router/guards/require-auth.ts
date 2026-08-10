import { redirect } from "@tanstack/react-router";
import type { AuthSnapshot } from "../context";
import { sanitizeInternalReturnUrl } from "../../routing/sanitize-return-url";

export function requireAuth(
  auth: AuthSnapshot | undefined,
  currentPath: string,
): void {
  if (auth?.isAuthenticated) return;

  throw redirect({
    to: "/sign-in",
    search: { redirect: sanitizeInternalReturnUrl(currentPath) },
    replace: true,
  });
}
