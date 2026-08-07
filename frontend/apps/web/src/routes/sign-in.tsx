import { Suspense, useMemo } from 'react';
import { createLoginForm } from '@notrelix/features-auth';
import { useAppRuntime } from '@notrelix/runtime-web';
import { AuthLayout } from './auth-layout';

export function SignInPage() {
  const { api: runtimeClient } = useAppRuntime();
  const LoginForm = useMemo(
    () => createLoginForm({ api: runtimeClient.api, endpoints: runtimeClient.endpoints }),
    [runtimeClient],
  );

  return (
    <AuthLayout>
      <Suspense fallback={<div className="flex h-40 items-center justify-center text-sm text-muted-foreground">Loading form...</div>}>
        <LoginForm />
      </Suspense>
    </AuthLayout>
  );
}
