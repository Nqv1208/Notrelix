"use client"

import * as React from "react"
import Link from "next/link"
import { useForm } from "react-hook-form"
import { zodResolver } from "@hookform/resolvers/zod"
import { Mail, ArrowLeft, ArrowRight, Loader2, CheckCircle2, AlertCircle } from "lucide-react"

import { cn } from "@/lib/utils"
import { routes } from "@/lib/routes"
import { Button } from "@/registry/new-york-v4/ui/button"
import { Input } from "@/registry/new-york-v4/ui/input"
import { Label } from "@/registry/new-york-v4/ui/label"
import { useForgotPassword } from "@/features/auth/hooks/useForgotPassword"
import { ApiError } from "@/lib/api/api-error"
import { forgotPasswordSchema, type ForgotPasswordRequest } from "@/features/auth/schemas/forgot-password.schema"

export function ForgotPasswordForm() {
  const mutation = useForgotPassword()
  const [sentEmail, setSentEmail] = React.useState("")

  const {
    register,
    handleSubmit,
    formState: { errors },
    reset,
    getValues,
  } = useForm<ForgotPasswordRequest>({
    resolver: zodResolver(forgotPasswordSchema),
    defaultValues: { email: "" },
  })

  const onSubmit = (data: ForgotPasswordRequest) => {
    mutation.mutate(
      { email: data.email },
      {
        onSuccess: () => {
          setSentEmail(data.email)
        },
      }
    )
  }

  const handleResend = () => {
    const email = sentEmail || getValues("email")
    if (email) {
      mutation.reset()
      mutation.mutate(
        { email },
        {
          onSuccess: () => {
            setSentEmail(email)
          },
        }
      )
    }
  }

  const handleTryDifferentEmail = () => {
    mutation.reset()
    setSentEmail("")
    reset({ email: "" })
  }

  const serverError = React.useMemo(() => {
    if (!mutation.error) return null
    if (mutation.error instanceof ApiError) {
      const payload = mutation.error.data as { message?: string; detail?: string } | undefined
      return payload?.message ?? payload?.detail ?? mutation.error.message
    }
    return mutation.error.message
  }, [mutation.error])

  if (sentEmail && mutation.isSuccess) {
    return (
      <div className="flex flex-col items-center text-center gap-6">
        <div className="flex items-center justify-center size-16 rounded-2xl bg-emerald-500/10">
          <CheckCircle2 className="size-8 text-emerald-600" />
        </div>

        <div>
          <h1 className="text-2xl font-bold tracking-tight mb-2">Check your email</h1>
          <p className="text-muted-foreground text-[15px] leading-relaxed max-w-sm">
            We&apos;ve sent a password reset link to{" "}
            <span className="font-medium text-foreground">{sentEmail}</span>.
            Click the link in the email to reset your password.
          </p>
        </div>

        <div className="w-full space-y-3">
          <Button
            variant="outline"
            className="w-full h-10"
            onClick={handleTryDifferentEmail}
          >
            Try a different email
          </Button>

          <Link href={routes.auth.signIn} className="block">
            <Button
              variant="ghost"
              className="w-full h-10 text-muted-foreground"
            >
              <ArrowLeft className="size-4 mr-2" />
              Back to sign in
            </Button>
          </Link>
        </div>

        <p className="text-xs text-muted-foreground">
          Didn&apos;t receive an email? Check your spam folder or{" "}
          <button
            type="button"
            onClick={handleResend}
            disabled={mutation.isPending}
            className="text-foreground underline-offset-4 hover:underline disabled:opacity-50"
          >
            {mutation.isPending ? "sending..." : "resend"}
          </button>
          .
        </p>
      </div>
    )
  }

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="flex flex-col gap-7">
      <div>
        <h1 className="text-2xl font-bold tracking-tight mb-1.5">
          Reset your password
        </h1>
        <p className="text-muted-foreground text-[15px] leading-relaxed">
          Enter the email address associated with your account and we&apos;ll send
          you a link to reset your password.
        </p>
      </div>

      {serverError && (
        <div className="flex items-start gap-2.5 rounded-lg bg-destructive/10 border border-destructive/20 px-4 py-3 text-sm text-destructive">
          <AlertCircle className="size-4 shrink-0 mt-0.5" />
          <span>{serverError}</span>
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
                (errors.email || serverError) && "border-destructive focus-visible:ring-destructive"
              )}
              {...register("email")}
            />
          </div>
          {errors.email && (
            <p className="text-xs text-destructive">{errors.email.message}</p>
          )}
        </div>
      </div>

      <Button
        type="submit"
        className="w-full h-10 bg-gradient-to-r from-violet-600 to-indigo-600 hover:from-violet-700 hover:to-indigo-700 text-white border-0 shadow-lg shadow-violet-500/20"
        disabled={mutation.isPending}
      >
        {mutation.isPending ? (
          <>
            <Loader2 className="size-4 mr-2 animate-spin" />
            Sending link...
          </>
        ) : (
          <>
            Send reset link
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
