import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { useNavigate, useSearch } from "@tanstack/react-router";
import {
  AccountSecuritySurface,
  createUseProfile,
  createUseSecurity,
  passwordChangeFailureOf,
  pluralCopy,
  useAccountSecurityCopy,
  type AccountSecurityData,
  type AccountPasswordSubmission,
  type AccountSecurityFlow,
  type AccountSection,
  type StepUpPurpose,
} from "@notrelix/features-account";
import { useAppRuntime } from "@notrelix/runtime-web";
import { clearApplicationCache } from "@/composition/application-lifecycle";
import {
  useRouteWorkspaceId,
  useRouteWorkspaceName,
  useToast,
} from "./use-account-pages";

/** The seconds a throttled step must wait (429 `retryAfterSeconds`), or `null`. */
function waitOf(error: unknown): number | null {
  const e = error as {
    status?: number;
    details?: { retryAfterSeconds?: unknown };
  };
  if (e?.status !== 429) return null;
  const seconds = e.details?.retryAfterSeconds;
  return typeof seconds === "number" && seconds > 0 ? seconds : 1;
}

type Step =
  | {
      kind: "enroll";
      methodId: string;
      secret: string;
      otpAuthUri: string;
      /** The activation spends a proof of its own, bought with the same answer as the start. */
      verifyProof: string;
    }
  | {
      kind: "stepUp";
      purpose: StepPurpose;
      /** `link` and `unlink`: the provider the proof is for. */
      provider?: string;
      factor: "code" | "password" | "provider";
      /** `provider`: the linked providers the person can confirm with. */
      providers?: readonly string[];
      challengeToken: string | null;
    }
  | { kind: "codes"; codes: readonly string[]; othersEnded?: number }
  | { kind: "password" }
  /** A first password; `proof` is the single-use `ChangePassword` proof already bought. */
  | { kind: "setPassword"; proof: string }
  /** A new sign-in email; `proof` is the single-use `ChangeSecurityIdentity` proof already bought. */
  | { kind: "changeEmail"; proof: string }
  | { kind: "emailSent"; email: string }
  | { kind: "passwordChanged" }
  | { kind: "twoFactorOff" }
  | { kind: "signOutThis" };

type StepPurpose =
  | "enroll"
  | "disable"
  | "renew"
  | "others"
  | "link"
  | "unlink"
  | "setPassword"
  | "changeEmail";

const PURPOSE: Record<StepPurpose, StepUpPurpose> = {
  enroll: "EnrollMfa",
  disable: "DisableMfa",
  renew: "RegenerateRecoveryCodes",
  others: "RevokeOtherSessions",
  link: "LinkOAuth",
  unlink: "UnlinkOAuth",
  setPassword: "ChangePassword",
  changeEmail: "ChangeSecurityIdentity",
};

const LINK_RESULTS = ["linked", "already", "collision", "failed"] as const;

const named = (text: string, provider: string) =>
  text.replaceAll("{{provider}}", provider);

const isStepPurpose = (value: unknown): value is StepPurpose =>
  typeof value === "string" && value in PURPOSE;

export function AccountSecurityPage() {
  const { api: runtimeClient, env } = useAppRuntime();
  const workspaceId = useRouteWorkspaceId();
  const search = useSearch({ strict: false }) as {
    stepUp?: unknown;
    stepUpResult?: unknown;
    /** The provider a link or unlink step is for (back from the provider's confirmation). */
    provider?: unknown;
    /** How a link ended, said by the backend when the browser comes back. */
    link?: unknown;
  };
  const copy = useAccountSecurityCopy();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const workspaceName = useRouteWorkspaceName();
  const [busy, setBusy] = useState(false);
  const [actionError, setActionError] = useState(false);
  const [providerProblem, setProviderProblem] = useState<string | null>(null);
  const [notice, showNotice] = useToast();
  const [step, setStep] = useState<Step | null>(null);
  const [stepError, setStepError] = useState<{
    error: "wrong" | "wait" | "failed" | "expired";
    wait?: number;
  } | null>(null);
  const [passwordError, setPasswordError] = useState<{
    error: "wrongCurrent" | "wrongCode" | "rejected" | "wait" | "failed";
    wait?: number;
  } | null>(null);
  const [activityPages, setActivityPages] = useState<
    { items: AccountSecurityData["activity"]; next: string | null }[]
  >([]);

  const deps = useMemo(
    () => ({ api: runtimeClient.api, endpoints: runtimeClient.endpoints }),
    [runtimeClient],
  );
  const useProfile = useMemo(() => createUseProfile(deps), [deps]);
  const useSecurity = useMemo(() => createUseSecurity(deps), [deps]);
  const { profile } = useProfile();
  const { service, summary, sessions, activity, invalidate } = useSecurity();

  const activeSessions = useMemo(
    () => (sessions.data ?? []).filter((s) => s.status === "active"),
    [sessions.data],
  );
  const lines = useMemo(
    () => [
      ...(activity.data?.items ?? []),
      ...activityPages.flatMap((page) => page.items ?? []),
    ],
    [activity.data, activityPages],
  );
  const nextCursor =
    activityPages.length > 0
      ? (activityPages[activityPages.length - 1]?.next ?? null)
      : (activity.data?.nextCursor ?? null);

  const section = useMemo<AccountSection<AccountSecurityData>>(
    () =>
      summary.data && sessions.data
        ? {
            status: "ready",
            data: {
              // The producer has no "managed by a company" fact for the password.
              ssoProvider: null,
              hasPassword: summary.data.hasPassword,
              linkedProviders: summary.data.linkedProviders,
              email: profile?.email ?? "",
              workspaceName,
              twoFactorEnabled: summary.data.twoFactorEnabled,
              backupCodesLeft: summary.data.recoveryCodesLeft,
              sessions: activeSessions.map((s) => ({
                id: s.id,
                device: s.device,
                kind: s.kind,
                isCurrent: s.isCurrent,
                signedInAt: s.createdAt,
              })),
              now: summary.data.serverTime,
              activity: lines.map((l) => ({
                id: l.id,
                type: l.type,
                occurredAt: l.occurredAt,
                device: l.device,
                ipAddress: l.ipAddress,
              })),
              activityHasMore: nextCursor !== null,
            },
          }
        : summary.isError || sessions.isError
          ? { status: "error" }
          : { status: "loading" },
    [
      summary.data,
      summary.isError,
      sessions.data,
      sessions.isError,
      activeSessions,
      lines,
      nextCursor,
      profile?.email,
      workspaceName,
    ],
  );

  const signOutThisDevice = useCallback(async () => {
    // The session is over: drop what it read, so the sign-in page is not sent back here.
    clearApplicationCache(queryClient, { sessionEnded: true });
    await navigate({ to: "/sign-in", replace: true });
  }, [navigate, queryClient]);

  const signOut = useCallback(
    async (sessionId: string) => {
      setActionError(false);
      // The device in hand ends the session at once: ask first.
      if (activeSessions.find((s) => s.id === sessionId)?.isCurrent) {
        setStep({ kind: "signOutThis" });
        return;
      }
      setBusy(true);
      try {
        await service.revokeSession(sessionId);
        await invalidate();
        showNotice(copy.sessions.signedOut);
      } catch (error) {
        // Not found: that device is already gone (ended elsewhere); show the list as it is.
        if ((error as { status?: number }).status === 404) {
          await invalidate();
          showNotice(copy.sessions.gone);
        } else setActionError(true);
      } finally {
        setBusy(false);
      }
    },
    [
      activeSessions,
      copy.sessions.gone,
      copy.sessions.signedOut,
      invalidate,
      service,
      showNotice,
    ],
  );

  const confirmSignOutThis = useCallback(async () => {
    const currentId = activeSessions.find((s) => s.isCurrent)?.id;
    if (!currentId) {
      setStep(null);
      return;
    }
    setBusy(true);
    setActionError(false);
    try {
      await service.revokeSession(currentId);
      await signOutThisDevice();
    } catch {
      setActionError(true);
      setBusy(false);
    }
  }, [activeSessions, service, signOutThisDevice]);

  // Turning two-factor on or off asks for proof first.
  const beginStepUp = useCallback(
    async (purpose: StepPurpose, provider?: string) => {
      setActionError(false);
      setProviderProblem(null);
      setStepError(null);
      setBusy(true);
      try {
        const requirement = await service.getStepUpRequirement(
          PURPOSE[purpose],
        );
        setStep({
          kind: "stepUp",
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
        setActionError(true);
      } finally {
        setBusy(false);
      }
    },
    [service, summary.data?.linkedProviders],
  );

  const setTwoFactor = useCallback(
    async (enabled: boolean) => {
      // The proof comes first; the enrollment starts once it is in hand (`finishStepUp`).
      // One start at a time: the buttons wait while the proof is asked for.
      await beginStepUp(enabled ? "enroll" : "disable");
    },
    [beginStepUp],
  );

  const fail = (error: unknown, wrongStatus: boolean) => {
    const wait = waitOf(error);
    if (wait !== null) setStepError({ error: "wait", wait });
    else
      setStepError({
        error:
          wrongStatus && (error as { status?: number }).status !== undefined
            ? "wrong"
            : "failed",
      });
  };

  const finishStepUp = useCallback(
    async (
      proof: Promise<string>,
      current: Extract<Step, { kind: "stepUp" }>,
      /** Turning two-factor on spends two proofs (start, then activation): this buys the second. */
      spare?: () => Promise<string>,
    ) => {
      setBusy(true);
      setStepError(null);
      // Once the proof is in hand a refusal is the action's, never "that code is wrong".
      let proved = false;
      try {
        const token = await proof;
        const verifyProof = spare ? await spare() : "";
        proved = true;
        if (current.purpose === "enroll") {
          const enrollment = await service.startTwoFactor(token);
          setStep({ kind: "enroll", ...enrollment, verifyProof });
        } else if (current.purpose === "disable") {
          await service.disableTwoFactor(token);
          // The producer ended every session, this one too: nothing more can be read here.
          setStep({ kind: "twoFactorOff" });
        } else if (current.purpose === "link" && current.provider) {
          // The proof is spent by the link start: the browser leaves for the provider, and the
          // backend brings it back to this page with how it ended.
          const path = service.providerLinkPath(
            current.provider,
            token,
            `/workspaces/${workspaceId}/account/security`,
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
        } else if (current.purpose === "setPassword") {
          // The proof is kept for the form that follows; the password is not asked before the proof,
          // because the provider round trip would lose it.
          setPasswordError(null);
          setStep({ kind: "setPassword", proof: token });
        } else if (current.purpose === "changeEmail") {
          // Like the first password: the address is asked after the proof, so a provider round trip loses nothing.
          setPasswordError(null);
          setStep({ kind: "changeEmail", proof: token });
        } else if (current.purpose === "unlink" && current.provider) {
          try {
            await service.unlinkProvider(current.provider, token);
            setStep(null);
            await invalidate();
            showNotice(named(copy.providers.unlinked, current.provider));
          } catch (error) {
            // Refused (409): it is the last way to sign in; the proof is spent, so ask again next time.
            if ((error as { status?: number }).status !== 409) throw error;
            setStep(null);
            await invalidate();
            setProviderProblem(
              named(copy.providers.unlinkRefused, current.provider),
            );
          }
        } else if (current.purpose === "others") {
          const count = await service.revokeOtherSessions(token);
          setStep(null);
          await invalidate();
          showNotice(
            count > 0
              ? pluralCopy(
                  {
                    one: copy.others.doneCount_one,
                    other: copy.others.doneCount_other,
                  },
                  count,
                )
              : copy.others.nobody,
          );
        } else {
          setStep({
            kind: "codes",
            codes: await service.regenerateRecoveryCodes(token),
          });
          await invalidate();
          showNotice(copy.twoFactor.renewed);
        }
      } catch (error) {
        // The proof or the challenge is gone (out of tries, too old): open a fresh one for the same step.
        if (
          (error as { code?: string }).code ===
          "identity.security.step-up-invalid"
        ) {
          await beginStepUp(current.purpose, current.provider);
          setStepError({ error: "expired" });
        } else fail(error, !proved);
      } finally {
        setBusy(false);
      }
    },
    [
      beginStepUp,
      copy.others.doneCount_one,
      copy.others.doneCount_other,
      copy.others.nobody,
      copy.providers.unlinkRefused,
      copy.providers.unlinked,
      copy.twoFactor.renewed,
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
    async (code: string) => {
      if (!step) return;
      if (step.kind === "enroll") {
        setBusy(true);
        setStepError(null);
        try {
          // Authenticator apps group the digits ("123 456"); the producer wants six.
          const activation = await service.verifyTwoFactor(
            step.methodId,
            code.replace(/\s+/g, ""),
            step.verifyProof,
          );
          setStep({
            kind: "codes",
            codes: activation.recoveryCodes,
            othersEnded: activation.otherSessionsEnded,
          });
          await invalidate();
          showNotice(copy.twoFactor.turnedOn);
        } catch (error) {
          // The activation's proof is gone (too old): ask again, then start a fresh enrollment.
          if (
            (error as { code?: string }).code ===
            "identity.security.step-up-invalid"
          ) {
            await beginStepUp("enroll");
            setStepError({ error: "expired" });
          } else fail(error, true);
        } finally {
          setBusy(false);
        }
        return;
      }
      if (step.kind === "stepUp" && step.challengeToken)
        await finishStepUp(
          service.completeStepUpWithCode(
            PURPOSE[step.purpose],
            step.challengeToken,
            code,
          ),
          step,
        );
    },
    [
      beginStepUp,
      copy.twoFactor.turnedOn,
      finishStepUp,
      invalidate,
      service,
      showNotice,
      step,
    ],
  );

  const submitPassword = useCallback(
    async (password: string) => {
      if (step?.kind !== "stepUp") return;
      const proofFor = () =>
        service.completeStepUpWithPassword(PURPOSE[step.purpose], password);
      await finishStepUp(
        proofFor(),
        step,
        step.purpose === "enroll" ? proofFor : undefined,
      );
    },
    [finishStepUp, service, step],
  );

  // The person confirms with the provider they signed in with: the browser leaves for it and comes
  // back to this page with `stepUp=<purpose>`, the backend's proof waiting in its httpOnly cookie.
  const startProviderStepUp = useCallback(
    (provider: string) => {
      if (step?.kind !== "stepUp") return;
      const path = service.providerStepUpPath(
        provider,
        PURPOSE[step.purpose],
        `/workspaces/${workspaceId}/account/security?stepUp=${step.purpose}${step.provider ? `&provider=${encodeURIComponent(step.provider)}` : ""}`,
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
  const resumePurpose = isStepPurpose(search.stepUp) ? search.stepUp : null;
  useEffect(() => {
    // The marker is stripped once read, so the next return (the mock world stays in the page) resumes again.
    if (resumePurpose === null) resumed.current = false;
    if (resumed.current || resumePurpose === null || !summary.data) return;
    resumed.current = true;
    const failed = search.stepUpResult === "failed";
    // The marker is read once: a reload must not spend (or ask for) another proof.
    void navigate({
      to: ".",
      search: {},
      replace: true,
    } as Parameters<typeof navigate>[0]);
    const current: Extract<Step, { kind: "stepUp" }> = {
      kind: "stepUp",
      purpose: resumePurpose,
      provider:
        typeof search.provider === "string" ? search.provider : undefined,
      factor: "provider",
      providers: summary.data.linkedProviders,
      challengeToken: null,
    };
    const fromProvider = async () => {
      if (failed) throw new Error("provider-failed");
      return service.claimProviderStepUp();
    };
    setBusy(true);
    void fromProvider()
      .then(({ proof, spare }) =>
        finishStepUp(
          Promise.resolve(proof),
          current,
          resumePurpose === "enroll" ? () => Promise.resolve(spare) : undefined,
        ),
      )
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
    void navigate({
      to: ".",
      search: {},
      replace: true,
    } as Parameters<typeof navigate>[0]);
    // A refusal is a sentence too long for the toast: it stays in the block until the next try.
    if (linkResult === "linked" || linkResult === "already")
      showNotice(named(copy.providers[linkResult], provider));
    else setProviderProblem(named(copy.providers[linkResult], provider));
    if (linkResult === "linked") void invalidate();
  }, [
    copy.providers,
    invalidate,
    linkResult,
    navigate,
    search.provider,
    showNotice,
  ]);

  const startPasswordChange = useCallback(() => {
    setActionError(false);
    setStepError(null);
    setPasswordError(null);
    setStep({ kind: "password" });
  }, []);

  // Changing the password: with two-factor on, a second-factor proof first
  // (its single-use token rides with the change), then the change itself. The
  // producer ends every session when it succeeds, this one included.
  const submitNewPassword = useCallback(
    async ({ current, next, code }: AccountPasswordSubmission) => {
      setBusy(true);
      setPasswordError(null);
      let stage: "proof" | "change" = "proof";
      try {
        let stepUpToken: string | undefined;
        if (summary.data?.twoFactorEnabled) {
          const requirement =
            await service.getStepUpRequirement("ChangePassword");
          stepUpToken =
            requirement.factor === "mfa" && requirement.challengeToken
              ? await service.completeStepUpWithCode(
                  "ChangePassword",
                  requirement.challengeToken,
                  code,
                )
              : await service.completeStepUpWithPassword(
                  "ChangePassword",
                  current,
                );
        }
        stage = "change";
        await service.changePassword({
          currentPassword: current,
          newPassword: next,
          stepUpToken,
        });
        setStep({ kind: "passwordChanged" });
      } catch (error) {
        const failure = passwordChangeFailureOf(error, stage);
        setPasswordError(
          failure.kind === "wait"
            ? { error: "wait", wait: failure.seconds }
            : {
                error:
                  failure.kind === "wrong-current"
                    ? "wrongCurrent"
                    : failure.kind === "wrong-code"
                      ? "wrongCode"
                      : failure.kind,
              },
        );
      } finally {
        setBusy(false);
      }
    },
    [service, summary.data?.twoFactorEnabled],
  );

  const submitFirstPassword = useCallback(
    async (next: string) => {
      if (step?.kind !== "setPassword") return;
      setBusy(true);
      setPasswordError(null);
      try {
        await service.setFirstPassword({
          newPassword: next,
          stepUpToken: step.proof,
        });
        setStep(null);
        await invalidate();
        showNotice(copy.firstPassword.done);
      } catch (error) {
        const code = (error as { code?: string }).code;
        if (code === "identity.security.step-up-invalid") {
          // The proof is gone (too old): ask again for the same step.
          await beginStepUp("setPassword");
          setStepError({ error: "expired" });
        } else if (code === "identity.auth.password-already-set") {
          setStep(null);
          await invalidate();
          setProviderProblem(copy.firstPassword.already);
        } else {
          const failure = passwordChangeFailureOf(error, "change");
          setPasswordError(
            failure.kind === "wait"
              ? { error: "wait", wait: failure.seconds }
              : {
                  error: failure.kind === "rejected" ? "rejected" : "failed",
                },
          );
        }
      } finally {
        setBusy(false);
      }
    },
    [
      beginStepUp,
      copy.firstPassword.already,
      copy.firstPassword.done,
      invalidate,
      service,
      showNotice,
      step,
    ],
  );

  const submitEmail = useCallback(
    async (email: string) => {
      if (step?.kind !== "changeEmail") return;
      setBusy(true);
      setPasswordError(null);
      try {
        const answer = await service.requestEmailChange({
          email,
          stepUpToken: step.proof,
        });
        // The same answer for any address; null = it already is the sign-in email.
        if (answer.pendingEmail === null) setStep(null);
        else setStep({ kind: "emailSent", email: answer.pendingEmail });
      } catch (error) {
        if (
          (error as { code?: string }).code ===
          "identity.security.step-up-invalid"
        ) {
          // The proof is gone (too old): ask again for the same step.
          await beginStepUp("changeEmail");
          setStepError({ error: "expired" });
        } else {
          const seconds = waitOf(error);
          setPasswordError(
            seconds !== null
              ? { error: "wait", wait: seconds }
              : { error: "failed" },
          );
        }
      } finally {
        setBusy(false);
      }
    },
    [beginStepUp, service, step],
  );

  const flow = useMemo<AccountSecurityFlow | null>(() => {
    if (!step) return null;
    if (step.kind === "changeEmail")
      return {
        kind: "changeEmail",
        error:
          passwordError?.error === "wait"
            ? "wait"
            : passwordError
              ? "failed"
              : null,
        wait: passwordError?.wait,
      };
    if (step.kind === "emailSent") return step;
    if (step.kind === "setPassword")
      return {
        kind: "setPassword",
        error: passwordError?.error ?? null,
        wait: passwordError?.wait,
      };
    if (
      step.kind === "codes" ||
      step.kind === "passwordChanged" ||
      step.kind === "twoFactorOff" ||
      step.kind === "signOutThis"
    )
      return step;
    if (step.kind === "password")
      return {
        kind: "password",
        needsCode: summary.data?.twoFactorEnabled ?? false,
        error: passwordError?.error ?? null,
        wait: passwordError?.wait,
      };
    const error = stepError?.error ?? null;
    return step.kind === "enroll"
      ? {
          kind: "enroll",
          secret: step.secret,
          otpAuthUri: step.otpAuthUri,
          error,
          wait: stepError?.wait,
        }
      : {
          kind: "stepUp",
          purpose: step.purpose,
          provider: step.provider,
          factor: step.factor,
          providers: step.providers,
          error,
          wait: stepError?.wait,
        };
  }, [step, stepError, passwordError, summary.data?.twoFactorEnabled]);

  const loadMoreActivity = useCallback(async () => {
    if (!nextCursor) return;
    try {
      const page = await service.listActivity(nextCursor);
      setActivityPages((current) => [
        ...current,
        { items: page.items, next: page.nextCursor },
      ]);
    } catch {
      setActionError(true);
    }
  }, [nextCursor, service]);

  return (
    <AccountSecuritySurface
      copy={copy}
      security={section}
      busy={busy}
      actionError={actionError}
      notice={notice}
      flow={flow}
      onChangePassword={startPasswordChange}
      onSetPassword={() => void beginStepUp("setPassword")}
      onSubmitFirstPassword={(next) => void submitFirstPassword(next)}
      onChangeEmail={() => void beginStepUp("changeEmail")}
      onSubmitEmail={(email) => void submitEmail(email)}
      onSubmitNewPassword={(submission) => void submitNewPassword(submission)}
      onPasswordChangedContinue={() => void signOutThisDevice()}
      onSetTwoFactor={(enabled) => void setTwoFactor(enabled)}
      onSubmitCode={(code) => void submitCode(code)}
      onSubmitPassword={(password) => void submitPassword(password)}
      onStartProviderStepUp={startProviderStepUp}
      providerProblem={providerProblem}
      onLinkProvider={(provider) => void beginStepUp("link", provider)}
      onUnlinkProvider={(provider) => void beginStepUp("unlink", provider)}
      onCloseFlow={() => {
        setStep(null);
        setStepError(null);
        setPasswordError(null);
      }}
      onLoadMoreActivity={() => void loadMoreActivity()}
      onRenewBackupCodes={() => void beginStepUp("renew")}
      onSignOut={(id) => void signOut(id)}
      onSignOutOthers={() => void beginStepUp("others")}
      onConfirmSignOutThis={() => void confirmSignOutThis()}
      onRetry={() => {
        void summary.refetch();
        void sessions.refetch();
      }}
    />
  );
}
