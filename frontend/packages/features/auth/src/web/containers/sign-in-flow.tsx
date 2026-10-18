import { useEffect, useMemo, useState } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import {
  readFlowError,
  type AuthFlowService,
  type SessionResult,
} from "../../core";
import { authQueryKeys } from "../../core/query/keys";
import { useAuthCopy } from "../copy/use-auth-copy";
import {
  AuthSignInSurface,
  type AuthSignInStep,
} from "../components/auth-sign-in-surface";
import type { AuthFlowNavigation } from "./flow-navigation";

/** Seconds the lock card counts down from a refusal that carries `retryAfterSeconds`. */
function useCountdown(seconds: number | null): number {
  const [deadline, setDeadline] = useState<number | null>(null);
  const [left, setLeft] = useState(0);
  useEffect(() => {
    setDeadline(seconds && seconds > 0 ? Date.now() + seconds * 1000 : null);
  }, [seconds]);
  useEffect(() => {
    if (deadline === null) {
      setLeft(0);
      return;
    }
    const tick = () =>
      setLeft(Math.max(0, Math.ceil((deadline - Date.now()) / 1000)));
    tick();
    const timer = setInterval(tick, 1000);
    return () => clearInterval(timer);
  }, [deadline]);
  return left;
}

type Phase =
  | { kind: "email"; error: "invalid" | "unreachable" | null }
  | {
      kind: "password";
      email: string;
      error: "wrong" | "unreachable" | null;
      attemptsLeft: number | null;
    }
  | {
      kind: "sso";
      email: string;
      org: {
        name: string;
        initials: string;
        providerLabel: string;
        domain: string;
      };
    }
  | {
      kind: "otp";
      email: string;
      challengeId: string;
      error: "wrong" | "expired" | "unreachable" | null;
      attemptsLeft: number | null;
      backup: boolean;
    };

/**
 * The sign-in page's state machine: email, then password, single sign-on or
 * the second step, and the session at the end. Holds no pixel; the surface
 * does.
 */
export function SignInFlow({
  service,
  navigation,
  redirect,
  initialEmail = "",
}: {
  service: AuthFlowService;
  navigation: AuthFlowNavigation;
  /** Where to go after signing in; the home page without it. */
  redirect?: string | null;
  initialEmail?: string;
}) {
  const copy = useAuthCopy();
  const queryClient = useQueryClient();
  const [phase, setPhase] = useState<Phase>({ kind: "email", error: null });
  const [typedEmail, setTypedEmail] = useState(initialEmail);
  const [lockSeconds, setLockSeconds] = useState<number | null>(null);
  const lockedSeconds = useCountdown(lockSeconds);

  const signedIn = useMemo(
    () => (session: SessionResult) => {
      queryClient.setQueryData(authQueryKeys.profile, session.user);
      navigation.navigate({ to: redirect || "/home", replace: true });
    },
    [navigation, queryClient, redirect],
  );

  const identify = useMutation({
    mutationFn: (email: string) => service.identify(email),
    onSuccess: (result, email) => {
      setTypedEmail(email);
      setPhase(
        result.method === "sso" && result.sso
          ? {
              kind: "sso",
              email,
              org: {
                name: result.sso.orgName,
                initials: result.sso.orgInitials,
                providerLabel: result.sso.providerLabel,
                domain: email.split("@")[1] ?? "",
              },
            }
          : { kind: "password", email, error: null, attemptsLeft: null },
      );
    },
    onError: (error) =>
      setPhase({
        kind: "email",
        error: readFlowError(error).fieldErrors.email
          ? "invalid"
          : "unreachable",
      }),
  });

  const login = useMutation({
    mutationFn: (input: { email: string; password: string }) =>
      service.login(input),
    onSuccess: (result, input) => {
      setLockSeconds(null);
      if ("requiresOtp" in result)
        setPhase({
          kind: "otp",
          email: input.email,
          challengeId: result.challengeId,
          error: null,
          attemptsLeft: null,
          backup: false,
        });
      else signedIn(result);
    },
    onError: (error, input) => {
      const failure = readFlowError(error);
      if (failure.code === "SSO_REQUIRED") {
        identify.mutate(input.email);
        return;
      }
      if (failure.network || failure.status === null) {
        setPhase({
          kind: "password",
          email: input.email,
          error: "unreachable",
          attemptsLeft: null,
        });
        return;
      }
      setLockSeconds(failure.retryAfterSeconds);
      setPhase({
        kind: "password",
        email: input.email,
        // A refusal for a locked account says it is locked, not that the password was wrong.
        error: failure.code === "ACCOUNT_LOCKED" ? null : "wrong",
        attemptsLeft: failure.attemptsLeft,
      });
    },
  });

  const verify = useMutation({
    mutationFn: (input: {
      challengeId: string;
      code: string;
      rememberDevice: boolean;
    }) => service.verifyOtp(input),
    onSuccess: signedIn,
    onError: (error) => {
      const failure = readFlowError(error);
      setPhase((current) =>
        current.kind === "otp"
          ? {
              ...current,
              error: failure.network
                ? "unreachable"
                : failure.code === "OTP_EXPIRED"
                  ? "expired"
                  : "wrong",
              attemptsLeft: failure.attemptsLeft,
            }
          : current,
      );
    },
  });

  const provider = useMutation({
    mutationFn: (input: { email?: string; provider: string }) =>
      service.ssoStart(input),
    onSuccess: (result) => {
      if (result.redirectUrl) window.location.assign(result.redirectUrl);
      else signedIn(result);
    },
    onError: () =>
      setPhase((current) =>
        current.kind === "email"
          ? { kind: "email", error: "unreachable" }
          : current,
      ),
  });

  const pending =
    identify.isPending ||
    login.isPending ||
    verify.isPending ||
    provider.isPending;

  const step: AuthSignInStep =
    phase.kind === "password"
      ? {
          kind: "password",
          email: phase.email,
          error: phase.error,
          attemptsLeft: phase.attemptsLeft,
          lockedSeconds,
        }
      : phase.kind === "otp"
        ? {
            kind: "otp",
            email: phase.email,
            error: phase.error,
            attemptsLeft: phase.attemptsLeft,
            backup: phase.backup,
          }
        : phase;

  const backToEmail = () => {
    setLockSeconds(null);
    setPhase({ kind: "email", error: null });
  };

  return (
    <AuthSignInSurface
      copy={copy}
      step={step}
      pending={pending}
      initialEmail={typedEmail}
      onContinue={(email) => identify.mutate(email)}
      onProvider={(name) => provider.mutate({ provider: name })}
      onCreateWorkspace={() => navigation.navigate({ to: "/sign-up" })}
      onChangeEmail={backToEmail}
      onSubmitPassword={(password) =>
        phase.kind === "password" &&
        login.mutate({ email: phase.email, password })
      }
      onForgot={() =>
        navigation.navigate({
          to: "/forgot-password",
          search: typedEmail ? { email: typedEmail } : undefined,
        })
      }
      onSso={() =>
        phase.kind === "sso" &&
        provider.mutate({ email: phase.email, provider: "microsoft-entra" })
      }
      onVerify={({ code, rememberDevice }) =>
        phase.kind === "otp" &&
        verify.mutate({ challengeId: phase.challengeId, code, rememberDevice })
      }
      onToggleBackup={() =>
        setPhase((current) =>
          current.kind === "otp"
            ? { ...current, backup: !current.backup, error: null }
            : current,
        )
      }
      onBack={backToEmail}
    />
  );
}
