import { useEffect, useRef, useState } from "react";
import { useMutation } from "@tanstack/react-query";
import { readFlowError, type AuthFlowService } from "../../core";
import { useAuthCopy } from "../copy/use-auth-copy";
import {
  AuthVerifyEmailSurface,
  type AuthVerifyEmailStep,
} from "../components/auth-verify-email-surface";
import type { AuthFlowNavigation } from "./flow-navigation";

/**
 * Email confirmation. The link in the email carries the one-time token; it is
 * spent once, here, as soon as the page opens. Without a token the page is
 * where a member who has not confirmed yet asks for a new link.
 */
export function VerifyEmailFlow({
  service,
  navigation,
  token,
  initialEmail = "",
}: {
  service: AuthFlowService;
  navigation: AuthFlowNavigation;
  token: string | null;
  initialEmail?: string;
}) {
  const copy = useAuthCopy();
  const [step, setStep] = useState<AuthVerifyEmailStep>(
    token ? { kind: "confirming" } : { kind: "waiting" },
  );
  const [sent, setSent] = useState(false);
  const [error, setError] = useState<"invalid" | "unreachable" | null>(null);

  const confirm = useMutation({
    mutationFn: (value: string) => service.confirmEmail(value),
    onSuccess: () => setStep({ kind: "done" }),
    onError: () => setStep({ kind: "failed" }),
  });
  // The token is single use: a second call (a re-render, strict mode) would fail the first.
  const spent = useRef(false);
  useEffect(() => {
    if (!token || spent.current) return;
    spent.current = true;
    confirm.mutate(token);
  }, [token]);

  const resend = useMutation({
    mutationFn: (email: string) => service.resendEmailVerification(email),
    onSuccess: () => {
      setSent(true);
      setError(null);
    },
    onError: (failure) => {
      setSent(false);
      setError(
        readFlowError(failure).fieldErrors.email ? "invalid" : "unreachable",
      );
    },
  });

  return (
    <AuthVerifyEmailSurface
      copy={copy}
      step={step}
      pending={resend.isPending}
      initialEmail={initialEmail}
      sent={sent}
      error={error}
      onResend={(email) => resend.mutate(email)}
      onContinue={() =>
        navigation.navigate({ to: "/onboarding", replace: true })
      }
      onBack={() => navigation.navigate({ to: "/sign-in" })}
    />
  );
}
