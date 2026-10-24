import { Navigate } from "@tanstack/react-router";

/**
 * Where the backend sends the browser after a Google or Microsoft sign-in
 * that opened a session (the session is in httpOnly cookies the page never
 * reads). The home page's session check reads the account and routes on.
 */
export function AuthCallbackPage() {
  return <Navigate to="/home" replace />;
}
