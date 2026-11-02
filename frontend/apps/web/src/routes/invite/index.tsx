import { InviteFlow } from "@notrelix/features-auth";
import { AuthLayout } from "../auth-layout";
import { useAuthFlowDeps } from "../auth-flow-deps";

/** The token of the link in the invitation email: `#token=` (the backend's link, kept out of server logs). */
function readToken(): string {
  return (
    new URLSearchParams(window.location.hash.replace(/^#/, "")).get("token") ??
    ""
  );
}

/** Opened from the link in a workspace invitation email. */
export function InviteLinkPage() {
  const { service, navigation } = useAuthFlowDeps();
  return (
    <AuthLayout>
      <InviteFlow
        service={service}
        navigation={navigation}
        token={readToken()}
      />
    </AuthLayout>
  );
}
