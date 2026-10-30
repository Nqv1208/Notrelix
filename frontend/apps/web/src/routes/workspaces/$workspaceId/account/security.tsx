import { useCallback, useMemo, useState } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { useNavigate } from "@tanstack/react-router";
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
import { useRouteWorkspaceName, useToast } from "./use-account-pages";

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
  | { kind: "enroll"; methodId: string; secret: string; otpAuthUri: string }
  | {
      kind: "stepUp";
      purpose: "disable" | "renew" | "others";
      factor: "code" | "password";
      challengeToken: string | null;
    }
  | { kind: "codes"; codes: readonly string[] }
  | { kind: "password" }
  | { kind: "passwordChanged" }
  | { kind: "signOutThis" };

const PURPOSE: Record<"disable" | "renew" | "others", StepUpPurpose> = {
  disable: "DisableMfa",
  renew: "RegenerateRecoveryCodes",
  others: "RevokeOtherSessions",
};

export function AccountSecurityPage() {
  const { api: runtimeClient } = useAppRuntime();
  const copy = useAccountSecurityCopy();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const workspaceName = useRouteWorkspaceName();
  const [busy, setBusy] = useState(false);
  const [actionError, setActionError] = useState(false);
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

  // Turning two-factor on starts the enrollment; off asks for proof first.
  const beginStepUp = useCallback(
    async (purpose: "disable" | "renew" | "others") => {
      setActionError(false);
      setStepError(null);
      setBusy(true);
      try {
        const requirement = await service.getStepUpRequirement(
          PURPOSE[purpose],
        );
        setStep({
          kind: "stepUp",
          purpose,
          factor: requirement.factor === "mfa" ? "code" : "password",
          challengeToken: requirement.challengeToken,
        });
      } catch {
        setActionError(true);
      } finally {
        setBusy(false);
      }
    },
    [service],
  );

  const setTwoFactor = useCallback(
    async (enabled: boolean) => {
      if (!enabled) {
        await beginStepUp("disable");
        return;
      }
      setActionError(false);
      setStepError(null);
      // One start at a time: a second one would replace the secret the first one shows.
      setBusy(true);
      try {
        const enrollment = await service.startTwoFactor();
        setStep({ kind: "enroll", ...enrollment });
      } catch {
        setActionError(true);
      } finally {
        setBusy(false);
      }
    },
    [beginStepUp, service],
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
    ) => {
      setBusy(true);
      setStepError(null);
      try {
        const token = await proof;
        if (current.purpose === "disable") {
          await service.disableTwoFactor(token);
          setStep(null);
          await invalidate();
          showNotice(copy.twoFactor.turnedOff);
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
        }
      } catch (error) {
        // The proof or the challenge is gone (out of tries, too old): open a fresh one for the same step.
        if (
          (error as { code?: string }).code ===
          "identity.security.step-up-invalid"
        ) {
          await beginStepUp(current.purpose);
          setStepError({ error: "expired" });
        } else fail(error, true);
      } finally {
        setBusy(false);
      }
    },
    [
      beginStepUp,
      copy.others.doneCount_one,
      copy.others.doneCount_other,
      copy.others.nobody,
      copy.twoFactor.turnedOff,
      invalidate,
      service,
      showNotice,
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
          const codes = await service.verifyTwoFactor(
            step.methodId,
            code.replace(/\s+/g, ""),
          );
          setStep({ kind: "codes", codes });
          await invalidate();
          showNotice(copy.twoFactor.turnedOn);
        } catch (error) {
          fail(error, true);
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
      if (step?.kind === "stepUp")
        await finishStepUp(
          service.completeStepUpWithPassword(PURPOSE[step.purpose], password),
          step,
        );
    },
    [finishStepUp, service, step],
  );

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

  const flow = useMemo<AccountSecurityFlow | null>(() => {
    if (!step) return null;
    if (
      step.kind === "codes" ||
      step.kind === "passwordChanged" ||
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
          factor: step.factor,
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
      onSubmitNewPassword={(submission) => void submitNewPassword(submission)}
      onPasswordChangedContinue={() => void signOutThisDevice()}
      onSetTwoFactor={(enabled) => void setTwoFactor(enabled)}
      onSubmitCode={(code) => void submitCode(code)}
      onSubmitPassword={(password) => void submitPassword(password)}
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
