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
} from "lucide-react";
import { Button, Input, Label } from "@notrelix/ui-web";

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
