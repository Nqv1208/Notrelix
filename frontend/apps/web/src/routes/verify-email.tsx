import { VerifyEmailFlow } from "@notrelix/features-auth";
import { AuthLayout } from "./auth-layout";
import { useAuthFlowDeps } from "./auth-flow-deps";

/** The token of the link in the confirmation email: `#token=` (the backend's link), or `?token=`. */
function readToken(): string | null {
  const fromHash = new URLSearchParams(window.location.hash.replace(/^#/, ""));
  const fromQuery = new URLSearchParams(window.location.search);
  return fromHash.get("token") ?? fromQuery.get("token");
}

/** Opened from the confirmation email: confirms the address, or asks for a new link. */
export function VerifyEmailPage() {
  const { service, navigation } = useAuthFlowDeps();
  const search = new URLSearchParams(window.location.search);
  return (
    <AuthLayout>
      <VerifyEmailFlow
        service={service}
        navigation={navigation}
        token={readToken()}
        initialEmail={search.get("email") ?? ""}
      />
    </AuthLayout>
  );
}
