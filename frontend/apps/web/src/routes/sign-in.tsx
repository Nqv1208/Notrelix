import { useLocation } from "@tanstack/react-router";
import { SignInFlow } from "@notrelix/features-auth";
import { AuthLayout } from "./auth-layout";
import { useAuthFlowDeps } from "./auth-flow-deps";

export function SignInPage() {
  const { service, navigation } = useAuthFlowDeps();
  // Subscribing to the location re-renders the page when the address changes in place.
  const { searchStr } = useLocation();
  const search = new URLSearchParams(searchStr);
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
        initialStep={search.get("step") === "mfa" ? "mfa" : null}
      />
    </AuthLayout>
  );
}
