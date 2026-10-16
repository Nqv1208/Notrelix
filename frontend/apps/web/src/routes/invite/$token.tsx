import { useParams } from "@tanstack/react-router";
import { InviteFlow } from "@notrelix/features-auth";
import { AuthLayout } from "../auth-layout";
import { useAuthFlowDeps } from "../auth-flow-deps";

/** A guest invitation to one board, opened from the link in the email. */
export function InvitePage() {
  const { token } = useParams({ from: "/invite/$token" });
  const { service, navigation } = useAuthFlowDeps();
  return (
    <AuthLayout>
      <InviteFlow service={service} navigation={navigation} token={token} />
    </AuthLayout>
  );
}
