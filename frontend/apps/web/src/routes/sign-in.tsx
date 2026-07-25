import { Suspense } from 'react';
import { createLoginForm } from '@notrelix/features-auth';
import { api, endpoints } from '@notrelix/contracts';
import { AuthLayout } from './auth-layout';

const LoginForm = createLoginForm({ api, endpoints });

export function SignInPage() {
  return (
    <AuthLayout>
      <Suspense fallback={<div className="flex h-40 items-center justify-center text-sm text-muted-foreground">Loading form...</div>}>
        <LoginForm />
      </Suspense>
    </AuthLayout>
  );
}
