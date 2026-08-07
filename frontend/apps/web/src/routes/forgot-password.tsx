import { useMemo } from 'react';
import { createForgotPasswordForm } from '@notrelix/features-auth';
import { useAppRuntime } from '@notrelix/runtime-web';
import { AuthLayout } from './auth-layout';

export function ForgotPasswordPage() {
  const { api: runtimeClient } = useAppRuntime();
  const ForgotPasswordForm = useMemo(
    () => createForgotPasswordForm({ api: runtimeClient.api, endpoints: runtimeClient.endpoints }),
    [runtimeClient],
  );

  return (
    <AuthLayout>
      <ForgotPasswordForm />
    </AuthLayout>
  );
}
