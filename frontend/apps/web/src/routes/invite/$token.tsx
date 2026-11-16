import { useParams } from "@tanstack/react-router";
import { InviteFlow } from "@notrelix/features-auth";
import { AuthLayout } from "../auth-layout";
import { useAuthFlowDeps } from "../auth-flow-deps";
import { useInviteAccount } from "./use-invite-account";

/** A guest invitation to one board, opened from the link in the email. */
export function InvitePage() {
  const { token } = useParams({ from: "/invite/$token" });
  const { service, navigation } = useAuthFlowDeps();
  const account = useInviteAccount();
  return (
    <AuthLayout>
      <InviteFlow
        service={service}
        navigation={navigation}
        token={token}
        {...account}
      />
    </AuthLayout>
  );
}
