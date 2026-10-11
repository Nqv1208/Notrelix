import { useState } from "react";
import { AlertCircle, ArrowRight, Eye, EyeOff, Loader2 } from "lucide-react";
import { Button, Checkbox, Input, Label } from "@notrelix/ui-web";
import { GoogleMark } from "./google-mark";

export interface AuthSubmitData {
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
        <p className="text-muted-foreground text-body">
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
          <GoogleMark className="size-4 mr-2 shrink-0" />
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
        className="w-full h-10 bg-[image:var(--brand-gradient)] hover:opacity-90 text-white font-medium shadow-md shadow-danger/10 transition-all border-0"
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
          className="font-medium text-foreground hover:text-link transition-colors underline-offset-4 hover:underline"
        >
          Create one
        </button>
      </p>
    </form>
  );
}
