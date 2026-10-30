import { useCallback, useMemo, useState } from "react";
import { useNavigate, useSearch } from "@tanstack/react-router";
import { toast } from "sonner";
import {
  isWorkspaceAdmin,
  pendingInvitationFor,
  type CreateWorkspaceInvitationInput,
  type WorkspaceFailure,
  type WorkspaceInvitation,
  type WorkspaceMember,
  type WorkspaceRole,
} from "@notrelix/features-workspace/core";
import {
  createUseChangeInvitationRole,
  createUseCreateInvitation,
  createUseMemberMutations,
  createUseResendInvitation,
  createUseRevokeInvitation,
  createUseWorkspaceInvitations,
  formatCopy,
  useWorkspaceMembersCopy,
  WorkspaceMembersSurface,
  type MemberWriteTarget,
  type MembersTab,
  type WorkspaceMembersCopy,
} from "@notrelix/features-workspace/web";
import {
  describeWorkspaceFailure,
  PENDING_INVITATION_EXISTS_CODE,
} from "@notrelix/features-workspace/core";
import { useFormatters } from "@notrelix/i18n";
import { useAppRuntime } from "@notrelix/runtime-web";
import { SeatInviteDialog } from "./billing/seat-invite";
import { useWorkspaceContext } from "@/providers/workspace-provider";
import { toastFailure } from "@/shell/workspace/failure-toast";
import { sectionState } from "@/shell/workspace/section-state";
import { useIdSet } from "@/shell/workspace/use-id-set";
import { useNow } from "@/shell/workspace/use-now";
import { useWorkspaceViewer } from "@/shell/workspace/use-workspace-viewer";

function recoveryHint(copy: WorkspaceMembersCopy) {
  return (failure: WorkspaceFailure): string => {
    switch (failure.kind) {
      case "forbidden":
        return copy.toasts.forbidden;
      case "conflict":
        return copy.toasts.conflict;
      case "not-found":
        return copy.toasts.notFound;
      case "network":
        return copy.toasts.network;
      default:
        return copy.toasts.unknown;
    }
  };
}

/**
 * Members and invitations (WX-MEM-001): composes the Workspaces member and
 * invitation producers into the pure surface; every outcome is reported with
 * the producer's own rule message when it sends one.
 */
export function MembersPage() {
  const { workspaceId, workspace } = useWorkspaceContext();
  const copy = useWorkspaceMembersCopy();
  const formatters = useFormatters();
  const now = useNow();
  const navigate = useNavigate();
  const search = useSearch({ strict: false }) as {
    tab?: MembersTab;
    invite?: "seats";
  };
  const tab: MembersTab =
    search.tab === "invitations" ? "invitations" : "members";
  const { api: runtimeClient } = useAppRuntime();

  const hooks = useMemo(() => {
    const deps = { api: runtimeClient.api, endpoints: runtimeClient.endpoints };
    return {
      useMemberMutations: createUseMemberMutations(deps),
      useInvitations: createUseWorkspaceInvitations(deps),
      useCreateInvitation: createUseCreateInvitation(deps),
      useRevokeInvitation: createUseRevokeInvitation(deps),
      useResendInvitation: createUseResendInvitation(deps),
      useChangeInvitationRole: createUseChangeInvitationRole(deps),
    };
  }, [runtimeClient]);

  const viewer = useWorkspaceViewer(workspaceId);
  const admin = isWorkspaceAdmin(viewer.role);
  const invitations = hooks.useInvitations(workspaceId, { enabled: admin });
  const mutations = hooks.useMemberMutations(workspaceId);
  const createInvitation = hooks.useCreateInvitation(workspaceId);
  const revokeInvitation = hooks.useRevokeInvitation(workspaceId);
  const resendInvitation = hooks.useResendInvitation(workspaceId);
  const changeInvitationRole = hooks.useChangeInvitationRole(workspaceId);
  const pendingMembers = useIdSet();
  const pendingInvitations = useIdSet();
  const [inviteError, setInviteError] = useState<string | null>(null);
  const [inviteResetKey, setInviteResetKey] = useState(0);
  const hint = useMemo(() => recoveryHint(copy), [copy]);

  const run = useCallback(
    (
      member: WorkspaceMember,
      action: (target: MemberWriteTarget) => Promise<unknown>,
      messages: { success: string; failure: string },
    ) => {
      pendingMembers.add(member.userId);
      action({ userId: member.userId, expectedVersion: member.version })
        .then(() =>
          toast.success(formatCopy(messages.success, { name: member.name })),
        )
        .catch((error: unknown) =>
          toastFailure(
            formatCopy(messages.failure, { name: member.name }),
            error,
            hint,
          ),
        )
        .finally(() => pendingMembers.remove(member.userId));
    },
    [hint, pendingMembers],
  );

  const onChangeRole = useCallback(
    (member: WorkspaceMember, role: WorkspaceRole) =>
      run(
        member,
        (target) => mutations.updateRole.mutateAsync({ ...target, role }),
        {
          success: formatCopy(copy.toasts.roleChanged, {
            name: "{{name}}",
            role: copy.roles[role],
          }),
          failure: copy.toasts.roleFailed,
        },
      ),
    [copy, mutations.updateRole, run],
  );

  const onInvite = useCallback(
    (input: CreateWorkspaceInvitationInput) => {
      setInviteError(null);
      // One pending invitation per address (the producer's rule): say so in
      // the member's language before sending. The producer still decides.
      const waiting = pendingInvitationFor(
        invitations.data ?? [],
        input.email,
        now,
      );
      if (waiting) {
        setInviteError(
          formatCopy(copy.invitations.alreadyPending, { email: waiting.email }),
        );
        return;
      }
      createInvitation.mutate(input, {
        onSuccess: () => {
          setInviteResetKey((key) => key + 1);
          toast.success(
            formatCopy(copy.toasts.invited, { email: input.email }),
          );
        },
        onError: (error) => {
          const failure = describeWorkspaceFailure(error);
          const fieldMessage =
            Object.values(failure.fieldErrors).flat()[0] ?? null;
          // Someone else invited this address a moment ago: the same words as the check before sending.
          if (failure.errorCode === PENDING_INVITATION_EXISTS_CODE) {
            setInviteError(
              formatCopy(copy.invitations.alreadyPending, {
                email: input.email,
              }),
            );
            return;
          }
          if (failure.kind === "validation" || failure.kind === "rule") {
            // Shown next to the field; the typed address is kept.
            setInviteError(
              fieldMessage ?? failure.detail ?? copy.toasts.unknown,
            );
            return;
          }
          toastFailure(
            formatCopy(copy.toasts.inviteFailed, { email: input.email }),
            error,
            hint,
          );
        },
      });
    },
    [copy, createInvitation, hint, invitations.data, now],
  );

  // Several addresses typed at once: sent one after the other, each decided
  // by the producer; what was not sent stays in the field to be fixed.
  const onInviteMany = useCallback(
    async (inputs: readonly CreateWorkspaceInvitationInput[]) => {
      setInviteError(null);
      const failed: string[] = [];
      let sent = 0;
      for (const input of inputs) {
        if (pendingInvitationFor(invitations.data ?? [], input.email, now)) {
          failed.push(input.email);
          continue;
        }
        try {
          await createInvitation.mutateAsync(input);
          sent += 1;
        } catch {
          failed.push(input.email);
        }
      }
      if (failed.length === 0) {
        setInviteResetKey((key) => key + 1);
        toast.success(formatCopy(copy.toasts.invitedMany, { count: sent }));
        return;
      }
      const emails = failed.join(", ");
      if (sent > 0)
        toast.success(
          formatCopy(copy.toasts.invitedSome, { count: sent, emails }),
        );
      setInviteError(formatCopy(copy.invitations.emailInvalidMany, { emails }));
    },
    [copy, createInvitation, invitations.data, now],
  );

  const onChangeInvitationRole = useCallback(
    (
      invitation: WorkspaceInvitation,
      role: CreateWorkspaceInvitationInput["role"],
    ) => {
      pendingInvitations.add(invitation.id);
      changeInvitationRole.mutate(
        {
          invitationId: invitation.id,
          role,
          expectedVersion: invitation.version,
        },
        {
          onSuccess: () =>
            toast.success(
              formatCopy(copy.toasts.invitationRoleChanged, {
                email: invitation.email,
                role: copy.roles[role],
              }),
            ),
          onError: (error) =>
            toastFailure(
              formatCopy(copy.toasts.invitationRoleFailed, {
                email: invitation.email,
              }),
              error,
              hint,
            ),
          onSettled: () => pendingInvitations.remove(invitation.id),
        },
      );
    },
    [changeInvitationRole, copy, hint, pendingInvitations],
  );

  const onResend = useCallback(
    (invitation: WorkspaceInvitation) => {
      pendingInvitations.add(invitation.id);
      resendInvitation.mutate(invitation.id, {
        onSuccess: () =>
          toast.success(
            formatCopy(copy.toasts.resent, { email: invitation.email }),
          ),
        onError: (error) =>
          toastFailure(
            formatCopy(copy.toasts.resendFailed, { email: invitation.email }),
            error,
            hint,
          ),
        onSettled: () => pendingInvitations.remove(invitation.id),
      });
    },
    [copy, hint, pendingInvitations, resendInvitation],
  );

  const onRevoke = useCallback(
    (invitation: WorkspaceInvitation) => {
      pendingInvitations.add(invitation.id);
      revokeInvitation.mutate(invitation.id, {
        onSuccess: () =>
          toast.success(
            formatCopy(copy.toasts.revoked, { email: invitation.email }),
          ),
        onError: (error) =>
          toastFailure(
            formatCopy(copy.toasts.revokeFailed, { email: invitation.email }),
            error,
            hint,
          ),
        onSettled: () => pendingInvitations.remove(invitation.id),
      });
    },
    [copy, hint, pendingInvitations, revokeInvitation],
  );

  return (
    <>
      <WorkspaceMembersSurface
        copy={copy}
        formatters={formatters}
        now={now}
        workspaceName={workspace?.name ?? null}
        viewer={{ userId: viewer.userId, role: viewer.role }}
        members={
          // A Guest reads only their own row; the list is closed to them.
          viewer.role === "guest"
            ? sectionState(
                viewer.mine,
                viewer.mine.data ? [viewer.mine.data] : undefined,
              )
            : sectionState(viewer.members, viewer.members.data)
        }
        invitations={admin ? sectionState(invitations, invitations.data) : null}
        tab={tab}
        onTabChange={(next) =>
          void navigate({
            to: "/workspaces/$workspaceId/members",
            params: { workspaceId },
            search: next === "invitations" ? { tab: "invitations" } : {},
            replace: true,
          } as never)
        }
        pendingMemberIds={pendingMembers.ids}
        pendingInvitationIds={pendingInvitations.ids}
        inviting={createInvitation.isPending}
        inviteError={inviteError}
        inviteResetKey={inviteResetKey}
        onChangeRole={onChangeRole}
        onRemove={(member) =>
          run(member, (target) => mutations.remove.mutateAsync(target), {
            success: copy.toasts.removed,
            failure: copy.toasts.removeFailed,
          })
        }
        onSuspend={(member) =>
          run(member, (target) => mutations.suspend.mutateAsync(target), {
            success: copy.toasts.suspended,
            failure: copy.toasts.suspendFailed,
          })
        }
        onActivate={(member) =>
          run(member, (target) => mutations.activate.mutateAsync(target), {
            success: copy.toasts.activated,
            failure: copy.toasts.activateFailed,
          })
        }
        onInvite={onInvite}
        onInviteMany={onInviteMany}
        onResend={onResend}
        onChangeInvitationRole={onChangeInvitationRole}
        onRevoke={onRevoke}
        onRetryMembers={() =>
          void (
            viewer.role === "guest" ? viewer.mine : viewer.members
          ).refetch()
        }
        onRetryInvitations={() => void invitations.refetch()}
      />
      <SeatInviteDialog
        open={admin && search.invite === "seats"}
        onClose={() =>
          void navigate({
            to: "/workspaces/$workspaceId/members",
            params: { workspaceId },
            search: {},
            replace: true,
          } as never)
        }
        createInvitation={createInvitation.mutateAsync}
      />
    </>
  );
}
