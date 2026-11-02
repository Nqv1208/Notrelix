import { RegisterFlow } from "@notrelix/features-auth";
import { AuthLayout } from "./auth-layout";
import { useAuthFlowDeps } from "./auth-flow-deps";

export function SignUpPage() {
  const { service, navigation } = useAuthFlowDeps();
  return (
    <AuthLayout>
      <RegisterFlow service={service} navigation={navigation} />
    </AuthLayout>
  );
}
