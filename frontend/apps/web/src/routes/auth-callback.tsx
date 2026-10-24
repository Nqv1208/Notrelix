import { useEffect } from "react";
import { useRouter } from "@tanstack/react-router";
import { safeReturnPath } from "@notrelix/features-auth";

/**
 * Where the backend sends the browser after a Google or Microsoft sign-in
 * that opened a session (the session is in httpOnly cookies the page never
 * reads). It goes back to where the person was (`?returnTo=`) only when that
 * is an internal path; anything else lands on the home page, whose session
 * check reads the account and routes on.
 */
export function AuthCallbackPage() {
  const router = useRouter();
  useEffect(() => {
    const returnTo = safeReturnPath(
      new URLSearchParams(window.location.search).get("returnTo"),
    );
    router.history.replace(returnTo ?? "/home");
  }, [router]);
  return null;
}
