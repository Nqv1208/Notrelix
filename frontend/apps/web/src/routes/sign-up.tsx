import { createRegisterForm } from '@notrelix/features-auth';
import { api, endpoints } from '@notrelix/contracts';
import { AuthLayout } from './auth-layout';

const RegisterForm = createRegisterForm({ api, endpoints });

export function SignUpPage() {
  return (
    <AuthLayout>
      <RegisterForm />
    </AuthLayout>
  );
}
