import { SignInFlow } from "@notrelix/features-auth";
import { AuthLayout } from "./auth-layout";
import { useAuthFlowDeps } from "./auth-flow-deps";

export function SignInPage() {
  const { service, navigation } = useAuthFlowDeps();
  const search = new URLSearchParams(window.location.search);
  return (
    <AuthLayout>
      <SignInFlow
        service={service}
        navigation={navigation}
        redirect={search.get("redirect")}
        initialEmail={search.get("email") ?? ""}
        providerError={search.get("error")}
      />
    </AuthLayout>
  );
}
