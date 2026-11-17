import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { useNavigate, useSearch } from "@tanstack/react-router";
import {
  AccountEmailSurface,
  createUseProfile,
  createUseSecurity,
  useAccountSecurityCopy,
  type AccountEmailFlow,
} from "@notrelix/features-account";
import { useAppRuntime } from "@notrelix/runtime-web";
import { useRouteWorkspaceId, useToast } from "./use-account-pages";

const PURPOSE = "ChangeSecurityIdentity" as const;

const waitOf = (error: unknown): number | null => {
  const e = error as {
    status?: number;
    details?: { retryAfterSeconds?: unknown };
  };
  if (e?.status !== 429) return null;
  const seconds = e.details?.retryAfterSeconds;
  return typeof seconds === "number" && seconds > 0 ? seconds : 1;
};

type StepUp = Extract<AccountEmailFlow, { kind: "stepUp" }>;

/**
 * The sign-in email (`Identity.Profiles.UpdateEmail`): the new address is chosen, then the proof
 * of presence is bought (code, password or provider), then the link goes to the new inbox. The
 * address is held in this page's state while the proof is asked for; a provider round trip
 * carries it in the address bar (it is not a secret) and the proof waits in the httpOnly cookie.
 */
export function AccountEmailPage() {
  const { api: runtimeClient, env } = useAppRuntime();
  const workspaceId = useRouteWorkspaceId();
  const copy = useAccountSecurityCopy();
  const navigate = useNavigate();
  const search = useSearch({ strict: false }) as {
    stepUp?: unknown;
    stepUpResult?: unknown;
    email?: unknown;
  };
  const [busy, setBusy] = useState(false);
  const [notice] = useToast();
  const [step, setStep] = useState<
    (StepUp & { challengeToken: string | null }) | AccountEmailFlow | null
  >(null);
  const [stepError, setStepError] = useState<{
    error: "wrong" | "wait" | "failed" | "expired";
    wait?: number;
  } | null>(null);
  const [formError, setFormError] = useState<{
    error: "wait" | "failed";
    wait?: number;
  } | null>(null);
  const pending = useRef("");
  const challenge = useRef<string | null>(null);

  const deps = useMemo(
    () => ({ api: runtimeClient.api, endpoints: runtimeClient.endpoints }),
    [runtimeClient],
  );
  const useProfile = useMemo(() => createUseProfile(deps), [deps]);
  const useSecurity = useMemo(() => createUseSecurity(deps), [deps]);
  const { profile } = useProfile();
  const { service, summary } = useSecurity();

  const beginStepUp = useCallback(async () => {
    setFormError(null);
    setStepError(null);
    setBusy(true);
    try {
      const requirement = await service.getStepUpRequirement(PURPOSE);
      challenge.current = requirement.challengeToken;
      setStep({
        kind: "stepUp",
        purpose: "changeEmail",
        factor:
          requirement.factor === "mfa"
            ? "code"
            : requirement.factor === "oauth"
              ? "provider"
              : "password",
        providers:
          requirement.factor === "oauth"
            ? (summary.data?.linkedProviders ?? [])
            : undefined,
        error: null,
      });
    } catch {
      setFormError({ error: "failed" });
    } finally {
      setBusy(false);
    }
  }, [service, summary.data?.linkedProviders]);

  const finishStepUp = useCallback(
    async (proof: Promise<string>) => {
      setBusy(true);
      setStepError(null);
      // Once the proof is in hand a refusal is the action's, never "that code is wrong".
      let proved = false;
      try {
        const token = await proof;
        proved = true;
        const answer = await service.requestEmailChange({
          email: pending.current,
          stepUpToken: token,
        });
        // The same answer for any address; null = it already is the sign-in email.
        setStep(
          answer.pendingEmail === null
            ? null
            : { kind: "emailSent", email: answer.pendingEmail },
        );
      } catch (error) {
        const code = (error as { code?: string }).code;
        if (code === "identity.security.step-up-invalid") {
          // The proof is gone (too old): ask again for the same step.
          await beginStepUp();
          setStepError({ error: "expired" });
          return;
        }
        const seconds = waitOf(error);
        if (proved) {
          setStep(null);
          setFormError(
            seconds !== null
              ? { error: "wait", wait: seconds }
              : { error: "failed" },
          );
        } else if (seconds !== null)
          setStepError({ error: "wait", wait: seconds });
        else
          setStepError({
            error:
              (error as { status?: number }).status !== undefined
                ? "wrong"
                : "failed",
          });
      } finally {
        setBusy(false);
      }
    },
    [beginStepUp, service],
  );

  const submitEmail = useCallback(
    (email: string) => {
      pending.current = email;
      void beginStepUp();
    },
    [beginStepUp],
  );

  const submitCode = useCallback(
    (code: string) => {
      if (step?.kind !== "stepUp" || !challenge.current) return;
      void finishStepUp(
        service.completeStepUpWithCode(PURPOSE, challenge.current, code),
      );
    },
    [finishStepUp, service, step?.kind],
  );

  const submitPassword = useCallback(
    (password: string) => {
      if (step?.kind !== "stepUp") return;
      void finishStepUp(service.completeStepUpWithPassword(PURPOSE, password));
    },
    [finishStepUp, service, step?.kind],
  );

  // The person confirms with the provider they signed in with: the browser leaves for it and comes
  // back to this page with `stepUp=changeEmail`, the backend's proof waiting in its httpOnly cookie.
  const startProviderStepUp = useCallback(
    (provider: string) => {
      const path = service.providerStepUpPath(
        provider,
        PURPOSE,
        `/workspaces/${workspaceId}/account/email?stepUp=changeEmail&email=${encodeURIComponent(pending.current)}`,
      );
      if (!env.mockApi) {
        window.location.assign(`${env.apiUrl}${path}`);
        return;
      }
      // No browser leaves in the mock world: the mock plays the round trip and says where the redirect lands.
      void runtimeClient.api
        .get<{ redirectTo?: string }>(path, {
          skipAuthRefresh: true,
          skipGlobalErrorToast: true,
        })
        .then((answer) => {
          const landing = new URL(
            answer.redirectTo ?? "/",
            window.location.origin,
          );
          void navigate({
            to: landing.pathname,
            search: Object.fromEntries(landing.searchParams),
            replace: true,
          } as unknown as Parameters<typeof navigate>[0]);
        })
        .catch(() => setStepError({ error: "failed" }));
    },
    [env, navigate, runtimeClient.api, service, workspaceId],
  );

  // Back from the provider: spend the proof it left on the change that asked for it.
  const resumed = useRef(false);
  const resuming = search.stepUp === "changeEmail";
  useEffect(() => {
    // The marker is stripped once read, so the next return (the mock world stays in the page) resumes again.
    if (!resuming) resumed.current = false;
    if (resumed.current || !resuming || !summary.data) return;
    resumed.current = true;
    const failed = search.stepUpResult === "failed";
    pending.current = typeof search.email === "string" ? search.email : "";
    // The marker is read once: a reload must not spend (or ask for) another proof.
    void navigate({ to: ".", search: {}, replace: true } as Parameters<
      typeof navigate
    >[0]);
    const providerStep: StepUp = {
      kind: "stepUp",
      purpose: "changeEmail",
      factor: "provider",
      providers: summary.data.linkedProviders,
      error: null,
    };
    setBusy(true);
    void (async () => {
      if (failed) throw new Error("provider-failed");
      return service.claimProviderStepUp();
    })()
      .then(({ proof }) => finishStepUp(Promise.resolve(proof)))
      .catch(() => {
        setStep(providerStep);
        setStepError({ error: "failed" });
      })
      .finally(() => setBusy(false));
  }, [
    finishStepUp,
    navigate,
    resuming,
    search.email,
    search.stepUpResult,
    service,
    summary.data,
  ]);

  const flow = useMemo<AccountEmailFlow | null>(
    () =>
      step?.kind === "stepUp"
        ? { ...step, error: stepError?.error ?? null, wait: stepError?.wait }
        : (step as AccountEmailFlow | null),
    [step, stepError],
  );

  return (
    <AccountEmailSurface
      copy={copy}
      status={
        summary.isError
          ? "error"
          : summary.data && profile
            ? "ready"
            : "loading"
      }
      currentEmail={profile?.email ?? ""}
      busy={busy}
      error={formError?.error ?? null}
      wait={formError?.wait}
      notice={notice}
      flow={flow}
      pendingEmail={pending.current}
      onSubmitEmail={submitEmail}
      onSubmitCode={submitCode}
      onSubmitPassword={submitPassword}
      onStartProviderStepUp={startProviderStepUp}
      onCloseFlow={() => {
        setStep(null);
        setStepError(null);
      }}
      onRetry={() => void summary.refetch()}
    />
  );
}
