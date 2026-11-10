import { useEffect, useState } from "react";
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
  // "checking" until the session answers, "leaving" when a signed-in person is sent
  // on, "form" otherwise. Only the first answer counts: a sign-in finished on this
  // page navigates on its own, and an MFA step or a provider error needs the form.
  // The form is not drawn before the answer, so it never flashes before the move.
  const [phase, setPhase] = useState<"checking" | "leaving" | "form">(
    "checking",
  );
  useEffect(() => {
    if (isLoading || phase !== "checking") return;
    if (
      !isAuthenticated ||
      search.get("step") === "mfa" ||
      search.get("error")
    ) {
      setPhase("form");
      return;
    }
    setPhase("leaving");
    const target = search.get("returnTo") ?? search.get("redirect");
    void navigate({
      to: target ? sanitizeInternalReturnUrl(target) : "/home",
      replace: true,
    });
  });
  if (phase !== "form") {
    return <div className="bg-background h-screen w-screen" aria-hidden />;
  }
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
