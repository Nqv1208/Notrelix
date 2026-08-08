import { Suspense, useCallback, useMemo } from 'react';
import { useNavigate } from '@tanstack/react-router';
import { createLoginForm } from '@notrelix/features-auth';
import { useAppRuntime } from '@notrelix/runtime-web';
import { AuthLayout } from './auth-layout';

export function SignInPage() {
  const { api: runtimeClient } = useAppRuntime();
  const navigate = useNavigate();
  const getSearchParams = useCallback(() => new URLSearchParams(window.location.search), []);

  const LoginForm = useMemo(
    () =>
      createLoginForm({
        api: runtimeClient.api,
        endpoints: runtimeClient.endpoints,
        navigate: (options) => navigate({ to: options.to, replace: options.replace }),
        getSearchParams,
      }),
    [runtimeClient, navigate, getSearchParams],
  );

  return (
    <AuthLayout>
      <Suspense fallback={<div className="flex h-40 items-center justify-center text-sm text-muted-foreground">Loading form...</div>}>
        <LoginForm />
      </Suspense>
    </AuthLayout>
  );
}
