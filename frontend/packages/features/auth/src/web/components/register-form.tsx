import { useState } from "react";
import { Link } from "@tanstack/react-router";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import {
  Mail,
  Lock,
  Eye,
  EyeOff,
  User,
  ArrowRight,
  Loader2,
  AlertCircle,
} from "lucide-react";
import { cn } from "@notrelix/ui-web";
import { Button } from "@notrelix/ui-web";
import { Label } from "@notrelix/ui-web";
import { Checkbox } from "@notrelix/ui-web";
import { InputGroup, InputGroupAddon, InputGroupInput } from "@notrelix/ui-web";
import {
  registerSchema,
  type RegisterRequest,
  parseAuthError,
  resolveErrorDisplay,
} from "../../core";
import { createUseRegister, type NavigationDeps } from "../hooks/use-register";
import type { AuthApiClient, AuthEndpoints } from "../../core/api/auth.service";

function getPasswordStrength(password: string): {
  score: number;
  label: string;
  color: string;
} {
  let score = 0;
  if (password.length >= 8) score++;
  if (/[A-Z]/.test(password)) score++;
  if (/[a-z]/.test(password)) score++;
  if (/[0-9]/.test(password)) score++;
  if (/[^A-Za-z0-9]/.test(password)) score++;

  if (score <= 1) return { score, label: "Weak", color: "bg-red-500" };
  if (score <= 2) return { score, label: "Fair", color: "bg-orange-500" };
  if (score <= 3) return { score, label: "Good", color: "bg-amber-500" };
  if (score <= 4) return { score, label: "Strong", color: "bg-emerald-500" };
  return { score, label: "Very strong", color: "bg-emerald-600" };
}

interface RegisterFormDeps extends NavigationDeps {
  api: AuthApiClient;
  endpoints: AuthEndpoints;
  translate?: (key: string) => string;
}

export function createRegisterForm(deps: RegisterFormDeps) {
  const useRegister = createUseRegister(deps);
  const t = deps.translate ?? ((key: string) => key);

  return function RegisterForm({
    className,
    ...props
  }: React.ComponentPropsWithoutRef<"form">) {
    const registerMutation = useRegister();
    const [serverError, setServerError] = useState<string | null>(null);
    const [showPassword, setShowPassword] = useState(false);
    const [showConfirm, setShowConfirm] = useState(false);

    const {
      register,
      handleSubmit,
      setError,
      watch,
      formState: { errors },
    } = useForm<RegisterRequest>({
      resolver: zodResolver(registerSchema),
    });

    const passwordValue = watch("password") ?? "";
    const strength = getPasswordStrength(passwordValue);

    const onSubmit = (data: RegisterRequest) => {
      setServerError(null);
      const payload = {
        name: `${data.firstName} ${data.lastName}`,
        email: data.email,
        password: data.password,
      };

      registerMutation.mutate(payload, {
        onError: (error) => {
          const parsed = parseAuthError(error);
          if (parsed.fieldErrors.email)
            setError("email", {
              type: "server",
              message: String(parsed.fieldErrors.email),
            });
          if (parsed.fieldErrors.password)
            setError("password", {
              type: "server",
              message: String(parsed.fieldErrors.password),
            });
          setServerError(
            parsed.messageKey ? t(parsed.messageKey) : parsed.rawMessage,
          );
        },
      });
    };

    return (
      <form
        onSubmit={handleSubmit(onSubmit)}
        className={cn("flex flex-col gap-6", className)}
        {...props}
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

        {serverError && (
          <div className="flex items-start gap-2.5 rounded-lg bg-destructive/10 border border-destructive/20 px-4 py-3 text-sm text-destructive">
            <AlertCircle className="size-4 shrink-0 mt-0.5" />
            <span>{serverError}</span>
          </div>
        )}

        <div className="space-y-4">
          <div className="grid grid-cols-2 gap-3">
            <div className="space-y-2">
              <Label htmlFor="firstName">First name</Label>
              <InputGroup className="h-10" aria-invalid={!!errors.firstName}>
                <InputGroupAddon align="inline-start">
                  <User className="size-4 text-muted-foreground" />
                </InputGroupAddon>
                <InputGroupInput
                  id="firstName"
                  type="text"
                  placeholder="John"
                  autoComplete="given-name"
                  {...register("firstName")}
                />
              </InputGroup>
              {errors.firstName && (
                <p className="text-xs text-destructive">
                  {resolveErrorDisplay(errors.firstName.message, t)}
                </p>
              )}
            </div>
            <div className="space-y-2">
              <Label htmlFor="lastName">Last name</Label>
              <InputGroup className="h-10" aria-invalid={!!errors.lastName}>
                <InputGroupAddon align="inline-start">
                  <User className="size-4 text-muted-foreground" />
                </InputGroupAddon>
                <InputGroupInput
                  id="lastName"
                  type="text"
                  placeholder="Doe"
                  autoComplete="family-name"
                  {...register("lastName")}
                />
              </InputGroup>
              {errors.lastName && (
                <p className="text-xs text-destructive">
                  {resolveErrorDisplay(errors.lastName.message, t)}
                </p>
              )}
            </div>
          </div>

          <div className="space-y-2">
            <Label htmlFor="email">Work email</Label>
            <InputGroup className="h-10" aria-invalid={!!errors.email}>
              <InputGroupAddon align="inline-start">
                <Mail className="size-4 text-muted-foreground" />
              </InputGroupAddon>
              <InputGroupInput
                id="email"
                type="email"
                placeholder="you@company.com"
                autoComplete="email"
                {...register("email")}
              />
            </InputGroup>
            {errors.email && (
              <p className="text-xs text-destructive">
                {resolveErrorDisplay(errors.email.message, t)}
              </p>
            )}
          </div>

          <div className="space-y-2">
            <Label htmlFor="password">Password</Label>
            <InputGroup className="h-10" aria-invalid={!!errors.password}>
              <InputGroupAddon align="inline-start">
                <Lock className="size-4 text-muted-foreground" />
              </InputGroupAddon>
              <InputGroupInput
                id="password"
                type={showPassword ? "text" : "password"}
                placeholder="Min. 8 characters"
                autoComplete="new-password"
                {...register("password")}
              />
              <InputGroupAddon align="inline-end">
                <button
                  type="button"
                  onClick={() => setShowPassword(!showPassword)}
                  className="text-muted-foreground hover:text-foreground transition-colors p-1"
                  aria-label={showPassword ? "Hide password" : "Show password"}
                >
                  {showPassword ? (
                    <EyeOff className="size-4" />
                  ) : (
                    <Eye className="size-4" />
                  )}
                </button>
              </InputGroupAddon>
            </InputGroup>
            {errors.password && (
              <p className="text-xs text-destructive">
                {resolveErrorDisplay(errors.password.message, t)}
              </p>
            )}
            {passwordValue.length > 0 && (
              <div className="flex items-center gap-2">
                <div className="flex-1 flex gap-1">
                  {[1, 2, 3, 4, 5].map((i) => (
                    <div
                      key={i}
                      className={cn(
                        "h-1 flex-1 rounded-full transition-colors",
                        i <= strength.score ? strength.color : "bg-muted",
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

          <div className="space-y-2">
            <Label htmlFor="confirmPassword">Confirm password</Label>
            <InputGroup
              className="h-10"
              aria-invalid={!!errors.confirmPassword}
            >
              <InputGroupAddon align="inline-start">
                <Lock className="size-4 text-muted-foreground" />
              </InputGroupAddon>
              <InputGroupInput
                id="confirmPassword"
                type={showConfirm ? "text" : "password"}
                placeholder="Re-enter your password"
                autoComplete="new-password"
                {...register("confirmPassword")}
              />
              <InputGroupAddon align="inline-end">
                <button
                  type="button"
                  onClick={() => setShowConfirm(!showConfirm)}
                  className="text-muted-foreground hover:text-foreground transition-colors p-1"
                  aria-label={showConfirm ? "Hide password" : "Show password"}
                >
                  {showConfirm ? (
                    <EyeOff className="size-4" />
                  ) : (
                    <Eye className="size-4" />
                  )}
                </button>
              </InputGroupAddon>
            </InputGroup>
            {errors.confirmPassword && (
              <p className="text-xs text-destructive">
                {resolveErrorDisplay(errors.confirmPassword.message, t)}
              </p>
            )}
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

        <p className="text-center text-sm text-muted-foreground">
          Already have an account?{" "}
          <Link
            to="/sign-in"
            className="font-medium text-foreground hover:text-primary transition-colors underline-offset-4 hover:underline"
          >
            Sign in
          </Link>
        </p>
      </form>
    );
  };
}
