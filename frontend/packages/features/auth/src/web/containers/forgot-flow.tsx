import { useEffect, useState } from "react";
import { useMutation } from "@tanstack/react-query";
import { isThrottled, readFlowError, type AuthFlowService } from "../../core";
import { useAuthCopy } from "../copy/use-auth-copy";
import {
  AuthForgotSurface,
  type AuthForgotStep,
} from "../components/auth-forgot-surface";
import type { AuthFlowNavigation } from "./flow-navigation";

/** Seconds before the reset link can be sent again. */
export const RESEND_SECONDS = 60;

/** Password reset: ask for the email, then say a link went out. */
export function ForgotFlow({
  service,
  navigation,
  initialEmail = "",
}: {
  service: AuthFlowService;
  navigation: AuthFlowNavigation;
  initialEmail?: string;
}) {
  const copy = useAuthCopy();
  const [step, setStep] = useState<AuthForgotStep>({ kind: "ask" });
  // "Dùng email khác" starts the form again with an empty field.
  const [startEmail, setStartEmail] = useState(initialEmail);
  const [deadline, setDeadline] = useState<number | null>(null);
  const [left, setLeft] = useState(0);
  // A send again that was refused leaves the person on the "sent" step with the reason.
  const [sentError, setSentError] = useState<"limited" | "unreachable" | null>(
    null,
  );

  useEffect(() => {
    if (deadline === null) return;
    const tick = () =>
      setLeft(Math.max(0, Math.ceil((deadline - Date.now()) / 1000)));
    tick();
    const timer = setInterval(tick, 1000);
    return () => clearInterval(timer);
  }, [deadline]);

  const send = useMutation({
    mutationFn: (email: string) => service.forgotPassword(email),
    onSuccess: (_result, email) => {
      setSentError(null);
      setDeadline(Date.now() + RESEND_SECONDS * 1000);
      setLeft(RESEND_SECONDS);
      setStep({ kind: "sent", email, resendSeconds: RESEND_SECONDS });
    },
    onError: (error) => {
      const failure = readFlowError(error);
      const reason = isThrottled(failure)
        ? "limited"
        : failure.fieldErrors.email
          ? "invalid"
          : "unreachable";
      if (step.kind === "sent" && reason !== "invalid") setSentError(reason);
      else setStep({ kind: "ask", error: reason });
    },
  });

  const shown: AuthForgotStep =
    step.kind === "sent"
      ? { ...step, resendSeconds: left, error: sentError }
      : step;

  return (
    <AuthForgotSurface
      copy={copy}
      step={shown}
      pending={send.isPending}
      initialEmail={startEmail}
      onSend={(email) => send.mutate(email)}
      onResend={() => step.kind === "sent" && send.mutate(step.email)}
      onBack={() => navigation.navigate({ to: "/sign-in" })}
      onOtherEmail={() => {
        setSentError(null);
        setDeadline(null);
        setStartEmail("");
        setStep({ kind: "ask" });
      }}
    />
  );
}
