import { createForgotPasswordForm } from '@notrelix/features-auth';
import { api, endpoints } from '@notrelix/contracts';
import { AuthLayout } from './auth-layout';

const ForgotPasswordForm = createForgotPasswordForm({ api, endpoints });

export function ForgotPasswordPage() {
  return (
    <AuthLayout>
      <ForgotPasswordForm />
    </AuthLayout>
  );
}
