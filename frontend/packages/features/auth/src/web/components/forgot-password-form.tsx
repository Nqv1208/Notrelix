import * as React from 'react';
import { Link } from '@tanstack/react-router';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import {
  Mail, ArrowLeft, ArrowRight, Loader2,
  AlertCircle, Lock, Eye, EyeOff, ShieldCheck,
} from 'lucide-react';
import { cn } from '@notrelix/ui-web';
import { Button } from '@notrelix/ui-web';
import { Input } from '@notrelix/ui-web';
import { Label } from '@notrelix/ui-web';
import {
  InputOTP,
  InputOTPGroup,
  InputOTPSlot,
  InputOTPSeparator,
} from '@notrelix/ui-web';
import { REGEXP_ONLY_DIGITS } from 'input-otp';
import {
  forgotPasswordSchema,
  resetPasswordSchema,
  type ForgotPasswordRequest,
  type ResetPasswordRequest,
  parseAuthError,
  resolveErrorDisplay,
} from '~/core';
import type { AuthApiClient, AuthEndpoints } from '~/core/api/auth.service';
import { createAuthService } from '~/core/api/auth.service';

interface ForgotPasswordFormDeps {
  api: AuthApiClient;
  endpoints: AuthEndpoints;
  translate?: (key: string) => string;
}

type Step = 'email' | 'otp' | 'success';

const resetFormSchema = resetPasswordSchema
  .extend({
    confirmPassword: resetPasswordSchema.shape.newPassword,
  })
  .refine((data) => data.newPassword === data.confirmPassword, {
    message: "Passwords don't match",
    path: ['confirmPassword'],
  });

type ResetFormRequest = {
  email: string;
  code: string;
  newPassword: string;
  confirmPassword: string;
};

export function createForgotPasswordForm(deps: ForgotPasswordFormDeps) {
  const authService = createAuthService(deps.api, deps.endpoints);
  const t = deps.translate ?? ((key: string) => key);

  return function ForgotPasswordForm() {
    const [step, setStep] = React.useState<Step>('email');
    const [email, setEmail] = React.useState('');
    const [isSending, setIsSending] = React.useState(false);
    const [isResetting, setIsResetting] = React.useState(false);
    const [showPassword, setShowPassword] = React.useState(false);
    const [showConfirm, setShowConfirm] = React.useState(false);

    const emailForm = useForm<ForgotPasswordRequest>({
      resolver: zodResolver(forgotPasswordSchema),
      defaultValues: { email: '' },
    });

    const resetForm = useForm<ResetFormRequest>({
      resolver: zodResolver(resetFormSchema),
      defaultValues: { email: '', code: '', newPassword: '', confirmPassword: '' },
    });

    const forgotError = emailForm.formState.errors.root?.message ?? null;
    const resetError = resetForm.formState.errors.root?.message ?? null;

    const onEmailSubmit = async (data: ForgotPasswordRequest) => {
      try {
        setIsSending(true);
        await authService.forgotPassword(data);
        setEmail(data.email);
        resetForm.setValue('email', data.email);
        setStep('otp');
      } catch (error) {
        const parsed = parseAuthError(error);
        emailForm.setError('root', {
          message: parsed.messageKey ? t(parsed.messageKey) : parsed.rawMessage,
        });
      } finally {
        setIsSending(false);
      }
    };

    const onResetSubmit = async (data: ResetFormRequest) => {
      try {
        setIsResetting(true);
        await authService.resetPassword({
          email: data.email,
          code: data.code,
          newPassword: data.newPassword,
        });
        setStep('success');
      } catch (error) {
        const parsed = parseAuthError(error);
        resetForm.setError('root', {
          message: parsed.messageKey ? t(parsed.messageKey) : parsed.rawMessage,
        });
      } finally {
        setIsResetting(false);
      }
    };

    const handleResend = async () => {
      try {
        setIsSending(true);
        await authService.forgotPassword({ email });
      } catch {
        // ignore
      } finally {
        setIsSending(false);
      }
    };

    const handleStartOver = () => {
      emailForm.reset();
      resetForm.reset();
      setEmail('');
      setStep('email');
    };

    // ---------- SUCCESS ----------
    if (step === 'success') {
      return (
        <div className="flex flex-col items-center text-center gap-6">
          <div className="flex items-center justify-center size-16 rounded-2xl bg-emerald-500/10">
            <ShieldCheck className="size-8 text-emerald-600" />
          </div>
          <div>
            <h1 className="text-2xl font-bold tracking-tight mb-2">Password reset complete</h1>
            <p className="text-muted-foreground text-[15px] leading-relaxed max-w-sm">
              Your password has been changed and all sessions have been revoked. Please sign in with your new password.
            </p>
          </div>
          <Link to="/sign-in" className="w-full">
            <Button className="w-full h-10 bg-gradient-to-r from-violet-600 to-indigo-600 hover:from-violet-700 hover:to-indigo-700 text-white border-0 shadow-lg shadow-violet-500/20">
              Sign in
              <ArrowRight className="size-4 ml-2" />
            </Button>
          </Link>
        </div>
      );
    }

    // ---------- OTP + NEW PASSWORD ----------
    if (step === 'otp') {
      return (
        <form onSubmit={resetForm.handleSubmit(onResetSubmit)} className="flex flex-col gap-6">
          <div>
            <h1 className="text-2xl font-bold tracking-tight mb-1.5">Enter verification code</h1>
            <p className="text-muted-foreground text-[15px] leading-relaxed">
              We sent a 6-digit code to{' '}
              <span className="font-medium text-foreground">{email}</span>.
              Enter it below along with your new password.
            </p>
          </div>

          {resetError && (
            <div className="flex items-start gap-2.5 rounded-lg bg-destructive/10 border border-destructive/20 px-4 py-3 text-sm text-destructive">
              <AlertCircle className="size-4 shrink-0 mt-0.5" />
              <span>{resetError}</span>
            </div>
          )}

          <div className="space-y-4">
            <div className="space-y-2">
              <Label>Verification code</Label>
              <div className="flex justify-center">
                <InputOTP
                  maxLength={6}
                  pattern={REGEXP_ONLY_DIGITS}
                  value={resetForm.watch('code')}
                  onChange={(value) => resetForm.setValue('code', value, { shouldValidate: true })}
                >
                  <InputOTPGroup>
                    <InputOTPSlot index={0} />
                    <InputOTPSlot index={1} />
                    <InputOTPSlot index={2} />
                  </InputOTPGroup>
                  <InputOTPSeparator />
                  <InputOTPGroup>
                    <InputOTPSlot index={3} />
                    <InputOTPSlot index={4} />
                    <InputOTPSlot index={5} />
                  </InputOTPGroup>
                </InputOTP>
              </div>
              {resetForm.formState.errors.code && (
                <p className="text-xs text-destructive text-center">
                  {resolveErrorDisplay(resetForm.formState.errors.code.message, t)}
                </p>
              )}
            </div>

            <div className="space-y-2">
              <Label htmlFor="newPassword">New password</Label>
              <div className="relative">
                <Lock className="absolute left-3 top-1/2 -translate-y-1/2 size-4 text-muted-foreground pointer-events-none" />
                <Input
                  id="newPassword"
                  type={showPassword ? 'text' : 'password'}
                  placeholder="Min. 8 characters"
                  autoComplete="new-password"
                  className={cn('pl-9 pr-10 h-10', resetForm.formState.errors.newPassword && 'border-destructive')}
                  {...resetForm.register('newPassword')}
                />
                <button
                  type="button"
                  onClick={() => setShowPassword(!showPassword)}
                  className="absolute right-3 top-1/2 -translate-y-1/2 text-muted-foreground hover:text-foreground transition-colors"
                  tabIndex={-1}
                >
                  {showPassword ? <EyeOff className="size-4" /> : <Eye className="size-4" />}
                </button>
              </div>
              {resetForm.formState.errors.newPassword && (
                <p className="text-xs text-destructive">
                  {resolveErrorDisplay(resetForm.formState.errors.newPassword.message, t)}
                </p>
              )}
            </div>

            <div className="space-y-2">
              <Label htmlFor="confirmPassword">Confirm new password</Label>
              <div className="relative">
                <Lock className="absolute left-3 top-1/2 -translate-y-1/2 size-4 text-muted-foreground pointer-events-none" />
                <Input
                  id="confirmPassword"
                  type={showConfirm ? 'text' : 'password'}
                  placeholder="Re-enter password"
                  autoComplete="new-password"
                  className={cn('pl-9 pr-10 h-10', resetForm.formState.errors.confirmPassword && 'border-destructive')}
                  {...resetForm.register('confirmPassword')}
                />
                <button
                  type="button"
                  onClick={() => setShowConfirm(!showConfirm)}
                  className="absolute right-3 top-1/2 -translate-y-1/2 text-muted-foreground hover:text-foreground transition-colors"
                  tabIndex={-1}
                >
                  {showConfirm ? <EyeOff className="size-4" /> : <Eye className="size-4" />}
                </button>
              </div>
              {resetForm.formState.errors.confirmPassword && (
                <p className="text-xs text-destructive">
                  {resolveErrorDisplay(resetForm.formState.errors.confirmPassword.message, t)}
                </p>
              )}
            </div>
          </div>

          <Button
            type="submit"
            className="w-full h-10 bg-gradient-to-r from-violet-600 to-indigo-600 hover:from-violet-700 hover:to-indigo-700 text-white border-0 shadow-lg shadow-violet-500/20"
            disabled={isResetting}
          >
            {isResetting ? (
              <>
                <Loader2 className="size-4 mr-2 animate-spin" />
                Resetting password...
              </>
            ) : (
              <>
                Reset password
                <ArrowRight className="size-4 ml-2" />
              </>
            )}
          </Button>

          <div className="flex items-center justify-between text-xs text-muted-foreground">
            <button type="button" onClick={handleResend} disabled={isSending} className="hover:text-foreground transition-colors disabled:opacity-50">
              {isSending ? 'Sending...' : 'Resend code'}
            </button>
            <button type="button" onClick={handleStartOver} className="hover:text-foreground transition-colors">
              Use different email
            </button>
          </div>
        </form>
      );
    }

    // ---------- EMAIL ----------
    return (
      <form onSubmit={emailForm.handleSubmit(onEmailSubmit)} className="flex flex-col gap-7">
        <div>
          <h1 className="text-2xl font-bold tracking-tight mb-1.5">Reset your password</h1>
          <p className="text-muted-foreground text-[15px] leading-relaxed">
            Enter the email address associated with your account and we&apos;ll send
            you a verification code.
          </p>
        </div>

        {forgotError && (
          <div className="flex items-start gap-2.5 rounded-lg bg-destructive/10 border border-destructive/20 px-4 py-3 text-sm text-destructive">
            <AlertCircle className="size-4 shrink-0 mt-0.5" />
            <span>{forgotError}</span>
          </div>
        )}

        <div className="space-y-4">
          <div className="space-y-2">
            <Label htmlFor="email">Email address</Label>
            <div className="relative">
              <Mail className="absolute left-3 top-1/2 -translate-y-1/2 size-4 text-muted-foreground pointer-events-none" />
              <Input
                id="email"
                type="email"
                placeholder="you@company.com"
                autoComplete="email"
                autoFocus
                className={cn(
                  'pl-9 h-10',
                  (emailForm.formState.errors.email || forgotError) && 'border-destructive focus-visible:ring-destructive',
                )}
                {...emailForm.register('email')}
              />
            </div>
            {emailForm.formState.errors.email && (
              <p className="text-xs text-destructive">
                {resolveErrorDisplay(emailForm.formState.errors.email.message, t)}
              </p>
            )}
          </div>
        </div>

        <Button
          type="submit"
          className="w-full h-10 bg-gradient-to-r from-violet-600 to-indigo-600 hover:from-violet-700 hover:to-indigo-700 text-white border-0 shadow-lg shadow-violet-500/20"
          disabled={isSending}
        >
          {isSending ? (
            <>
              <Loader2 className="size-4 mr-2 animate-spin" />
              Sending code...
            </>
          ) : (
            <>
              Send verification code
              <ArrowRight className="size-4 ml-2" />
            </>
          )}
        </Button>

        <Link to="/sign-in">
          <Button variant="ghost" type="button" className="w-full h-10 text-muted-foreground">
            <ArrowLeft className="size-4 mr-2" />
            Back to sign in
          </Button>
        </Link>
      </form>
    );
  };
}
