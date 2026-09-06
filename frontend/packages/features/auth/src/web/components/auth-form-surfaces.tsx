import { useState } from "react";
import {
  AlertCircle,
  ArrowLeft,
  ArrowRight,
  Eye,
  EyeOff,
  Loader2,
  Lock,
  Mail,
  ShieldCheck,
  User,
} from "lucide-react";
import { Button, Checkbox, Input, Label } from "@notrelix/ui-web";

export interface AuthSubmitData {
  email: string;
  password: string;
}

export interface RegisterSubmitData {
  firstName: string;
  lastName: string;
  email: string;
  password: string;
}

export interface AuthLoginFormSurfaceProps {
  status?: "idle" | "pending";
  serverError?: string | null;
  fieldError?: "email" | "password" | null;
  onSubmit: (data: AuthSubmitData) => void;
  onGoogleSignIn?: () => void;
  onGithubSignIn?: () => void;
  onForgotPassword?: () => void;
  onRegister?: () => void;
}

export function AuthLoginFormSurface({
  status = "idle",
  serverError = null,
  fieldError = null,
  onSubmit,
  onGoogleSignIn,
  onGithubSignIn,
  onForgotPassword,
  onRegister,
}: AuthLoginFormSurfaceProps) {
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [showPassword, setShowPassword] = useState(false);
  const isPending = status === "pending";

  return (
    <form
      className="flex flex-col gap-7"
      onSubmit={(event) => {
        event.preventDefault();
        onSubmit({ email, password });
      }}
    >
      <div>
        <h1 className="text-2xl font-bold tracking-tight mb-1.5">
          Welcome back
        </h1>
        <p className="text-muted-foreground text-[15px]">
          Sign in to your Notrelix workspace
        </p>
      </div>

      <div className="grid grid-cols-2 gap-3">
        <Button
          variant="outline"
          type="button"
          className="h-10 text-sm font-medium"
          onClick={onGoogleSignIn}
        >
          <svg className="size-4 mr-2 shrink-0" viewBox="0 0 24 24">
            <path
              d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92c-.26 1.37-1.04 2.53-2.21 3.31v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.09z"
              fill="#4285F4"
            />
            <path
              d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z"
              fill="#34A853"
            />
            <path
              d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.07H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.93l2.85-2.22.81-.62z"
              fill="#FBBC05"
            />
            <path
              d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.07l3.66 2.84c.87-2.6 3.3-4.53 6.16-4.53z"
              fill="#EA4335"
            />
          </svg>
          Google
        </Button>
        <Button
          variant="outline"
          type="button"
          className="h-10 text-sm font-medium"
          onClick={onGithubSignIn}
        >
          <svg
            className="size-4 mr-2 shrink-0"
            fill="currentColor"
            viewBox="0 0 24 24"
          >
            <path d="M12 .297c-6.63 0-12 5.373-12 12 0 5.303 3.438 9.8 8.205 11.385.6.113.82-.258.82-.577 0-.285-.01-1.04-.015-2.04-3.338.724-4.042-1.61-4.042-1.61C4.422 18.07 3.633 17.7 3.633 17.7c-1.087-.744.084-.729.084-.729 1.205.084 1.838 1.236 1.838 1.236 1.07 1.835 2.809 1.305 3.495.998.108-.776.417-1.305.76-1.605-2.665-.3-5.466-1.332-5.466-5.93 0-1.31.465-2.38 1.235-3.22-.135-.303-.54-1.523.105-3.176 0 0 1.005-.322 3.3 1.23.96-.267 1.98-.399 3-.405 1.02.006 2.04.138 3 .405 2.28-1.552 3.285-1.23 3.285-1.23.645 1.653.24 2.873.12 3.176.765.84 1.23 1.91 1.23 3.22 0 4.61-2.805 5.625-5.475 5.92.42.36.81 1.096.81 2.22 0 1.606-.015 2.896-.015 3.286 0 .315.21.69.825.57C20.565 22.092 24 17.592 24 12.297c0-6.627-5.373-12-12-12" />
          </svg>
          GitHub
        </Button>
      </div>

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

      {serverError ? (
        <div className="flex items-start gap-2.5 rounded-lg bg-destructive/10 border border-destructive/20 px-4 py-3 text-sm text-foreground">
          <AlertCircle className="size-4 shrink-0 mt-0.5 text-destructive" />
          <span>{serverError}</span>
        </div>
      ) : null}

      <div className="space-y-4">
        <div className="space-y-2">
          <Label htmlFor="email">Email</Label>
          <Input
            id="email"
            type="email"
            placeholder="you@company.com"
            autoComplete="email"
            value={email}
            aria-invalid={fieldError === "email"}
            onChange={(event) => setEmail(event.target.value)}
          />
        </div>

        <div className="space-y-2">
          <div className="flex items-center justify-between">
            <Label htmlFor="password">Password</Label>
            <button
              type="button"
              onClick={onForgotPassword}
              className="text-xs text-muted-foreground hover:text-foreground transition-colors"
            >
              Forgot password?
            </button>
          </div>
          <div className="relative">
            <Input
              id="password"
              type={showPassword ? "text" : "password"}
              placeholder="Enter your password"
              autoComplete="current-password"
              value={password}
              aria-invalid={fieldError === "password"}
              onChange={(event) => setPassword(event.target.value)}
              className="pr-10"
            />
            <button
              type="button"
              onClick={() => setShowPassword((value) => !value)}
              className="absolute right-2 top-1/2 -translate-y-1/2 text-muted-foreground hover:text-foreground transition-colors p-1"
              aria-label={showPassword ? "Hide password" : "Show password"}
            >
              {showPassword ? (
                <EyeOff className="size-4" />
              ) : (
                <Eye className="size-4" />
              )}
            </button>
          </div>
        </div>

        <div className="flex items-center gap-2">
          <Checkbox id="remember" />
          <Label
            htmlFor="remember"
            className="text-sm font-normal cursor-pointer"
          >
            Remember me
          </Label>
        </div>
      </div>

      <Button
        type="submit"
        className="w-full h-10 bg-[linear-gradient(135deg,#FF1E56_0%,#FC744C_35%,#1E90FF_100%)] hover:opacity-90 text-white font-medium shadow-md shadow-red-500/10 transition-all border-0"
        disabled={isPending}
      >
        {isPending ? (
          <>
            <Loader2 className="size-4 mr-2 animate-spin" />
            Signing in...
          </>
        ) : (
          <>
            Sign in
            <ArrowRight className="size-4 ml-2" />
          </>
        )}
      </Button>

      <p className="text-center text-sm text-muted-foreground">
        Don&apos;t have an account?{" "}
        <button
          type="button"
          onClick={onRegister}
          className="font-medium text-foreground hover:text-primary transition-colors underline-offset-4 hover:underline"
        >
          Create one
        </button>
      </p>
    </form>
  );
}

export interface AuthRegisterFormSurfaceProps {
  status?: "idle" | "pending";
  serverError?: string | null;
  onSubmit: (data: RegisterSubmitData) => void;
  onGoogleSignIn?: () => void;
  onGithubSignIn?: () => void;
  onSignIn?: () => void;
}

export function AuthRegisterFormSurface({
  status = "idle",
  serverError = null,
  onSubmit,
  onGoogleSignIn,
  onGithubSignIn,
  onSignIn,
}: AuthRegisterFormSurfaceProps) {
  const [firstName, setFirstName] = useState("");
  const [lastName, setLastName] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [showPassword, setShowPassword] = useState(false);
  const isPending = status === "pending";

  return (
    <form
      className="flex flex-col gap-6"
      onSubmit={(event) => {
        event.preventDefault();
        onSubmit({ firstName, lastName, email, password });
      }}
    >
      <div>
        <h1 className="text-2xl font-bold tracking-tight mb-1.5">
          Create your account
        </h1>
        <p className="text-muted-foreground text-[15px]">
          Get started with Notrelix for free
        </p>
      </div>

      <div className="grid grid-cols-2 gap-3">
        <Button
          variant="outline"
          type="button"
          className="h-10 text-sm font-medium"
          onClick={onGoogleSignIn}
        >
          <svg className="size-4 mr-2 shrink-0" viewBox="0 0 24 24">
            <path
              d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92c-.26 1.37-1.04 2.53-2.21 3.31v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.09z"
              fill="#4285F4"
            />
            <path
              d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z"
              fill="#34A853"
            />
            <path
              d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.07H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.93l2.85-2.22.81-.62z"
              fill="#FBBC05"
            />
            <path
              d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.07l3.66 2.84c.87-2.6 3.3-4.53 6.16-4.53z"
              fill="#EA4335"
            />
          </svg>
          Google
        </Button>
        <Button
          variant="outline"
          type="button"
          className="h-10 text-sm font-medium"
          onClick={onGithubSignIn}
        >
          <svg
            className="size-4 mr-2 shrink-0"
            fill="currentColor"
            viewBox="0 0 24 24"
          >
            <path d="M12 .297c-6.63 0-12 5.373-12 12 0 5.303 3.438 9.8 8.205 11.385.6.113.82-.258.82-.577 0-.285-.01-1.04-.015-2.04-3.338.724-4.042-1.61-4.042-1.61C4.422 18.07 3.633 17.7 3.633 17.7c-1.087-.744.084-.729.084-.729 1.205.084 1.838 1.236 1.838 1.236 1.07 1.835 2.809 1.305 3.495.998.108-.776.417-1.305.76-1.605-2.665-.3-5.466-1.332-5.466-5.93 0-1.31.465-2.38 1.235-3.22-.135-.303-.54-1.523.105-3.176 0 0 1.005-.322 3.3 1.23.96-.267 1.98-.399 3-.405 1.02.006 2.04.138 3 .405 2.28-1.552 3.285-1.23 3.285-1.23.645 1.653.24 2.873.12 3.176.765.84 1.23 1.91 1.23 3.22 0 4.61-2.805 5.625-5.475 5.92.42.36.81 1.096.81 2.22 0 1.606-.015 2.896-.015 3.286 0 .315.21.69.825.57C20.565 22.092 24 17.592 24 12.297c0-6.627-5.373-12-12-12" />
          </svg>
          GitHub
        </Button>
      </div>

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

      {serverError ? (
        <div className="flex items-start gap-2.5 rounded-lg bg-destructive/10 border border-destructive/20 px-4 py-3 text-sm text-foreground">
          <AlertCircle className="size-4 shrink-0 mt-0.5 text-destructive" />
          <span>{serverError}</span>
        </div>
      ) : null}

      <div className="space-y-4">
        <div className="grid grid-cols-2 gap-3">
          <div className="space-y-2">
            <Label htmlFor="firstName">First name</Label>
            <div className="relative">
              <User className="pointer-events-none absolute left-2.5 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" />
              <Input
                id="firstName"
                type="text"
                placeholder="John"
                autoComplete="given-name"
                value={firstName}
                onChange={(event) => setFirstName(event.target.value)}
                className="pl-9"
              />
            </div>
          </div>
          <div className="space-y-2">
            <Label htmlFor="lastName">Last name</Label>
            <div className="relative">
              <User className="pointer-events-none absolute left-2.5 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" />
              <Input
                id="lastName"
                type="text"
                placeholder="Doe"
                autoComplete="family-name"
                value={lastName}
                onChange={(event) => setLastName(event.target.value)}
                className="pl-9"
              />
            </div>
          </div>
        </div>

        <div className="space-y-2">
          <Label htmlFor="email">Work email</Label>
          <div className="relative">
            <Mail className="pointer-events-none absolute left-2.5 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" />
            <Input
              id="email"
              type="email"
              placeholder="you@company.com"
              autoComplete="email"
              value={email}
              onChange={(event) => setEmail(event.target.value)}
              className="pl-9"
            />
          </div>
        </div>

        <div className="space-y-2">
          <Label htmlFor="password">Password</Label>
          <div className="relative">
            <Lock className="pointer-events-none absolute left-2.5 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" />
            <Input
              id="password"
              type={showPassword ? "text" : "password"}
              placeholder="Min. 8 characters"
              autoComplete="new-password"
              value={password}
              onChange={(event) => setPassword(event.target.value)}
              className="pl-9 pr-10"
            />
            <button
              type="button"
              onClick={() => setShowPassword((value) => !value)}
              className="absolute right-2 top-1/2 -translate-y-1/2 text-muted-foreground hover:text-foreground transition-colors p-1"
              aria-label={showPassword ? "Hide password" : "Show password"}
            >
              {showPassword ? (
                <EyeOff className="size-4" />
              ) : (
                <Eye className="size-4" />
              )}
            </button>
          </div>
        </div>

        <div className="flex items-start gap-2.5">
          <Checkbox id="terms" className="mt-0.5" />
          <Label
            htmlFor="terms"
            className="text-sm font-normal cursor-pointer leading-relaxed text-muted-foreground"
          >
            I agree to the{" "}
            <a
              href="/legal/terms"
              className="text-foreground underline-offset-4 hover:underline"
            >
              Terms of Service
            </a>{" "}
            and{" "}
            <a
              href="/legal/privacy"
              className="text-foreground underline-offset-4 hover:underline"
            >
              Privacy Policy
            </a>
          </Label>
        </div>
      </div>

      <Button
        type="submit"
        className="w-full h-10 bg-[linear-gradient(135deg,#FF1E56_0%,#FC744C_35%,#1E90FF_100%)] hover:opacity-90 text-white font-medium shadow-md shadow-red-500/10 transition-all border-0"
        disabled={isPending}
      >
        {isPending ? (
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

      <p className="text-center text-sm text-muted-foreground">
        Already have an account?{" "}
        <button
          type="button"
          onClick={onSignIn}
          className="font-medium text-foreground hover:text-primary transition-colors underline-offset-4 hover:underline"
        >
          Sign in
        </button>
      </p>
    </form>
  );
}

export function AuthForgotPasswordFormSurface({
  step = "email",
  email = "",
  status = "idle",
  serverError = null,
  onSendCode,
  onResendCode,
  onResetPassword,
  onStartOver,
  onBackToSignIn,
  onSignIn,
}: {
  step?: "email" | "otp" | "success";
  email?: string;
  status?: "idle" | "pending";
  serverError?: string | null;
  onSendCode: (email: string) => void;
  onResendCode?: () => void;
  onResetPassword: (data: { code: string; newPassword: string }) => void;
  onStartOver?: () => void;
  onBackToSignIn?: () => void;
  onSignIn?: () => void;
}) {
  const [draftEmail, setDraftEmail] = useState(email);
  const [code, setCode] = useState("");
  const [newPassword, setNewPassword] = useState("");
  const [showPassword, setShowPassword] = useState(false);
  const isPending = status === "pending";

  if (step === "success") {
    return (
      <div className="flex flex-col items-center text-center gap-6">
        <div className="flex items-center justify-center size-16 rounded-2xl bg-emerald-500/10">
          <ShieldCheck className="size-8 text-emerald-600" />
        </div>
        <div>
          <h1 className="text-2xl font-bold tracking-tight mb-2">
            Password reset complete
          </h1>
          <p className="text-muted-foreground text-[15px] leading-relaxed max-w-sm">
            Your password has been changed and all sessions have been revoked.
            Please sign in with your new password.
          </p>
        </div>
        <Button
          type="button"
          onClick={onSignIn}
          className="w-full h-10 bg-[linear-gradient(135deg,#FF1E56_0%,#FC744C_35%,#1E90FF_100%)] hover:opacity-90 text-white border-0 shadow-md"
        >
          Sign in
          <ArrowRight className="size-4 ml-2" />
        </Button>
      </div>
    );
  }

  if (step === "otp") {
    return (
      <form
        className="flex flex-col gap-6"
        onSubmit={(event) => {
          event.preventDefault();
          onResetPassword({ code, newPassword });
        }}
      >
        <div>
          <h1 className="text-2xl font-bold tracking-tight mb-1.5">
            Enter verification code
          </h1>
          <p className="text-muted-foreground text-[15px] leading-relaxed">
            We sent a 6-digit code to{" "}
            <span className="font-medium text-foreground">{email}</span>. Enter
            it below along with your new password.
          </p>
        </div>

        {serverError ? (
          <div className="flex items-start gap-2.5 rounded-lg bg-destructive/10 border border-destructive/20 px-4 py-3 text-sm text-destructive">
            <AlertCircle className="size-4 shrink-0 mt-0.5" />
            <span>{serverError}</span>
          </div>
        ) : null}

        <div className="space-y-4">
          <div className="space-y-2">
            <Label htmlFor="verification-code">Verification code</Label>
            <Input
              id="verification-code"
              inputMode="numeric"
              maxLength={6}
              placeholder="000000"
              value={code}
              onChange={(event) => setCode(event.target.value)}
              className="text-center text-lg tracking-widest"
            />
          </div>

          <div className="space-y-2">
            <Label htmlFor="newPassword">New password</Label>
            <div className="relative">
              <Lock className="pointer-events-none absolute left-2.5 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" />
              <Input
                id="newPassword"
                type={showPassword ? "text" : "password"}
                placeholder="Min. 8 characters"
                autoComplete="new-password"
                value={newPassword}
                onChange={(event) => setNewPassword(event.target.value)}
                className="pl-9 pr-10"
              />
              <button
                type="button"
                onClick={() => setShowPassword((value) => !value)}
                className="absolute right-2 top-1/2 -translate-y-1/2 text-muted-foreground hover:text-foreground transition-colors p-1"
                aria-label={showPassword ? "Hide password" : "Show password"}
              >
                {showPassword ? (
                  <EyeOff className="size-4" />
                ) : (
                  <Eye className="size-4" />
                )}
              </button>
            </div>
          </div>
        </div>

        <Button
          type="submit"
          className="w-full h-10 bg-[linear-gradient(135deg,#FF1E56_0%,#FC744C_35%,#1E90FF_100%)] hover:opacity-90 text-white font-medium border-0 shadow-md"
          disabled={isPending}
        >
          {isPending ? (
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
          <button
            type="button"
            onClick={onResendCode}
            disabled={isPending}
            className="hover:text-foreground transition-colors disabled:opacity-50"
          >
            {isPending ? "Sending..." : "Resend code"}
          </button>
          <button
            type="button"
            onClick={onStartOver}
            className="hover:text-foreground transition-colors"
          >
            Use different email
          </button>
        </div>
      </form>
    );
  }

  return (
    <form
      className="flex flex-col gap-7"
      onSubmit={(event) => {
        event.preventDefault();
        onSendCode(draftEmail);
      }}
    >
      <div>
        <h1 className="text-2xl font-bold tracking-tight mb-1.5">
          Reset your password
        </h1>
        <p className="text-muted-foreground text-[15px] leading-relaxed">
          Enter the email address associated with your account and we&apos;ll
          send you a verification code.
        </p>
      </div>

      {serverError ? (
        <div className="flex items-start gap-2.5 rounded-lg bg-destructive/10 border border-destructive/20 px-4 py-3 text-sm text-foreground">
          <AlertCircle className="size-4 shrink-0 mt-0.5 text-destructive" />
          <span>{serverError}</span>
        </div>
      ) : null}

      <div className="space-y-4">
        <div className="space-y-2">
          <Label htmlFor="email">Email address</Label>
          <div className="relative">
            <Mail className="pointer-events-none absolute left-2.5 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" />
            <Input
              id="email"
              type="email"
              placeholder="you@company.com"
              autoComplete="email"
              value={draftEmail}
              onChange={(event) => setDraftEmail(event.target.value)}
              className="pl-9"
            />
          </div>
        </div>
      </div>

      <Button
        type="submit"
        className="w-full h-10 bg-[linear-gradient(135deg,#FF1E56_0%,#FC744C_35%,#1E90FF_100%)] hover:opacity-90 text-white font-medium border-0 shadow-md"
        disabled={isPending}
      >
        {isPending ? (
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

      <Button
        variant="ghost"
        type="button"
        className="w-full h-10 text-muted-foreground"
        onClick={onBackToSignIn}
      >
        <ArrowLeft className="size-4 mr-2" />
        Back to sign in
      </Button>
    </form>
  );
}
