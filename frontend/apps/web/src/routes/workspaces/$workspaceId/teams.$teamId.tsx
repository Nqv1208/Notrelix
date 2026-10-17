import { useCallback, useMemo } from "react";
import { useParams } from "@tanstack/react-router";
import { toast } from "sonner";
import type { TeamRole } from "@notrelix/features-workspace/core";
import {
  archiveLoss,
  formatCopy,
  useWorkspaceTeamsCopy,
  WorkspaceTeamSurface,
  type AddTeamMemberInput,
  type TeamCandidate,
} from "@notrelix/features-workspace/web";
import { useWorkspaceContext } from "@/providers/workspace-provider";
import { sectionState } from "@/shell/workspace/section-state";
import { useIdSet } from "@/shell/workspace/use-id-set";
import { useSetupFrame } from "@/shell/workspace/use-setup-frame";
import { useShellNavigation } from "@/shell/workspace/use-shell-navigation";
import { useTeamsExt } from "@/shell/workspace/use-teams-ext";
import {
  toastTeamFailure,
  useTeamHooks,
} from "@/shell/workspace/use-workspace-teams";
import { useWorkspaceViewer } from "@/shell/workspace/use-workspace-viewer";

/**
 * One Team (WX-TEAM-001): details, members and archive. Every write carries
 * the version of the Team read from the producer; a stale one (409/412) is
 * reported as a conflict and the Team is refreshed so the next attempt
 * carries the current version. Workspace admins and owners write; everyone
 * else reads (the producer authorizes each request).
 */
export function TeamPage() {
  const { workspaceId } = useWorkspaceContext();
  const { teamId } = useParams({ strict: false }) as { teamId: string };
  const copy = useWorkspaceTeamsCopy();
  const navigation = useShellNavigation();
  const hooks = useTeamHooks();
  const viewer = useWorkspaceViewer(workspaceId);
  const team = hooks.useTeam(workspaceId, teamId);
  const members = hooks.useTeamMembers(workspaceId, teamId);
  const mutations = hooks.useTeamMutations(workspaceId);
  const pending = useIdSet();
  const frame = useSetupFrame("teams");
  const ext = useTeamsExt(
    workspaceId,
    `${team.data?.version ?? 0}:${members.data?.length ?? 0}`,
  );
  const teamExt = ext?.teams.find((item) => item.id === teamId) ?? null;

  const version = team.data?.version ?? null;
  const name = team.data?.name ?? "";
  const saving =
    mutations.rename.isPending ||
    mutations.describe.isPending ||
    mutations.archive.isPending ||
    mutations.unarchive.isPending ||
    mutations.addMember.isPending ||
    mutations.changeRole.isPending ||
    mutations.removeMember.isPending;

  const refetch = useCallback(() => {
    void team.refetch();
    void members.refetch();
  }, [members, team]);

  const memberName = useCallback(
    (userId: string) =>
      members.data?.find((member) => member.userId === userId)?.displayName ??
      viewer.members.data?.find((member) => member.userId === userId)?.name ??
      ext?.directory.find((person) => person.id === userId)?.name ??
      "",
    [ext, members.data, viewer.members.data],
  );

  const fail = useCallback(
    (error: unknown) => void toastTeamFailure(error, copy),
    [copy],
  );

  const onRename = useCallback(
    (next: string) => {
      if (version === null) return;
      mutations.rename.mutate(
        { teamId, name: next, expectedVersion: version },
        {
          onSuccess: () =>
            toast.success(formatCopy(copy.toasts.renamed, { name: next })),
          onError: fail,
        },
      );
    },
    [copy, fail, mutations.rename, teamId, version],
  );

  const onDescribe = useCallback(
    (description: string | null) => {
      if (version === null) return;
      mutations.describe.mutate(
        { teamId, description, expectedVersion: version },
        {
          onSuccess: () =>
            toast.success(formatCopy(copy.toasts.described, { name })),
          onError: fail,
        },
      );
    },
    [copy, fail, mutations.describe, name, teamId, version],
  );

  const onArchive = useCallback(() => {
    if (version === null) return;
    mutations.archive.mutate(
      { teamId, expectedVersion: version },
      {
        onSuccess: () =>
          toast.success(formatCopy(copy.toasts.archived, { name })),
        onError: fail,
      },
    );
  }, [copy, fail, mutations.archive, name, teamId, version]);

  const onUnarchive = useCallback(() => {
    if (version === null) return;
    mutations.unarchive.mutate(
      { teamId, expectedVersion: version },
      {
        onSuccess: () =>
          toast.success(formatCopy(copy.toasts.unarchived, { name })),
        onError: fail,
      },
    );
  }, [copy, fail, mutations.unarchive, name, teamId, version]);

  const onAddMember = useCallback(
    ({ userId, role }: AddTeamMemberInput) => {
      if (version === null) return;
      mutations.addMember.mutate(
        { teamId, userId, role, expectedVersion: version },
        {
          onSuccess: () =>
            toast.success(
              formatCopy(copy.toasts.memberAdded, { name: memberName(userId) }),
            ),
          onError: fail,
        },
      );
    },
    [copy, fail, memberName, mutations.addMember, teamId, version],
  );

  const onChangeRole = useCallback(
    (userId: string, role: TeamRole) => {
      if (version === null) return;
      pending.add(userId);
      mutations.changeRole.mutate(
        { teamId, userId, role, expectedVersion: version },
        {
          onSuccess: () =>
            toast.success(
              formatCopy(copy.toasts.roleChanged, {
                name: memberName(userId),
                role: copy.roles[role],
              }),
            ),
          onError: fail,
          onSettled: () => pending.remove(userId),
        },
      );
    },
    [copy, fail, memberName, mutations.changeRole, pending, teamId, version],
  );

  const onRemove = useCallback(
    (userId: string) => {
      if (version === null) return;
      const removed = memberName(userId);
      pending.add(userId);
      mutations.removeMember.mutate(
        { teamId, userId, expectedVersion: version },
        {
          onSuccess: () =>
            toast.success(
              formatCopy(copy.toasts.memberRemoved, { name: removed }),
            ),
          onError: fail,
          onSettled: () => pending.remove(userId),
        },
      );
    },
    [copy, fail, memberName, mutations.removeMember, pending, teamId, version],
  );

  const candidates = useMemo<readonly TeamCandidate[]>(
    () => ext?.directory ?? [],
    [ext],
  );

  return (
    <WorkspaceTeamSurface
      copy={copy}
      frame={frame}
      ext={teamExt}
      lossFor={teamExt ? archiveLoss(copy, teamExt) : null}
      formatDay={dayOf}
      usageNavigation={{
        getHref: (usage) =>
          navigation.getHref(
            usage.kind === "space"
              ? { section: "space", spaceId: usage.id, tab: "members" }
              : { section: "board", boardId: usage.id },
          ),
        onNavigate: (usage) =>
          navigation.onNavigate(
            usage.kind === "space"
              ? { section: "space", spaceId: usage.id, tab: "members" }
              : { section: "board", boardId: usage.id },
          ),
      }}
      team={sectionState(team, team.data)}
      members={sectionState(members, members.data)}
      viewer={{ userId: viewer.userId, role: viewer.role }}
      candidates={candidates}
      pendingUserIds={pending.ids}
      saving={saving}
      onRename={onRename}
      onDescribe={onDescribe}
      onArchive={onArchive}
      onUnarchive={onUnarchive}
      onAddMember={onAddMember}
      onChangeRole={onChangeRole}
      onRemove={onRemove}
      onRetry={refetch}
      backNavigation={{
        getHref: () => navigation.getHref({ section: "teams" }),
        onNavigate: () => navigation.onNavigate({ section: "teams" }),
      }}
    />
  );
}

function dayOf(iso: string): string {
  const date = new Date(iso);
  const pad = (value: number) => String(value).padStart(2, "0");
  return `${pad(date.getDate())}/${pad(date.getMonth() + 1)}`;
}
