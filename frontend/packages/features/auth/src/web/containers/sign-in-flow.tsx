import { useEffect, useMemo, useState } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import {
  IDENTITY_ERROR,
  isThrottled,
  readFlowError,
  safeReturnPath,
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

type EmailError =
  | "invalid"
  | "unreachable"
  | "mfa-required"
  | "provider-failed"
  | "provider-unavailable"
  | "provider-expired"
  | "provider-denied"
  | "provider-email-in-use"
  | "provider-email-unverified"
  | "session-ended"
  | "password-reset";

/** What the sign-in page says for each code the backend puts on `?error=`; any other code reads as a plain failure. */
const PROVIDER_ERROR: Readonly<Record<string, EmailError>> = {
  [IDENTITY_ERROR.oauthMfaRequired]: "mfa-required",
  [IDENTITY_ERROR.oauthProviderUnavailable]: "provider-unavailable",
  [IDENTITY_ERROR.oauthStateInvalid]: "provider-expired",
  [IDENTITY_ERROR.oauthDenied]: "provider-denied",
  [IDENTITY_ERROR.oauthEmailInUse]: "provider-email-in-use",
  [IDENTITY_ERROR.oauthEmailUnverified]: "provider-email-unverified",
};

type Phase =
  | { kind: "email"; error: EmailError | null }
  | {
      kind: "password";
      email: string;
      error: "wrong" | "unreachable" | null;
    }
  | {
      kind: "sso";
      email: string;
      org: {
        name: string;
        initials: string;
        provider: string;
        providerLabel: string;
        domain: string;
      };
    }
  | {
      kind: "otp";
      email: string;
      /** Null after a provider sign-in: the challenge is the backend's cookie and the page never holds it. */
      challengeToken: string | null;
      error: "wrong" | "expired" | "inactive" | "locked" | "unreachable" | null;
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
  providerError = null,
  sessionEnded = false,
  passwordReset = false,
  initialStep = null,
}: {
  service: AuthFlowService;
  navigation: AuthFlowNavigation;
  /** Where to go after signing in; the home page without it. */
  redirect?: string | null;
  initialEmail?: string;
  /** The `error` code the backend put on `/sign-in` after a provider sign-in that did not open a session. */
  providerError?: string | null;
  /** The page opened because the person's session ended: it says so above the form. */
  sessionEnded?: boolean;
  /** The page opened after a password reset: it says so above the form. */
  passwordReset?: boolean;
  /**
   * `"mfa"` when the backend sent the browser here after a provider sign-in that needs a second
   * step: the challenge is in its httpOnly cookie, so the page asks for the code alone.
   */
  initialStep?: "mfa" | null;
}) {
  const copy = useAuthCopy();
  const queryClient = useQueryClient();
  // Only an internal path is ever navigated to or handed to the provider round trip.
  const returnTo = safeReturnPath(redirect);
  const [phase, setPhase] = useState<Phase>(() =>
    !providerError && initialStep === "mfa"
      ? {
          kind: "otp",
          email: "",
          challengeToken: null,
          error: null,
          backup: false,
        }
      : {
          kind: "email",
          error: providerError
            ? (PROVIDER_ERROR[providerError] ?? "provider-failed")
            : passwordReset
              ? "password-reset"
              : sessionEnded
                ? "session-ended"
                : null,
        },
  );
  const [typedEmail, setTypedEmail] = useState(initialEmail);
  const [lockSeconds, setLockSeconds] = useState<number | null>(null);
  const lockedSeconds = useCountdown(lockSeconds);

  const signedIn = useMemo(
    () => (session: SessionResult) => {
      queryClient.setQueryData(authQueryKeys.profile, session.user);
      navigation.navigate({ to: returnTo ?? "/home", replace: true });
    },
    [navigation, queryClient, returnTo],
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
                provider: result.sso.provider,
                providerLabel: result.sso.providerLabel,
                domain: email.split("@")[1] ?? "",
              },
            }
          : { kind: "password", email, error: null },
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
      if (result.kind === "mfa")
        setPhase({
          kind: "otp",
          email: input.email,
          challengeToken: result.challengeToken,
          error: null,
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
        });
        return;
      }
      const throttled = isThrottled(failure);
      setLockSeconds(throttled ? failure.retryAfterSeconds : null);
      setPhase({
        kind: "password",
        email: input.email,
        // A throttled attempt says how long to wait, not that the password was wrong.
        error: throttled ? null : "wrong",
      });
    },
  });

  const verify = useMutation({
    mutationFn: (input: { challengeToken: string | null; code: string }) =>
      service.verifyMfa(input),
    onSuccess: (result) => {
      if (result.kind === "session") signedIn(result);
    },
    onError: (error) => {
      const failure = readFlowError(error);
      setLockSeconds(isThrottled(failure) ? failure.retryAfterSeconds : null);
      setPhase((current) =>
        current.kind === "otp"
          ? {
              ...current,
              error: failure.network
                ? "unreachable"
                : isThrottled(failure)
                  ? "locked"
                  : failure.code === IDENTITY_ERROR.accountInactive
                    ? "inactive"
                    : failure.code === IDENTITY_ERROR.challengeInvalid
                      ? "expired"
                      : "wrong",
            }
          : current,
      );
    },
  });

  // The browser is leaving for the provider: the buttons wait and say where it goes. A page
  // restored from the back-forward cache is the same page again, so it starts over.
  const [redirecting, setRedirecting] = useState<string | null>(null);
  useEffect(() => {
    const restore = (event: PageTransitionEvent) => {
      if (event.persisted) setRedirecting(null);
    };
    window.addEventListener("pageshow", restore);
    return () => window.removeEventListener("pageshow", restore);
  }, []);

  /** The provider's page and the way back are the backend's: the browser leaves. */
  const startProvider = (name: string) => {
    setRedirecting(name);
    navigation.startProviderSignIn(service.oauthStartPath(name, returnTo));
  };

  const pending =
    identify.isPending ||
    login.isPending ||
    verify.isPending ||
    redirecting !== null;

  const step: AuthSignInStep =
    phase.kind === "password"
      ? {
          kind: "password",
          email: phase.email,
          error: phase.error,
          lockedSeconds,
        }
      : phase.kind === "otp"
        ? {
            kind: "otp",
            email: phase.email,
            error: phase.error,
            lockedSeconds,
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
      redirecting={
        redirecting === "google" || redirecting === "microsoft"
          ? redirecting
          : null
      }
      initialEmail={typedEmail}
      onContinue={(email) => identify.mutate(email)}
      onProvider={startProvider}
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
      onSso={() => phase.kind === "sso" && startProvider(phase.org.provider)}
      onVerify={({ code }) =>
        phase.kind === "otp" &&
        verify.mutate({ challengeToken: phase.challengeToken, code })
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
