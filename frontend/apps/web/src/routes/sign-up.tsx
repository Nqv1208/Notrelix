import { useMemo } from 'react';
import { createRegisterForm } from '@notrelix/features-auth';
import { useAppRuntime } from '@notrelix/runtime-web';
import { AuthLayout } from './auth-layout';

export function SignUpPage() {
  const { api: runtimeClient } = useAppRuntime();
  const RegisterForm = useMemo(
    () => createRegisterForm({ api: runtimeClient.api, endpoints: runtimeClient.endpoints }),
    [runtimeClient],
  );

  return (
    <AuthLayout>
      <RegisterForm />
    </AuthLayout>
  );
}
