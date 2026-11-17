import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { useNavigate, useSearch } from "@tanstack/react-router";
import {
  AccountTokensSurface,
  createUseApiTokens,
  createUseSecurity,
  useAccountTokensCopy,
  type AccountApiToken,
  type AccountSection,
  type AccountTokensIssueFlow,
} from "@notrelix/features-account";
import { useLocale } from "@notrelix/i18n";
import { useAppRuntime } from "@notrelix/runtime-web";
import { useRouteWorkspaceId, useToast } from "./use-account-pages";

const DAY_MS = 24 * 60 * 60 * 1000;
const LIFETIMES = [30, 90, 365] as const;
type Lifetime = (typeof LIFETIMES)[number];
const PURPOSE = "IssueApiToken" as const;

const statusOf = (error: unknown) => (error as { status?: number })?.status;
const codeOf = (error: unknown) => (error as { code?: string })?.code;
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
 * The API tokens of the workspace (`Identity.ApiTokens.*`): list, issue after a
 * step-up proof, and revoke after a confirmation. The secret is held in this
 * page's state for the one step that shows it, and goes with it.
 */
export function AccountTokensPage() {
  const { api: runtimeClient, env } = useAppRuntime();
  const workspaceId = useRouteWorkspaceId();
  const copy = useAccountTokensCopy();
  const { locale } = useLocale();
  const navigate = useNavigate();
  const search = useSearch({ strict: false }) as {
    stepUp?: unknown;
    stepUpResult?: unknown;
    name?: unknown;
    days?: unknown;
  };
  const [flow, setFlow] = useState<AccountTokensIssueFlow | null>(null);
  const [busy, setBusy] = useState(false);
  const [actionError, setActionError] = useState(false);
  const [notice, showNotice] = useToast();
  /** The challenge a code answer quotes. */
  const challenge = useRef<string | null>(null);
  const draft = useRef<{ name: string; days: Lifetime }>({
    name: "",
    days: 90,
  });

  const deps = useMemo(
    () => ({ api: runtimeClient.api, endpoints: runtimeClient.endpoints }),
    [runtimeClient],
  );
  const useApiTokens = useMemo(() => createUseApiTokens(deps), [deps]);
  const useSecurity = useMemo(() => createUseSecurity(deps), [deps]);
  const { service, first, tokens, hasMore, loadingMore, loadMore, invalidate } =
    useApiTokens(workspaceId);
  const { summary } = useSecurity();

  // Not an owner or admin: the producer answers the NotFound of a missing workspace.
  const denied = first.isError && statusOf(first.error) === 404;
  const section = useMemo<
    AccountSection<{ tokens: readonly AccountApiToken[]; hasMore: boolean }>
  >(
    () =>
      first.data
        ? { status: "ready", data: { tokens, hasMore } }
        : first.isError
          ? { status: "error" }
          : { status: "loading" },
    [first.data, first.isError, tokens, hasMore],
  );
  const now = useMemo(
    () => Date.now(),
    // The age of "last used" is read against the moment the list was read.
    [first.dataUpdatedAt],
  );

  const beginProof = useCallback(
    async (error: "expired" | null = null) => {
      setActionError(false);
      setBusy(true);
      try {
        const requirement = await service.getStepUpRequirement(PURPOSE);
        const factor =
          requirement.factor === "mfa"
            ? "code"
            : requirement.factor === "oauth"
              ? "provider"
              : "password";
        const providers =
          factor === "provider"
            ? (await service.getSecuritySummary()).linkedProviders
            : [];
        challenge.current = requirement.challengeToken;
        setFlow({ kind: "proof", factor, providers, error });
      } catch {
        setFlow((current) =>
          current?.kind === "form" ? { ...current, error: "failed" } : current,
        );
      } finally {
        setBusy(false);
      }
    },
    [service],
  );
  const issue = useCallback(
    async (proof: Promise<string>) => {
      setBusy(true);
      // Once the proof is in hand a refusal is the issue's, never "that code is wrong".
      let proved = false;
      try {
        const token = await proof;
        proved = true;
        const { name, days } = draft.current;
        // A minute short of the limit, so a clock a little ahead of the server's never trips it.
        const expiresAt = new Date(
          Date.now() + days * DAY_MS - 60_000,
        ).toISOString();
        const issued = await service.issueApiToken(workspaceId, {
          name,
          expiresAt,
          stepUpToken: token,
        });
        setFlow({
          kind: "reveal",
          secret: issued.secret,
          name: issued.name,
          expiresAt,
        });
        await invalidate();
      } catch (error) {
        const wait = waitOf(error);
        if (codeOf(error) === "identity.security.step-up-invalid") {
          await beginProof("expired");
        } else if (codeOf(error) === "identity.api-tokens.lifetime-exceeded") {
          setFlow((c) =>
            c?.kind === "proof" ? { ...c, error: "lifetime" } : c,
          );
        } else {
          setFlow((c) =>
            c?.kind === "proof"
              ? wait !== null
                ? { ...c, error: "wait", wait }
                : {
                    ...c,
                    error:
                      !proved && statusOf(error) !== undefined
                        ? "wrong"
                        : "failed",
                  }
              : c,
          );
        }
      } finally {
        setBusy(false);
      }
    },
    [beginProof, invalidate, service, workspaceId],
  );

  const submitProof = useCallback(
    (value: string) => {
      if (flow?.kind !== "proof") return;
      if (flow.factor === "password")
        return void issue(service.completeStepUpWithPassword(PURPOSE, value));
      if (!challenge.current) return;
      void issue(
        service.completeStepUpWithCode(
          PURPOSE,
          challenge.current,
          value.replace(/\s+/g, ""),
        ),
      );
    },
    [flow, issue, service],
  );

  // The person confirms with the provider they signed in with: the browser leaves for it and comes
  // back here with the token's name and life in the address, the proof waiting in an httpOnly cookie.
  const startProvider = useCallback(
    (provider: string) => {
      const { name, days } = draft.current;
      const back = `/workspaces/${workspaceId}/account/tokens?stepUp=${PURPOSE}&name=${encodeURIComponent(name)}&days=${days}`;
      const path = service.providerStepUpPath(provider, PURPOSE, back);
      if (!env.mockApi) {
        window.location.assign(`${env.apiUrl}${path}`);
        return;
      }
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
        .catch(() =>
          setFlow((c) => (c?.kind === "proof" ? { ...c, error: "failed" } : c)),
        );
    },
    [env, navigate, runtimeClient.api, service, workspaceId],
  );

  // Back from the provider: issue the token the address names, spending the proof it left.
  const resumed = useRef(false);
  const resumeName = typeof search.name === "string" ? search.name : "";
  const resumeDays = LIFETIMES.find((d) => d === Number(search.days));
  const resuming =
    search.stepUp === PURPOSE && resumeName !== "" && resumeDays !== undefined;
  useEffect(() => {
    if (!resuming) resumed.current = false;
    if (resumed.current || !resuming || !summary.data) return;
    resumed.current = true;
    draft.current = { name: resumeName, days: resumeDays };
    const failed = search.stepUpResult === "failed";
    // Read once: a reload must not spend (or ask for) another proof.
    void navigate({ to: ".", search: {}, replace: true } as Parameters<
      typeof navigate
    >[0]);
    const providers = summary.data.linkedProviders;
    const fromProvider = async () => {
      if (failed) throw new Error("provider-failed");
      return (await service.claimProviderStepUp()).proof;
    };
    setFlow({ kind: "proof", factor: "provider", providers, error: null });
    void issue(fromProvider());
  }, [
    issue,
    navigate,
    resumeDays,
    resumeName,
    resuming,
    search.stepUpResult,
    service,
    summary.data,
  ]);

  const revoke = useCallback(
    async (token: AccountApiToken) => {
      setActionError(false);
      try {
        await service.revokeApiToken(workspaceId, token.id);
        showNotice(copy.revoked);
      } catch (error) {
        // Not found: it is already gone (another admin revoked it); show the list as it is.
        if (statusOf(error) === 404) showNotice(copy.gone);
        else setActionError(true);
      }
      await invalidate();
    },
    [copy.gone, copy.revoked, invalidate, service, showNotice, workspaceId],
  );

  return (
    <AccountTokensSurface
      copy={copy}
      locale={locale}
      section={section}
      denied={denied}
      now={now}
      flow={flow}
      busy={busy}
      loadingMore={loadingMore}
      notice={notice}
      actionError={actionError}
      onStart={() => {
        setActionError(false);
        setFlow({ kind: "form", name: "", days: 90, error: null });
      }}
      onSubmitForm={({ name, days }) => {
        draft.current = { name, days };
        void beginProof();
      }}
      onSubmitProof={submitProof}
      onProvider={startProvider}
      onCloseFlow={() => setFlow(null)}
      onRevoke={revoke}
      onLoadMore={() => void loadMore()}
      onRetry={() => void first.refetch()}
    />
  );
}
