'use client';

import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { loginSchema, type LoginRequest, parseAuthError, resolveErrorDisplay } from '../../core';
import { createUseLogin } from '../hooks/use-login';
import type { AuthApiClient, AuthEndpoints } from '../../core/api/auth.service';

interface LoginFormProps {
  api: AuthApiClient;
  endpoints: AuthEndpoints;
  translate?: (key: string) => string;
}

export function createLoginForm(deps: LoginFormProps) {
  const useLogin = createUseLogin(deps);
  const t = deps.translate ?? ((key: string) => key);

  return function LoginForm({
    className,
    ...props
  }: React.ComponentPropsWithoutRef<'form'>) {
    const loginMutation = useLogin();
    const [serverError, setServerError] = useState<string | null>(null);
    const [showPassword, setShowPassword] = useState(false);

    const {
      register,
      handleSubmit,
      setError,
      formState: { errors },
    } = useForm<LoginRequest>({
      resolver: zodResolver(loginSchema),
      defaultValues: { email: '', password: '' },
    });

    const onSubmit = (data: LoginRequest) => {
      setServerError(null);
      loginMutation.mutate(data, {
        onError: (error) => {
          const parsed = parseAuthError(error);
          if (parsed.fieldErrors.email)
            setError('email', { type: 'server', message: String(parsed.fieldErrors.email) });
          if (parsed.fieldErrors.password)
            setError('password', { type: 'server', message: String(parsed.fieldErrors.password) });
          setServerError(parsed.messageKey ? t(parsed.messageKey) : parsed.rawMessage);
        },
      });
    };

    return (
      <form
        onSubmit={handleSubmit(onSubmit)}
        className={`flex flex-col gap-7 ${className ?? ''}`}
        {...props}
      >
        <div>
          <h1 className="text-2xl font-bold tracking-tight mb-1.5">Welcome back</h1>
          <p className="text-muted-foreground text-[15px]">
            Sign in to your Notrelix workspace
          </p>
        </div>

        {serverError && (
          <div className="flex items-start gap-2.5 rounded-lg bg-destructive/10 border border-destructive/20 px-4 py-3 text-sm text-destructive">
            <span>{serverError}</span>
          </div>
        )}

        <div className="space-y-4">
          <div className="space-y-2">
            <label htmlFor="email" className="text-sm font-medium">Email</label>
            <input
              id="email"
              type="email"
              placeholder="you@company.com"
              autoComplete="email"
              className={`flex h-10 w-full rounded-md border bg-input px-3 py-2 text-sm ${errors.email ? 'border-destructive' : 'border-input'}`}
              {...register('email')}
            />
            {errors.email && (
              <p className="text-xs text-destructive">{resolveErrorDisplay(errors.email.message, t)}</p>
            )}
          </div>

          <div className="space-y-2">
            <label htmlFor="password" className="text-sm font-medium">Password</label>
            <input
              id="password"
              type={showPassword ? 'text' : 'password'}
              placeholder="Enter your password"
              autoComplete="current-password"
              className={`flex h-10 w-full rounded-md border bg-input px-3 py-2 text-sm ${errors.password ? 'border-destructive' : 'border-input'}`}
              {...register('password')}
            />
            {errors.password && (
              <p className="text-xs text-destructive">{resolveErrorDisplay(errors.password.message, t)}</p>
            )}
          </div>
        </div>

        <button
          type="submit"
          className="inline-flex items-center justify-center h-10 w-full rounded-md bg-primary text-primary-foreground hover:bg-primary/90 font-medium"
          disabled={loginMutation.isPending}
        >
          {loginMutation.isPending ? 'Signing in...' : 'Sign in'}
        </button>
      </form>
    );
  };
}
