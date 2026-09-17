import { useMemo, useState } from "react";
import { useNavigate } from "@tanstack/react-router";

import type {
  InvitationsEndpoints,
  WorkspaceApiClient,
  WorkspaceInvitation,
} from "../../core";
import { createUseAcceptInvitation } from "../hooks/mutations/use-accept-invitation";
import { createUsePendingInvitations } from "../query/hooks/use-pending-invitations";
import { PendingInvitationsMenuSurface } from "./workspace-pending-invitations-surface";

interface PendingInvitationsMenuProps {
  api: WorkspaceApiClient;
  endpoints: InvitationsEndpoints;
}

export function PendingInvitationsMenu({
  api,
  endpoints,
}: PendingInvitationsMenuProps) {
  const navigate = useNavigate();
  const [open, setOpen] = useState(false);
  const [acceptingInvitationId, setAcceptingInvitationId] = useState<
    string | null
  >(null);

  const usePendingInvitations = useMemo(
    () => createUsePendingInvitations({ api, endpoints }),
    [api, endpoints],
  );
  const useAcceptInvitation = useMemo(
    () => createUseAcceptInvitation({ api, endpoints }),
    [api, endpoints],
  );

  const { data: invitations = [], isLoading, refetch } =
    usePendingInvitations();
  const acceptMutation = useAcceptInvitation();

  const handleAccept = (invitation: WorkspaceInvitation) => {
    if (!invitation.token) return;

    setAcceptingInvitationId(invitation.id);
    acceptMutation.mutate(invitation.token, {
      onSuccess: () => {
        setOpen(false);
        setAcceptingInvitationId(null);
        void refetch();
        void navigate({ to: "/home" });
      },
      onError: () => {
        setAcceptingInvitationId(null);
      },
    });
  };

  return (
    <PendingInvitationsMenuSurface
      invitations={invitations}
      status={isLoading ? "loading" : "idle"}
      open={open}
      onOpenChange={setOpen}
      acceptingInvitationId={acceptingInvitationId}
      acceptDisabled={acceptMutation.isPending}
      onAccept={handleAccept}
    />
  );
}
