import { useCallback, useMemo } from 'react';
import { useNavigate } from '@tanstack/react-router';
import { createRegisterForm } from '@notrelix/features-auth';
import { useAppRuntime } from '@notrelix/runtime-web';
import { AuthLayout } from './auth-layout';

export function SignUpPage() {
  const { api: runtimeClient } = useAppRuntime();
  const navigate = useNavigate();
  const getSearchParams = useCallback(() => new URLSearchParams(window.location.search), []);

  const RegisterForm = useMemo(
    () =>
      createRegisterForm({
        api: runtimeClient.api,
        endpoints: runtimeClient.endpoints,
        navigate: (options) => navigate({ to: options.to, replace: options.replace }),
        getSearchParams,
      }),
    [runtimeClient, navigate, getSearchParams],
  );

  return (
    <AuthLayout>
      <RegisterForm />
    </AuthLayout>
  );
}
