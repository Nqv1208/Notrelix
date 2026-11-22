import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  EMAIL_NOT_CONFIRMED_ERROR,
  readFlowError,
  type AuthFlowService,
  type InvitePreview,
} from "../../core";
import { authQueryKeys } from "../../core/query/keys";
import { useAuthCopy } from "../copy/use-auth-copy";
import {
  AuthInviteSurface,
  type AuthInviteView,
} from "../components/auth-invite-surface";
import type { AuthFlowNavigation } from "./flow-navigation";

/** Why an accept or decline did not go through, as the surface shows it. */
type Problem =
  | { readonly kind: "unreachable" }
  | { readonly kind: "emailNotConfirmed" }
  | { readonly kind: "emailMismatch" }
  | { readonly kind: "refused"; readonly detail: string | null };

const sameEmail = (a: string, b: string) =>
  a.trim().toLowerCase() === b.trim().toLowerCase();

/** "2027-01-31" as "31/01/2027". */
function formatDay(day: string | null): string | null {
  const match = day ? /^(\d{4})-(\d{2})-(\d{2})/.exec(day) : null;
  return match ? `${match[3]}/${match[2]}/${match[1]}` : null;
}

function inviteOf(preview: InvitePreview) {
  return {
    inviterName: preview.inviterName,
    inviterInitials: preview.inviterInitials,
    orgName: preview.orgName,
    boardTitle: preview.boardTitle,
    level: preview.level,
    until: formatDay(preview.expiresOn),
    email: preview.email ?? "",
    name: preview.name ?? "",
    hasAccount: preview.hasAccount,
    workspaceRole: preview.workspaceRole,
  };
}

/** A guest invitation: read it, accept it with a name and password, or decline it. */
export function InviteFlow({
  service,
  navigation,
  token,
  signedInEmail = null,
  onSwitchAccount,
}: {
  service: AuthFlowService;
  navigation: AuthFlowNavigation;
  token: string;
  /** The email of the account signed in now, if any; a different one than the invited email is named above the form. */
  signedInEmail?: string | null;
  /** Ends the session and comes back to this invitation. */
  onSwitchAccount?: () => void;
}) {
  const copy = useAuthCopy();
  const queryClient = useQueryClient();
  const [override, setOverride] = useState<AuthInviteView | null>(null);
  const [fieldErrors, setFieldErrors] = useState<{
    name?: string;
    password?: string;
  }>({});
  const [problem, setProblem] = useState<Problem | null>(null);

  const preview = useQuery({
    queryKey: ["auth", "invite", token],
    queryFn: () => service.getInvite(token),
    retry: false,
    staleTime: Infinity,
  });

  // The invitation is for another address than the account signed in now.
  const otherAccount =
    !!signedInEmail &&
    !!preview.data &&
    !sameEmail(signedInEmail, preview.data.email ?? "");

  /**
   * What the producer's refusal means for the person. Only a call that got no answer is "could not
   * reach"; every other refusal says what it can: confirm the email first, the wrong account, or the
   * producer's own line.
   */
  const explain = (failure: ReturnType<typeof readFlowError>): Problem => {
    if (failure.code === EMAIL_NOT_CONFIRMED_ERROR)
      return { kind: "emailNotConfirmed" };
    if (failure.network || failure.status === null)
      return { kind: "unreachable" };
    // A gateway or server error that carries no problem body (502, 503) says nothing about the invitation.
    if (failure.status >= 500 && !failure.code && !failure.detail)
      return { kind: "unreachable" };
    if (failure.status === 400 && otherAccount)
      return { kind: "emailMismatch" };
    return { kind: "refused", detail: failure.detail };
  };

  const resend = useMutation({
    mutationFn: () =>
      service.resendEmailVerification(
        signedInEmail ?? preview.data?.email ?? "",
      ),
  });

  const toSignIn = (redirectTo: string) =>
    navigation.navigate({
      to: "/sign-in",
      search: { redirect: redirectTo },
      replace: true,
    });

  const accept = useMutation({
    // An email that has an account signs in first, then accepts with that session.
    mutationFn: (input: { name: string; password: string }) =>
      preview.data?.hasAccount
        ? service.acceptInvite(token)
        : service.acceptInviteWithRegistration(token, input),
    onSuccess: (result) => {
      if (result.session)
        queryClient.setQueryData(authQueryKeys.profile, result.session.user);
      else
        void queryClient.invalidateQueries({ queryKey: authQueryKeys.profile });
      // A second press of the link opens no session: sign in, then land.
      if (result.alreadyAccepted && !result.session)
        toSignIn(result.redirectTo);
      else navigation.navigate({ to: result.redirectTo, replace: true });
    },
    onError: (error) => {
      const failure = readFlowError(error);
      setProblem(null);
      setFieldErrors({});
      // 409: the invited email already has an account. 401: no session for the authenticated accept.
      if (failure.status === 409 || failure.status === 401)
        toSignIn(`/invite/${token}`);
      else if (failure.status === 404)
        setOverride({ kind: "gone", reason: "missing", orgName: null });
      else if (
        failure.status === 400 &&
        (failure.fieldErrors.name || failure.fieldErrors.password)
      )
        setFieldErrors({
          name: failure.fieldErrors.name,
          password: failure.fieldErrors.password,
        });
      else if (failure.status === 400) refreshAfterRefusal(explain(failure));
      else setProblem(explain(failure));
    },
  });

  /** Expired, used or revoked in the meantime: the preview says which; a still valid one shows the reason. */
  const refreshAfterRefusal = (reason: Problem) => {
    void preview.refetch().then((again) => {
      if (again.data?.state === "valid") setProblem(reason);
    });
  };

  const decline = useMutation({
    mutationFn: () => service.declineInvite(token),
    onSuccess: () =>
      setOverride({
        kind: "declined",
        inviterName: preview.data?.inviterName ?? "",
      }),
    onError: (error) => {
      const failure = readFlowError(error);
      if (failure.status === 404)
        setOverride({ kind: "gone", reason: "missing", orgName: null });
      else if (failure.status === 400) refreshAfterRefusal(explain(failure));
      else setProblem(explain(failure));
    },
  });

  let view: AuthInviteView;
  if (override) view = override;
  else if (preview.isPending) return <p role="status">{copy.loading}</p>;
  else if (preview.isError && readFlowError(preview.error).status !== 404)
    // A server or network failure says nothing about the link: offer a retry, not "not found".
    view = { kind: "unreachable" };
  else if (preview.isError || !preview.data)
    view = { kind: "gone", reason: "missing", orgName: null };
  else if (preview.data.state !== "valid")
    view = {
      kind: "gone",
      reason: preview.data.state,
      orgName: preview.data.orgName,
    };
  else
    view = {
      kind: "invite",
      invite: inviteOf(preview.data),
      error: problem ? problem.kind : null,
      errorDetail: problem?.kind === "refused" ? problem.detail : null,
      fieldErrors,
    };

  const signedInAs =
    view.kind === "invite" &&
    signedInEmail &&
    !sameEmail(signedInEmail, view.invite.email)
      ? signedInEmail
      : null;

  return (
    <AuthInviteSurface
      copy={copy}
      view={view}
      signedInAs={signedInAs}
      onSwitchAccount={onSwitchAccount}
      pending={
        accept.isPending ? "accept" : decline.isPending ? "decline" : null
      }
      onAccept={(input) => accept.mutate(input)}
      onDecline={() => decline.mutate()}
      onRetry={() => void preview.refetch()}
      onResendConfirmation={() => resend.mutate()}
      resendState={
        resend.isPending
          ? "sending"
          : resend.isSuccess
            ? "sent"
            : resend.isError
              ? "failed"
              : "idle"
      }
      resendEmail={signedInEmail ?? preview.data?.email ?? undefined}
      onSignIn={() =>
        navigation.navigate({
          to: "/sign-in",
          search: { redirect: `/invite/${token}` },
        })
      }
      onToSignIn={() => navigation.navigate({ to: "/sign-in" })}
    />
  );
}
