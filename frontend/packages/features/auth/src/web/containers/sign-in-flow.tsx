import { useEffect, useMemo, useRef, useState } from "react";
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
  type SsoErrorKind,
} from "../components/auth-sign-in-surface";
import type { AuthFlowNavigation } from "./flow-navigation";

/** A refusal's wait. A new object per refusal, so a second refusal with the same wait restarts the count. */
type Lock = { readonly seconds: number } | null;
const lockOf = (seconds: number | null): Lock =>
  seconds && seconds > 0 ? { seconds } : null;

/** Seconds the lock card counts down from a refusal that carries `retryAfterSeconds`. */
function useCountdown(lock: Lock): number {
  const [deadline, setDeadline] = useState<number | null>(null);
  const [left, setLeft] = useState(0);
  useEffect(() => {
    setDeadline(lock ? Date.now() + lock.seconds * 1000 : null);
  }, [lock]);
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
  | SsoErrorKind
  | "session-ended"
  | "password-reset"
  | "mfa-enrollment";

/** What the sign-in page says for each code the backend puts on `?error=`; any other code reads as a plain failure. */
const PROVIDER_ERROR: Readonly<Record<string, EmailError>> = {
  [IDENTITY_ERROR.oauthMfaRequired]: "mfa-required",
  [IDENTITY_ERROR.oauthProviderUnavailable]: "provider-unavailable",
  [IDENTITY_ERROR.oauthStateInvalid]: "provider-expired",
  [IDENTITY_ERROR.oauthDenied]: "provider-denied",
  [IDENTITY_ERROR.oauthEmailInUse]: "provider-email-in-use",
  [IDENTITY_ERROR.oauthEmailUnverified]: "provider-email-unverified",
  // The company sign-in. None of these says whether an account exists for the person.
  [IDENTITY_ERROR.oauthSsoRequired]: "sso-required-social",
  [IDENTITY_ERROR.ssoStateInvalid]: "sso-state",
  [IDENTITY_ERROR.ssoDenied]: "sso-denied",
  [IDENTITY_ERROR.ssoFailed]: "sso-failed",
  [IDENTITY_ERROR.ssoEmailUnverified]: "sso-email-unverified",
  [IDENTITY_ERROR.ssoDomainMismatch]: "sso-domain-mismatch",
  [IDENTITY_ERROR.ssoIdentityConflict]: "sso-identity-conflict",
  [IDENTITY_ERROR.ssoNotProvisioned]: "sso-not-provisioned",
  [IDENTITY_ERROR.ssoUnavailable]: "sso-unavailable",
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
        enforced: boolean;
      };
      error: "unreachable" | "refused" | null;
    }
  | {
      kind: "otp";
      email: string;
      /** Null after a provider sign-in: the challenge is the backend's cookie and the page never holds it. */
      challengeToken: string | null;
      error: "wrong" | "expired" | "inactive" | "locked" | "unreachable" | null;
      backup: boolean;
    }
  | {
      kind: "enroll";
      /** Null after a provider sign-in: the token is the backend's cookie and the page never holds it. */
      enrollmentToken: string | null;
      /** Null until the start answers. */
      enrollment: {
        methodId: string;
        secret: string;
        otpAuthUri: string;
      } | null;
      error: "wrong" | "expired" | "locked" | "unreachable" | null;
    }
  | {
      kind: "codes";
      session: SessionResult;
      codes: readonly string[];
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
  mfaEnrollmentRequired = false,
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
  /** The page opened because a refresh was refused: the Account now requires two-step verification. */
  mfaEnrollmentRequired?: boolean;
  /**
   * `"mfa"` when the backend sent the browser here after a provider sign-in that needs a second
   * step: the challenge is in its httpOnly cookie, so the page asks for the code alone.
   * `"enroll"` (the `enroll=1` mark) is the same for a person who has no factor yet and whose
   * Account requires one: the enrolment token is in the cookie, so the step starts with no token.
   */
  initialStep?: "mfa" | "enroll" | null;
}) {
  const copy = useAuthCopy();
  const queryClient = useQueryClient();
  // Only an internal path is ever navigated to or handed to the provider round trip.
  const returnTo = safeReturnPath(redirect);
  const [phase, setPhase] = useState<Phase>(() =>
    !providerError && initialStep === "enroll"
      ? { kind: "enroll", enrollmentToken: null, enrollment: null, error: null }
      : !providerError && initialStep === "mfa"
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
                : mfaEnrollmentRequired
                  ? "mfa-enrollment"
                  : sessionEnded
                    ? "session-ended"
                    : null,
          },
  );
  const [typedEmail, setTypedEmail] = useState(initialEmail);
  const [lock, setLock] = useState<Lock>(null);
  const lockedSeconds = useCountdown(lock);

  const signedIn = useMemo(
    () => (session: SessionResult) => {
      queryClient.setQueryData(authQueryKeys.profile, session.user);
      navigation.navigate(
        returnTo
          ? { to: returnTo, replace: true }
          : { to: "/home", search: { open: "default" }, replace: true },
      );
    },
    [navigation, queryClient, returnTo],
  );

  const passwordRefused = useRef(false);
  const identify = useMutation({
    mutationFn: (email: string) => service.identify(email),
    onSuccess: (result, email) => {
      setTypedEmail(email);
      // A password refused for a company that requires single sign-on lands back here, saying so.
      const refused = passwordRefused.current;
      passwordRefused.current = false;
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
                enforced: result.sso.enforced,
              },
              error: refused ? "refused" : null,
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

  const startEnroll = useMutation({
    mutationFn: (enrollmentToken: string | null) =>
      service.startRequiredEnrollment(enrollmentToken),
    onSuccess: (enrollment) =>
      setPhase((current) =>
        current.kind === "enroll" ? { ...current, enrollment } : current,
      ),
    onError: (error) =>
      setPhase((current) =>
        current.kind === "enroll"
          ? {
              ...current,
              error:
                enrollFailure(error) === "unreachable"
                  ? "unreachable"
                  : "expired",
            }
          : current,
      ),
  });

  const completeEnroll = useMutation({
    mutationFn: (input: {
      enrollmentToken: string | null;
      methodId: string;
      code: string;
    }) => service.completeRequiredEnrollment(input),
    onSuccess: ({ recoveryCodes, ...session }) =>
      setPhase({ kind: "codes", session, codes: recoveryCodes }),
    onError: (error) =>
      setPhase((current) =>
        current.kind === "enroll"
          ? { ...current, error: enrollFailure(error) }
          : current,
      ),
  });

  // After a provider sign-in the page opens on the step with the backend's cookie: start it once.
  const startedFromCookie = useRef(false);
  useEffect(() => {
    if (
      initialStep === "enroll" &&
      !providerError &&
      !startedFromCookie.current
    ) {
      startedFromCookie.current = true;
      startEnroll.mutate(null);
    }
  });

  const login = useMutation({
    mutationFn: (input: { email: string; password: string }) =>
      service.login(input),
    onSuccess: (result, input) => {
      setLock(null);
      if (result.kind === "mfa")
        setPhase({
          kind: "otp",
          email: input.email,
          challengeToken: result.challengeToken,
          error: null,
          backup: false,
        });
      else if (result.kind === "enroll") {
        // No session yet: the authenticator is prepared at once, with the token held in memory only.
        setPhase({
          kind: "enroll",
          enrollmentToken: result.enrollmentToken,
          enrollment: null,
          error: null,
        });
        startEnroll.mutate(result.enrollmentToken);
      } else signedIn(result);
    },
    onError: (error, input) => {
      const failure = readFlowError(error);
      // Only a correct password on a company that requires single sign-on is told so (403);
      // the page goes back to the company step instead of showing a wrong-password message.
      if (failure.code === IDENTITY_ERROR.authSsoRequired) {
        passwordRefused.current = true;
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
      setLock(throttled ? lockOf(failure.retryAfterSeconds) : null);
      setPhase({
        kind: "password",
        email: input.email,
        // A throttled attempt says how long to wait, not that the password was wrong.
        error: throttled ? null : "wrong",
      });
    },
  });

  const enrollFailure = (error: unknown) => {
    const failure = readFlowError(error);
    setLock(isThrottled(failure) ? lockOf(failure.retryAfterSeconds) : null);
    return failure.network
      ? ("unreachable" as const)
      : isThrottled(failure)
        ? ("locked" as const)
        : failure.code === IDENTITY_ERROR.challengeInvalid
          ? ("expired" as const)
          : ("wrong" as const);
  };

  const verify = useMutation({
    mutationFn: (input: { challengeToken: string | null; code: string }) =>
      service.verifyMfa(input),
    onSuccess: (result) => {
      if (result.kind === "session") signedIn(result);
    },
    onError: (error) => {
      const failure = readFlowError(error);
      setLock(isThrottled(failure) ? lockOf(failure.retryAfterSeconds) : null);
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

  const startSso = useMutation({
    mutationFn: (email: string) => service.startSso(email, returnTo),
    onSuccess: ({ authorizationUrl }) => {
      setRedirecting("sso");
      navigation.startSsoSignIn(authorizationUrl);
    },
    onError: (error) => {
      const failure = readFlowError(error);
      // The address no longer routes to a provider (changed since the page asked): same answer for every cause.
      if (failure.code === IDENTITY_ERROR.ssoUnavailable)
        setPhase({ kind: "email", error: "sso-unavailable" });
      else
        setPhase((current) =>
          current.kind === "sso"
            ? { ...current, error: "unreachable" }
            : current,
        );
    },
  });

  const pending =
    identify.isPending ||
    startSso.isPending ||
    login.isPending ||
    verify.isPending ||
    completeEnroll.isPending ||
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
        : phase.kind === "enroll"
          ? {
              kind: "enroll",
              secret: phase.enrollment?.secret ?? null,
              otpAuthUri: phase.enrollment?.otpAuthUri ?? null,
              error: phase.error,
              lockedSeconds,
            }
          : phase.kind === "codes"
            ? { kind: "codes", codes: phase.codes }
            : phase;

  const backToEmail = () => {
    setLock(null);
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
      onSso={() => phase.kind === "sso" && startSso.mutate(phase.email)}
      onUsePassword={() =>
        phase.kind === "sso" &&
        setPhase({ kind: "password", email: phase.email, error: null })
      }
      onVerify={({ code }) =>
        phase.kind === "otp" &&
        verify.mutate({ challengeToken: phase.challengeToken, code })
      }
      onEnroll={({ code }) =>
        phase.kind === "enroll" &&
        phase.enrollment &&
        completeEnroll.mutate({
          enrollmentToken: phase.enrollmentToken,
          methodId: phase.enrollment.methodId,
          code,
        })
      }
      onCodesDone={() => phase.kind === "codes" && signedIn(phase.session)}
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
