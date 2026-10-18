import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
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
  };
}

/** A guest invitation: read it, accept it with a name and password, or decline it. */
export function InviteFlow({
  service,
  navigation,
  token,
}: {
  service: AuthFlowService;
  navigation: AuthFlowNavigation;
  token: string;
}) {
  const copy = useAuthCopy();
  const queryClient = useQueryClient();
  const [override, setOverride] = useState<AuthInviteView | null>(null);
  const [fieldErrors, setFieldErrors] = useState<{
    name?: string;
    password?: string;
  }>({});
  const [unreachable, setUnreachable] = useState(false);

  const preview = useQuery({
    queryKey: ["auth", "invite", token],
    queryFn: () => service.getInvite(token),
    retry: false,
    staleTime: Infinity,
  });

  const accept = useMutation({
    mutationFn: (input: { name: string; password: string }) =>
      service.acceptInvite(token, input),
    onSuccess: (result) => {
      void queryClient.invalidateQueries({ queryKey: authQueryKeys.profile });
      navigation.navigate({ to: result.redirectTo, replace: true });
    },
    onError: (error) => {
      const failure = readFlowError(error);
      setUnreachable(false);
      setFieldErrors({});
      if (failure.status === 410)
        setOverride({
          kind: "gone",
          reason: failure.code === "INVITE_EXPIRED" ? "expired" : "used",
          orgName: preview.data?.orgName ?? null,
        });
      else if (failure.status === 422)
        setFieldErrors({
          name: failure.fieldErrors.name,
          password: failure.fieldErrors.password,
        });
      else setUnreachable(true);
    },
  });

  const decline = useMutation({
    mutationFn: () => service.declineInvite(token),
    onSuccess: () =>
      setOverride({
        kind: "declined",
        inviterName: preview.data?.inviterName ?? "",
      }),
    onError: () => setUnreachable(true),
  });

  let view: AuthInviteView;
  if (override) view = override;
  else if (preview.isPending) return <p role="status">{copy.loading}</p>;
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
      error: unreachable ? "unreachable" : null,
      fieldErrors,
    };

  return (
    <AuthInviteSurface
      copy={copy}
      view={view}
      pending={
        accept.isPending ? "accept" : decline.isPending ? "decline" : null
      }
      onAccept={(input) => accept.mutate(input)}
      onDecline={() => decline.mutate()}
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
