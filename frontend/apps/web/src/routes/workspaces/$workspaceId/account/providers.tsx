import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { useNavigate, useSearch } from "@tanstack/react-router";
import {
  AccountProvidersSurface,
  createUseSecurity,
  useAccountSecurityCopy,
  type AccountProvidersFlow,
} from "@notrelix/features-account";
import { useAppRuntime } from "@notrelix/runtime-web";
import { useRouteWorkspaceId, useToast } from "./use-account-pages";

type Purpose = "link" | "unlink";
const PURPOSE = { link: "LinkOAuth", unlink: "UnlinkOAuth" } as const;
interface Step {
  purpose: Purpose;
  provider?: string;
  factor: "code" | "password" | "provider";
  providers?: readonly string[];
  challengeToken: string | null;
}
const LINK_RESULTS = ["linked", "already", "collision", "failed"] as const;

const named = (text: string, provider: string) =>
  text.replaceAll("{{provider}}", provider);

const waitOf = (error: unknown): number | null => {
  const e = error as {
    status?: number;
    details?: { retryAfterSeconds?: unknown };
  };
  if (e?.status !== 429) return null;
  const seconds = e.details?.retryAfterSeconds;
  return typeof seconds === "number" && seconds > 0 ? seconds : 1;
};

/**
 * Connected accounts (`Identity.Auth.LinkOAuth` / `UnlinkOAuth`): the proof of presence first, then
 * the link leaves for the provider and the backend brings the browser back to this page's path.
 */
export function AccountProvidersPage() {
  const { api: runtimeClient, env } = useAppRuntime();
  const workspaceId = useRouteWorkspaceId();
  const copy = useAccountSecurityCopy();
  const navigate = useNavigate();
  const search = useSearch({ strict: false }) as {
    stepUp?: unknown;
    stepUpResult?: unknown;
    provider?: unknown;
    link?: unknown;
  };
  const [busy, setBusy] = useState(false);
  const [problem, setProblem] = useState<string | null>(null);
  const [notice, showNotice] = useToast();
  const [step, setStep] = useState<Step | null>(null);
  const [stepError, setStepError] = useState<{
    error: "wrong" | "wait" | "failed" | "expired";
    wait?: number;
  } | null>(null);

  const deps = useMemo(
    () => ({ api: runtimeClient.api, endpoints: runtimeClient.endpoints }),
    [runtimeClient],
  );
  const useSecurity = useMemo(() => createUseSecurity(deps), [deps]);
  const { service, summary, invalidate } = useSecurity();

  const beginStepUp = useCallback(
    async (purpose: Purpose, provider: string) => {
      setProblem(null);
      setStepError(null);
      setBusy(true);
      try {
        const requirement = await service.getStepUpRequirement(
          PURPOSE[purpose],
        );
        setStep({
          purpose,
          provider,
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
          challengeToken: requirement.challengeToken,
        });
      } catch {
        setProblem(copy.actionError);
      } finally {
        setBusy(false);
      }
    },
    [copy.actionError, service, summary.data?.linkedProviders],
  );

  const finishStepUp = useCallback(
    async (
      proof: Promise<string>,
      current: Pick<Step, "purpose" | "provider">,
    ) => {
      setBusy(true);
      setStepError(null);
      // Once the proof is in hand a refusal is the action's, never "that code is wrong".
      let proved = false;
      try {
        const token = await proof;
        proved = true;
        const provider = current.provider ?? "";
        if (current.purpose === "link") {
          // The proof is spent by the link start: the browser leaves for the provider, and the
          // backend brings it back to the path given here (an internal path, checked there).
          const path = service.providerLinkPath(
            provider,
            token,
            `/workspaces/${workspaceId}/account/providers`,
          );
          if (!env.mockApi) {
            window.location.assign(`${env.apiUrl}${path}`);
            return;
          }
          const answer = await runtimeClient.api.get<{ redirectTo?: string }>(
            path,
            { skipAuthRefresh: true, skipGlobalErrorToast: true },
          );
          const landing = new URL(
            answer.redirectTo ?? "/",
            window.location.origin,
          );
          setStep(null);
          await navigate({
            to: landing.pathname,
            search: Object.fromEntries(landing.searchParams),
            replace: true,
          } as unknown as Parameters<typeof navigate>[0]);
        } else {
          try {
            await service.unlinkProvider(provider, token);
            setStep(null);
            await invalidate();
            showNotice(named(copy.providers.unlinked, provider));
          } catch (error) {
            // Refused (409): it is the last way to sign in; the proof is spent, so ask again next time.
            if ((error as { status?: number }).status !== 409) throw error;
            setStep(null);
            await invalidate();
            setProblem(named(copy.providers.unlinkRefused, provider));
          }
        }
      } catch (error) {
        if (
          (error as { code?: string }).code ===
          "identity.security.step-up-invalid"
        ) {
          await beginStepUp(current.purpose, current.provider ?? "");
          setStepError({ error: "expired" });
          return;
        }
        const wait = waitOf(error);
        if (wait !== null) setStepError({ error: "wait", wait });
        else
          setStepError({
            error:
              !proved && (error as { status?: number }).status !== undefined
                ? "wrong"
                : "failed",
          });
      } finally {
        setBusy(false);
      }
    },
    [
      beginStepUp,
      copy.providers.unlinkRefused,
      copy.providers.unlinked,
      env,
      invalidate,
      navigate,
      runtimeClient.api,
      service,
      showNotice,
      workspaceId,
    ],
  );

  const submitCode = useCallback(
    (code: string) => {
      if (!step?.challengeToken) return;
      void finishStepUp(
        service.completeStepUpWithCode(
          PURPOSE[step.purpose],
          step.challengeToken,
          code,
        ),
        step,
      );
    },
    [finishStepUp, service, step],
  );

  const submitPassword = useCallback(
    (password: string) => {
      if (!step) return;
      void finishStepUp(
        service.completeStepUpWithPassword(PURPOSE[step.purpose], password),
        step,
      );
    },
    [finishStepUp, service, step],
  );

  // The person confirms with the provider they signed in with: the browser leaves for it and comes
  // back to this page with `stepUp=<purpose>`, the backend's proof waiting in its httpOnly cookie.
  const startProviderStepUp = useCallback(
    (provider: string) => {
      if (!step) return;
      const path = service.providerStepUpPath(
        provider,
        PURPOSE[step.purpose],
        `/workspaces/${workspaceId}/account/providers?stepUp=${step.purpose}${step.provider ? `&provider=${encodeURIComponent(step.provider)}` : ""}`,
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
    [env, navigate, runtimeClient.api, service, step, workspaceId],
  );

  // Back from the provider: spend the proof it left on the action that asked for it.
  const resumed = useRef(false);
  const resumePurpose: Purpose | null =
    search.stepUp === "link" || search.stepUp === "unlink"
      ? search.stepUp
      : null;
  useEffect(() => {
    // The marker is stripped once read, so the next return (the mock world stays in the page) resumes again.
    if (resumePurpose === null) resumed.current = false;
    if (resumed.current || resumePurpose === null || !summary.data) return;
    resumed.current = true;
    const failed = search.stepUpResult === "failed";
    // The marker is read once: a reload must not spend (or ask for) another proof.
    void navigate({ to: ".", search: {}, replace: true } as Parameters<
      typeof navigate
    >[0]);
    const current: Step = {
      purpose: resumePurpose,
      provider:
        typeof search.provider === "string" ? search.provider : undefined,
      factor: "provider",
      providers: summary.data.linkedProviders,
      challengeToken: null,
    };
    setBusy(true);
    void (async () => {
      if (failed) throw new Error("provider-failed");
      return service.claimProviderStepUp();
    })()
      .then(({ proof }) => finishStepUp(Promise.resolve(proof), current))
      .catch(() => {
        setStep(current);
        setStepError({ error: "failed" });
      })
      .finally(() => setBusy(false));
  }, [
    finishStepUp,
    navigate,
    resumePurpose,
    search.provider,
    search.stepUpResult,
    service,
    summary.data,
  ]);

  // Back from the provider's link: say how it ended, then strip the marker so a reload says nothing.
  const linkResult = LINK_RESULTS.find((result) => result === search.link);
  useEffect(() => {
    if (!linkResult) return;
    const provider = typeof search.provider === "string" ? search.provider : "";
    void navigate({ to: ".", search: {}, replace: true } as Parameters<
      typeof navigate
    >[0]);
    // A refusal is a sentence too long for the toast: it stays in the card until the next try.
    if (linkResult === "linked" || linkResult === "already")
      showNotice(named(copy.providers[linkResult], provider));
    else setProblem(named(copy.providers[linkResult], provider));
    if (linkResult === "linked") void invalidate();
  }, [
    copy.providers,
    invalidate,
    linkResult,
    navigate,
    search.provider,
    showNotice,
  ]);

  const flow = useMemo<AccountProvidersFlow | null>(
    () =>
      step
        ? {
            kind: "stepUp",
            purpose: step.purpose,
            provider: step.provider,
            factor: step.factor,
            providers: step.providers,
            error: stepError?.error ?? null,
            wait: stepError?.wait,
          }
        : null,
    [step, stepError],
  );

  return (
    <AccountProvidersSurface
      copy={copy}
      status={summary.isError ? "error" : summary.data ? "ready" : "loading"}
      linkedProviders={summary.data?.linkedProviders ?? []}
      hasPassword={summary.data?.hasPassword ?? true}
      busy={busy}
      notice={notice}
      problem={problem}
      flow={flow}
      onSetPassword={() =>
        void navigate({
          to: `/workspaces/${workspaceId}/account/security`,
        } as unknown as Parameters<typeof navigate>[0])
      }
      onLink={(provider) => void beginStepUp("link", provider)}
      onUnlink={(provider) => void beginStepUp("unlink", provider)}
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
