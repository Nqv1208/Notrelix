"use client"

import * as React from "react"
import Link from "next/link"
import { useForm } from "react-hook-form"
import { zodResolver } from "@hookform/resolvers/zod"
import { useTranslations } from "next-intl"
import {
  Mail, ArrowLeft, ArrowRight, Loader2, CheckCircle2,
  AlertCircle, Lock, Eye, EyeOff, ShieldCheck,
} from "lucide-react"
import { REGEXP_ONLY_DIGITS } from "input-otp"

import { cn } from "@/lib/utils"
import { routes } from "@/lib/routes"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import {
  InputOTP,
  InputOTPGroup,
  InputOTPSlot,
  InputOTPSeparator,
} from "@/components/ui/input-otp"
import { useForgotPassword } from "@/features/auth/hooks/useForgotPassword"
import { useResetPassword } from "@/features/auth/hooks/useResetPassword"
import { forgotPasswordSchema, type ForgotPasswordRequest } from "@/features/auth/schemas/forgot-password.schema"
import { resetPasswordSchema, type ResetPasswordFormData } from "@/features/auth/schemas/reset-password.schema"
import { parseAuthError } from "@/features/auth/utils/parse-auth-error"
import { resolveErrorDisplay } from "@/features/auth/utils/resolve-error-display"
import { ApiError } from "@/lib/api/api-error"

type Step = "email" | "otp" | "success"

export function ForgotPasswordForm() {
  const t = useTranslations()
  const forgotMutation = useForgotPassword()
  const resetMutation = useResetPassword()
  const [step, setStep] = React.useState<Step>("email")
  const [email, setEmail] = React.useState("")

  const emailForm = useForm<ForgotPasswordRequest>({
    resolver: zodResolver(forgotPasswordSchema),
    defaultValues: { email: "" },
  })

  const resetForm = useForm<ResetPasswordFormData>({
    resolver: zodResolver(resetPasswordSchema),
    defaultValues: { email: "", code: "", newPassword: "", confirmPassword: "" },
  })

  const [showPassword, setShowPassword] = React.useState(false)
  const [showConfirm, setShowConfirm] = React.useState(false)

  const forgotError = React.useMemo(() => {
    if (!forgotMutation.error) return null
    const parsed = parseAuthError(forgotMutation.error)
    return parsed.messageKey ? t(parsed.messageKey) : parsed.rawMessage
  }, [forgotMutation.error, t])

  const resetError = React.useMemo(() => {
    if (!resetMutation.error) return null
    const parsed = parseAuthError(resetMutation.error)
    return parsed.messageKey ? t(parsed.messageKey) : parsed.rawMessage
  }, [resetMutation.error, t])

  const onEmailSubmit = (data: ForgotPasswordRequest) => {
    forgotMutation.mutate(data, {
      onSuccess: () => {
        setEmail(data.email)
        resetForm.setValue("email", data.email)
        setStep("otp")
      },
    })
  }

  const onResetSubmit = (data: ResetPasswordFormData) => {
    resetMutation.mutate(
      { email: data.email, code: data.code, newPassword: data.newPassword },
      { onSuccess: () => setStep("success") }
    )
  }

  const handleResend = () => {
    forgotMutation.reset()
    forgotMutation.mutate({ email })
  }

  const handleStartOver = () => {
    forgotMutation.reset()
    resetMutation.reset()
    emailForm.reset()
    resetForm.reset()
    setEmail("")
    setStep("email")
  }

  // ---------- SUCCESS ----------
  if (step === "success") {
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
        <Link href={routes.auth.signIn} className="w-full">
          <Button className="w-full h-10 bg-gradient-to-r from-violet-600 to-indigo-600 hover:from-violet-700 hover:to-indigo-700 text-white border-0 shadow-lg shadow-violet-500/20">
            Sign in
            <ArrowRight className="size-4 ml-2" />
          </Button>
        </Link>
      </div>
    )
  }

  // ---------- OTP + NEW PASSWORD ----------
  if (step === "otp") {
    return (
      <form onSubmit={resetForm.handleSubmit(onResetSubmit)} className="flex flex-col gap-6">
        <div>
          <h1 className="text-2xl font-bold tracking-tight mb-1.5">Enter verification code</h1>
          <p className="text-muted-foreground text-[15px] leading-relaxed">
            We sent a 6-digit code to{" "}
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
                value={resetForm.watch("code")}
                onChange={(value) => resetForm.setValue("code", value, { shouldValidate: true })}
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
                type={showPassword ? "text" : "password"}
                placeholder="Min. 8 characters"
                autoComplete="new-password"
                className={cn("pl-9 pr-10 h-10", resetForm.formState.errors.newPassword && "border-destructive")}
                {...resetForm.register("newPassword")}
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
                type={showConfirm ? "text" : "password"}
                placeholder="Re-enter password"
                autoComplete="new-password"
                className={cn("pl-9 pr-10 h-10", resetForm.formState.errors.confirmPassword && "border-destructive")}
                {...resetForm.register("confirmPassword")}
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
          disabled={resetMutation.isPending}
        >
          {resetMutation.isPending ? (
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
          <button type="button" onClick={handleResend} disabled={forgotMutation.isPending} className="hover:text-foreground transition-colors disabled:opacity-50">
            {forgotMutation.isPending ? "Sending..." : "Resend code"}
          </button>
          <button type="button" onClick={handleStartOver} className="hover:text-foreground transition-colors">
            Use different email
          </button>
        </div>
      </form>
    )
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
                "pl-9 h-10",
                (emailForm.formState.errors.email || forgotError) && "border-destructive focus-visible:ring-destructive"
              )}
              {...emailForm.register("email")}
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
        disabled={forgotMutation.isPending}
      >
        {forgotMutation.isPending ? (
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

      <Link href={routes.auth.signIn}>
        <Button variant="ghost" type="button" className="w-full h-10 text-muted-foreground">
          <ArrowLeft className="size-4 mr-2" />
          Back to sign in
        </Button>
      </Link>
    </form>
  )
}
