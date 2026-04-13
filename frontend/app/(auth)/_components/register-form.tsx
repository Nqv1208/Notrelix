"use client"

import Link from "next/link"
import { useState } from "react"
import { useForm } from "react-hook-form"
import { zodResolver } from "@hookform/resolvers/zod"
import { useTranslations } from "next-intl"
import { Mail, Lock, Eye, EyeOff, User, ArrowRight, Loader2, Check } from "lucide-react"

import { cn } from "@/lib/utils"
import { routes } from "@/lib/routes"
import { Button } from "@/registry/new-york-v4/ui/button"
import { Input } from "@/registry/new-york-v4/ui/input"
import { Label } from "@/registry/new-york-v4/ui/label"
import { Checkbox } from "@/registry/new-york-v4/ui/checkbox"
import { useRegister } from "@/features/auth/hooks/useRegister"
import { RegisterRequest, registerSchema } from "@/features/auth/schemas/register.schemas"
import { parseAuthError } from "@/features/auth/utils/parse-auth-error"
import { AuthErrorKey, AUTH_ERROR_KEYS, isAuthErrorKey } from "@/features/auth/i18n/auth-error-keys"

function getPasswordStrength(password: string): { score: number; label: string; color: string } {
  let score = 0
  if (password.length >= 8) score++
  if (/[A-Z]/.test(password)) score++
  if (/[a-z]/.test(password)) score++
  if (/[0-9]/.test(password)) score++
  if (/[^A-Za-z0-9]/.test(password)) score++

  if (score <= 1) return { score, label: "Weak", color: "bg-red-500" }
  if (score <= 2) return { score, label: "Fair", color: "bg-orange-500" }
  if (score <= 3) return { score, label: "Good", color: "bg-amber-500" }
  if (score <= 4) return { score, label: "Strong", color: "bg-emerald-500" }
  return { score, label: "Very strong", color: "bg-emerald-600" }
}

export function RegisterForm({
  className,
  ...props
}: React.ComponentPropsWithoutRef<"form">) {
  const t = useTranslations()
  const registerMutation = useRegister()
  const [serverErrorKey, setServerErrorKey] = useState<AuthErrorKey | null>(null)
  const [showPassword, setShowPassword] = useState(false)
  const [showConfirm, setShowConfirm] = useState(false)

  const {
    register,
    handleSubmit,
    setError,
    watch,
    formState: { errors },
  } = useForm<RegisterRequest>({
    resolver: zodResolver(registerSchema),
  })

  const passwordValue = watch("password") ?? ""
  const strength = getPasswordStrength(passwordValue)

  const onSubmit = (data: RegisterRequest) => {
    setServerErrorKey(null)
    const payload = {
      name: `${data.firstName} ${data.lastName}`,
      email: data.email,
      password: data.password,
    }

    registerMutation.mutate(payload, {
      onError: (error) => {
        const parsed = parseAuthError(error)
        if (parsed.fieldErrors.email)
          setError("email", { type: "server", message: parsed.fieldErrors.email })
        if (parsed.fieldErrors.password)
          setError("password", { type: "server", message: parsed.fieldErrors.password })
        setServerErrorKey(parsed.messageKey)
      },
    })
  }

  return (
    <form
      onSubmit={handleSubmit(onSubmit)}
      className={cn("flex flex-col gap-6", className)}
      {...props}
    >
      {/* Header */}
      <div>
        <h1 className="text-2xl font-bold tracking-tight mb-1.5">Create your account</h1>
        <p className="text-muted-foreground text-[15px]">
          Get started with Craftboard for free
        </p>
      </div>

      {/* Social buttons */}
      <div className="grid grid-cols-2 gap-3">
        <Button variant="outline" type="button" className="h-10">
          <svg className="size-4 mr-2" viewBox="0 0 24 24">
            <path d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92c-.26 1.37-1.04 2.53-2.21 3.31v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.09z" fill="#4285F4" />
            <path d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z" fill="#34A853" />
            <path d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.07H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.93l2.85-2.22.81-.62z" fill="#FBBC05" />
            <path d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.07l3.66 2.84c.87-2.6 3.3-4.53 6.16-4.53z" fill="#EA4335" />
          </svg>
          Google
        </Button>
        <Button variant="outline" type="button" className="h-10">
          <svg className="size-4 mr-2" fill="currentColor" viewBox="0 0 24 24">
            <path d="M12 .297c-6.63 0-12 5.373-12 12 0 5.303 3.438 9.8 8.205 11.385.6.113.82-.258.82-.577 0-.285-.01-1.04-.015-2.04-3.338.724-4.042-1.61-4.042-1.61C4.422 18.07 3.633 17.7 3.633 17.7c-1.087-.744.084-.729.084-.729 1.205.084 1.838 1.236 1.838 1.236 1.07 1.835 2.809 1.305 3.495.998.108-.776.417-1.305.76-1.605-2.665-.3-5.466-1.332-5.466-5.93 0-1.31.465-2.38 1.235-3.22-.135-.303-.54-1.523.105-3.176 0 0 1.005-.322 3.3 1.23.96-.267 1.98-.399 3-.405 1.02.006 2.04.138 3 .405 2.28-1.552 3.285-1.23 3.285-1.23.645 1.653.24 2.873.12 3.176.765.84 1.23 1.91 1.23 3.22 0 4.61-2.805 5.625-5.475 5.92.42.36.81 1.096.81 2.22 0 1.606-.015 2.896-.015 3.286 0 .315.21.69.825.57C20.565 22.092 24 17.592 24 12.297c0-6.627-5.373-12-12-12" />
          </svg>
          GitHub
        </Button>
      </div>

      {/* Divider */}
      <div className="relative">
        <div className="absolute inset-0 flex items-center">
          <div className="w-full border-t" />
        </div>
        <div className="relative flex justify-center">
          <span className="bg-background px-3 text-xs text-muted-foreground uppercase tracking-wider">
            or continue with email
          </span>
        </div>
      </div>

      {/* Server error */}
      {serverErrorKey && (
        <div className="rounded-lg bg-destructive/10 border border-destructive/20 px-4 py-3 text-sm text-destructive">
          {t(serverErrorKey)}
        </div>
      )}

      {/* Fields */}
      <div className="space-y-4">
        {/* Name row */}
        <div className="grid grid-cols-2 gap-3">
          <div className="space-y-2">
            <Label htmlFor="firstName">First name</Label>
            <div className="relative">
              <User className="absolute left-3 top-1/2 -translate-y-1/2 size-4 text-muted-foreground pointer-events-none" />
              <Input
                id="firstName"
                type="text"
                placeholder="John"
                autoComplete="given-name"
                className={cn("pl-9 h-10", errors.firstName && "border-destructive")}
                {...register("firstName")}
              />
            </div>
            {errors.firstName && (
              <p className="text-xs text-destructive">{toMsg(errors.firstName.message, t)}</p>
            )}
          </div>
          <div className="space-y-2">
            <Label htmlFor="lastName">Last name</Label>
            <Input
              id="lastName"
              type="text"
              placeholder="Doe"
              autoComplete="family-name"
              className={cn("h-10", errors.lastName && "border-destructive")}
              {...register("lastName")}
            />
            {errors.lastName && (
              <p className="text-xs text-destructive">{toMsg(errors.lastName.message, t)}</p>
            )}
          </div>
        </div>

        {/* Email */}
        <div className="space-y-2">
          <Label htmlFor="email">Work email</Label>
          <div className="relative">
            <Mail className="absolute left-3 top-1/2 -translate-y-1/2 size-4 text-muted-foreground pointer-events-none" />
            <Input
              id="email"
              type="email"
              placeholder="you@company.com"
              autoComplete="email"
              className={cn("pl-9 h-10", errors.email && "border-destructive")}
              {...register("email")}
            />
          </div>
          {errors.email && (
            <p className="text-xs text-destructive">{toMsg(errors.email.message, t)}</p>
          )}
        </div>

        {/* Password */}
        <div className="space-y-2">
          <Label htmlFor="password">Password</Label>
          <div className="relative">
            <Lock className="absolute left-3 top-1/2 -translate-y-1/2 size-4 text-muted-foreground pointer-events-none" />
            <Input
              id="password"
              type={showPassword ? "text" : "password"}
              placeholder="Min. 8 characters"
              autoComplete="new-password"
              className={cn("pl-9 pr-10 h-10", errors.password && "border-destructive")}
              {...register("password")}
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
          {errors.password && (
            <p className="text-xs text-destructive">{toMsg(errors.password.message, t)}</p>
          )}
          {/* Strength bar */}
          {passwordValue.length > 0 && (
            <div className="flex items-center gap-2">
              <div className="flex-1 flex gap-1">
                {[1, 2, 3, 4, 5].map((i) => (
                  <div
                    key={i}
                    className={cn(
                      "h-1 flex-1 rounded-full transition-colors",
                      i <= strength.score ? strength.color : "bg-muted"
                    )}
                  />
                ))}
              </div>
              <span className="text-[11px] text-muted-foreground shrink-0">
                {strength.label}
              </span>
            </div>
          )}
        </div>

        {/* Confirm Password */}
        <div className="space-y-2">
          <Label htmlFor="confirmPassword">Confirm password</Label>
          <div className="relative">
            <Lock className="absolute left-3 top-1/2 -translate-y-1/2 size-4 text-muted-foreground pointer-events-none" />
            <Input
              id="confirmPassword"
              type={showConfirm ? "text" : "password"}
              placeholder="Re-enter your password"
              autoComplete="new-password"
              className={cn("pl-9 pr-10 h-10", errors.confirmPassword && "border-destructive")}
              {...register("confirmPassword")}
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
          {errors.confirmPassword && (
            <p className="text-xs text-destructive">{toMsg(errors.confirmPassword.message, t)}</p>
          )}
        </div>

        {/* Terms */}
        <div className="flex items-start gap-2.5">
          <Checkbox id="terms" className="mt-0.5" />
          <Label htmlFor="terms" className="text-sm font-normal cursor-pointer leading-relaxed text-muted-foreground">
            I agree to the{" "}
            <Link href={routes.auth.terms} className="text-foreground underline-offset-4 hover:underline">
              Terms of Service
            </Link>{" "}
            and{" "}
            <Link href={routes.auth.privacy} className="text-foreground underline-offset-4 hover:underline">
              Privacy Policy
            </Link>
          </Label>
        </div>
      </div>

      {/* Submit */}
      <Button
        type="submit"
        className="w-full h-10 bg-gradient-to-r from-violet-600 to-indigo-600 hover:from-violet-700 hover:to-indigo-700 text-white border-0 shadow-lg shadow-violet-500/20"
        disabled={registerMutation.isPending}
      >
        {registerMutation.isPending ? (
          <>
            <Loader2 className="size-4 mr-2 animate-spin" />
            Creating account...
          </>
        ) : (
          <>
            Create account
            <ArrowRight className="size-4 ml-2" />
          </>
        )}
      </Button>

      {/* Footer */}
      <p className="text-center text-sm text-muted-foreground">
        Already have an account?{" "}
        <Link
          href={routes.auth.signIn}
          className="font-medium text-foreground hover:text-primary transition-colors underline-offset-4 hover:underline"
        >
          Sign in
        </Link>
      </p>
    </form>
  )
}

function toMsg(
  message: string | undefined,
  t: ReturnType<typeof useTranslations>
): string {
  if (!message) return t(AUTH_ERROR_KEYS.SERVER_GENERIC)
  if (isAuthErrorKey(message)) return t(message)
  return message
}
