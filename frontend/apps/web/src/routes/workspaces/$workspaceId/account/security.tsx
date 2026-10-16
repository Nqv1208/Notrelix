import { useCallback, useMemo, useState } from "react";
import { useNavigate } from "@tanstack/react-router";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import {
  AccountSecuritySurface,
  useAccountSecurityCopy,
  type AccountSecurityData,
  type AccountSection,
} from "@notrelix/features-account";
import { useAppRuntime } from "@notrelix/runtime-web";
import {
  accountPagesKeys,
  useAccountPagesApi,
  useToast,
} from "./use-account-pages";

export function AccountSecurityPage() {
  const { api: runtimeClient } = useAppRuntime();
  const copy = useAccountSecurityCopy();
  const api = useAccountPagesApi();
  const queryClient = useQueryClient();
  const navigate = useNavigate();
  const [busy, setBusy] = useState(false);
  const [actionError, setActionError] = useState(false);
  const [notice, showNotice] = useToast();

  const security = useQuery({
    queryKey: accountPagesKeys.security,
    queryFn: () => api.getSecurity(),
    retry: false,
  });
  const details = useQuery({
    queryKey: accountPagesKeys.details,
    queryFn: () => api.getDetails(),
    retry: false,
  });

  const section = useMemo<AccountSection<AccountSecurityData>>(
    () =>
      security.data
        ? {
            status: "ready",
            data: {
              ssoProvider: security.data.sso?.provider ?? null,
              email: security.data.sso?.email ?? "",
              workspaceName: details.data?.workspaceName ?? "",
              twoFactorEnabled: security.data.twoFactorEnabled,
              backupCodesLeft: security.data.backupCodesLeft,
              sessions: security.data.sessions,
              now: security.data.serverTime,
            },
          }
        : security.isError
          ? { status: "error" }
          : { status: "loading" },
    [security.data, security.isError, details.data?.workspaceName],
  );

  const run = useCallback(
    async (action: () => Promise<unknown>, message: string) => {
      setBusy(true);
      setActionError(false);
      try {
        await action();
        await queryClient.invalidateQueries({
          queryKey: accountPagesKeys.security,
        });
        showNotice(message);
      } catch {
        setActionError(true);
      } finally {
        setBusy(false);
      }
    },
    [queryClient, showNotice],
  );

  const signOut = useCallback(
    async (sessionId: string) => {
      const session = security.data?.sessions.find((s) => s.id === sessionId);
      if (session?.lastActiveAt === null) {
        // This device: the sign-out of the app itself.
        setBusy(true);
        try {
          await runtimeClient.api.post(runtimeClient.endpoints.auth.logout, {});
          await navigate({ to: "/sign-in", replace: true });
        } catch {
          setActionError(true);
          setBusy(false);
        }
        return;
      }
      await run(() => api.revokeSession(sessionId), copy.sessions.signedOut);
    },
    [api, copy.sessions.signedOut, navigate, run, runtimeClient, security.data],
  );

  return (
    <AccountSecuritySurface
      copy={copy}
      security={section}
      busy={busy}
      actionError={actionError}
      notice={notice}
      passwordHref="/forgot-password"
      onSetTwoFactor={(enabled) =>
        void run(
          () => api.setTwoFactor(enabled),
          enabled ? copy.twoFactor.turnedOn : copy.twoFactor.turnedOff,
        )
      }
      onRenewBackupCodes={() =>
        void run(() => api.renewBackupCodes(), copy.twoFactor.renewed)
      }
      onSignOut={(id) => void signOut(id)}
      onSignOutOthers={() =>
        void run(() => api.revokeOtherSessions(), copy.others.done)
      }
      onRetry={() => void security.refetch()}
    />
  );
}
