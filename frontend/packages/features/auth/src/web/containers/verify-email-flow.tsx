import { useEffect, useRef, useState } from "react";
import { useMutation } from "@tanstack/react-query";
import { isThrottled, readFlowError, type AuthFlowService } from "../../core";
import { useAuthCopy } from "../copy/use-auth-copy";
import {
  AuthVerifyEmailSurface,
  type AuthVerifyEmailStep,
} from "../components/auth-verify-email-surface";
import type { AuthFlowNavigation } from "./flow-navigation";

/**
 * The link's token is single use, and the page can mount twice while the app
 * settles (the second mount would spend the token again and show a dead link
 * over a confirmed address): one call per token serves every mount. A call that
 * failed is forgotten, so a retry with the same token (after a network drop) asks
 * the server again instead of replaying the failure.
 */
const confirmations = new Map<string, Promise<void>>();
function confirmOnce(service: AuthFlowService, token: string): Promise<void> {
  let call = confirmations.get(token);
  if (!call) {
    call = service.confirmEmail(token);
    confirmations.set(token, call);
    call.catch(() => {
      if (confirmations.get(token) === call) confirmations.delete(token);
    });
  }
  return call;
}

/** Seconds before another confirmation link can be sent. */
export const RESEND_SECONDS = 60;

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
  const [error, setError] = useState<
    "invalid" | "unreachable" | "limited" | null
  >(null);
  const [deadline, setDeadline] = useState<number | null>(null);
  const [left, setLeft] = useState(0);
  useEffect(() => {
    if (deadline === null) return;
    const tick = () => {
      const seconds = Math.max(0, Math.ceil((deadline - Date.now()) / 1000));
      setLeft(seconds);
      if (seconds === 0) setDeadline(null);
    };
    tick();
    const timer = setInterval(tick, 1000);
    return () => clearInterval(timer);
  }, [deadline]);

  const confirm = useMutation({
    mutationFn: (value: string) => confirmOnce(service, value),
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
      setDeadline(Date.now() + RESEND_SECONDS * 1000);
      setLeft(RESEND_SECONDS);
    },
    onError: (failure) => {
      setSent(false);
      const read = readFlowError(failure);
      setError(
        isThrottled(read)
          ? "limited"
          : read.fieldErrors.email
            ? "invalid"
            : "unreachable",
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
      resendSeconds={left}
      onResend={(email) => resend.mutate(email)}
      onContinue={() =>
        navigation.navigate({ to: "/onboarding", replace: true })
      }
      onBack={() => navigation.navigate({ to: "/sign-in" })}
    />
  );
}
