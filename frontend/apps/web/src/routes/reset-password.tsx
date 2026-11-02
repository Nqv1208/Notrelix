import { ResetFlow } from "@notrelix/features-auth";
import { AuthLayout } from "./auth-layout";
import { useAuthFlowDeps } from "./auth-flow-deps";

export function ResetPasswordPage() {
  const { service, navigation } = useAuthFlowDeps();
  const search = new URLSearchParams(window.location.search);
  return (
    <AuthLayout>
      <ResetFlow
        service={service}
        navigation={navigation}
        initialEmail={search.get("email") ?? ""}
      />
    </AuthLayout>
  );
}
