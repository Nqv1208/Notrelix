import { useEffect, useRef } from "react";
import { useLocation, useNavigate } from "@tanstack/react-router";
import { SignInFlow, useAuth } from "@notrelix/features-auth";
import { sanitizeInternalReturnUrl } from "../routing/sanitize-return-url";
import { AuthLayout } from "./auth-layout";
import { useAuthFlowDeps } from "./auth-flow-deps";

export function SignInPage() {
  const { service, navigation } = useAuthFlowDeps();
  // Subscribing to the location re-renders the page when the address changes in place.
  const { searchStr } = useLocation();
  const search = new URLSearchParams(searchStr);
  const { isAuthenticated, isLoading } = useAuth();
  const navigate = useNavigate();
  const checked = useRef(false);
  // Someone who already has a session and types this address is sent on. Only the
  // first answer counts: a sign-in finished on this page navigates on its own, and
  // an MFA step or a provider error needs the form.
  useEffect(() => {
    if (isLoading || checked.current) return;
    checked.current = true;
    if (!isAuthenticated) return;
    if (search.get("step") === "mfa" || search.get("error")) return;
    const target = search.get("returnTo") ?? search.get("redirect");
    void navigate({
      to: target ? sanitizeInternalReturnUrl(target) : "/home",
      replace: true,
    });
  });
  return (
    <AuthLayout>
      <SignInFlow
        // A provider round trip in the mock world lands on this same route, so the
        // flow starts over for each new address rather than keeping its old step.
        key={searchStr}
        service={service}
        navigation={navigation}
        redirect={search.get("returnTo") ?? search.get("redirect")}
        initialEmail={search.get("email") ?? ""}
        providerError={search.get("error")}
        sessionEnded={search.get("reason") === "session-ended"}
        passwordReset={search.get("reason") === "password-reset"}
        initialStep={search.get("step") === "mfa" ? "mfa" : null}
      />
    </AuthLayout>
  );
}
