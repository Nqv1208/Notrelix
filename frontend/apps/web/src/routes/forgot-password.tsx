import { ForgotFlow } from "@notrelix/features-auth";
import { AuthLayout } from "./auth-layout";
import { useAuthFlowDeps } from "./auth-flow-deps";

export function ForgotPasswordPage() {
  const { service, navigation } = useAuthFlowDeps();
  const search = new URLSearchParams(window.location.search);
  return (
    <AuthLayout>
      <ForgotFlow
        service={service}
        navigation={navigation}
        initialEmail={search.get("email") ?? ""}
      />
    </AuthLayout>
  );
}
