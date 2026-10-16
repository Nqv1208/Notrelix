import { useCallback, useMemo, useState } from "react";
import { toast } from "sonner";
import {
  describeWorkspaceFailure,
  isWorkspaceAdmin,
} from "@notrelix/features-workspace/core";
import {
  archiveLoss,
  buildTeamRows,
  formatCopy,
  useWorkspaceTeamsCopy,
  WorkspaceTeamsSurface,
  type CreateTeamValues,
  type TeamCandidate,
} from "@notrelix/features-workspace/web";
import { useWorkspaceContext } from "@/providers/workspace-provider";
import { sectionState } from "@/shell/workspace/section-state";
import { useIdSet } from "@/shell/workspace/use-id-set";
import { useSetupFrame } from "@/shell/workspace/use-setup-frame";
import { useShellNavigation } from "@/shell/workspace/use-shell-navigation";
import { useTeamsExt } from "@/shell/workspace/use-teams-ext";
import {
  teamRecoveryHint,
  toastTeamFailure,
  useTeamHooks,
} from "@/shell/workspace/use-workspace-teams";
import { useWorkspaceViewer } from "@/shell/workspace/use-workspace-viewer";

function dayOf(iso: string): string {
  const date = new Date(iso);
  const pad = (value: number) => String(value).padStart(2, "0");
  return `${pad(date.getDate())}/${pad(date.getMonth() + 1)}`;
}

/**
 * The workspace's Teams (WX-TEAM-001) inside "Thiết lập workspace": composes
 * the Workspaces Team producer and its extras into the list surface. Every
 * member can open the page; only workspace admins and owners create, archive
 * and restore (the producer authorizes each write), and others are told whom
 * to ask.
 */
export function TeamsPage() {
  const { workspaceId } = useWorkspaceContext();
  const copy = useWorkspaceTeamsCopy();
  const navigation = useShellNavigation();
  const frame = useSetupFrame("teams");
  const hooks = useTeamHooks();
  const teams = hooks.useTeams(workspaceId);
  const mutations = hooks.useTeamMutations(workspaceId);
  const viewer = useWorkspaceViewer(workspaceId);
  const canManage = isWorkspaceAdmin(viewer.role);
  const pending = useIdSet();

  const revision = (teams.data ?? [])
    .map((team) => `${team.id}:${team.version}`)
    .join(",");
  const ext = useTeamsExt(workspaceId, revision);

  const [creating, setCreating] = useState(false);
  const [createError, setCreateError] = useState<string | null>(null);
  const [createKey, setCreateKey] = useState(0);

  const rows = useMemo(
    () =>
      teams.data
        ? buildTeamRows(
            copy,
            // The active Teams first, then the archived ones.
            [
              ...teams.data.filter((team) => team.status === "Active"),
              ...teams.data.filter((team) => team.status === "Archived"),
            ],
            ext?.teams ?? null,
            dayOf,
          )
        : null,
    [copy, ext, teams.data],
  );

  const candidates = useMemo<readonly TeamCandidate[]>(
    () => ext?.directory ?? [],
    [ext],
  );

  // Who keeps a department when a Team goes: the people added to it directly.
  const lossFor = useCallback(
    (teamId: string) => {
      const info = ext?.teams.find((team) => team.id === teamId);
      return info ? archiveLoss(copy, info) : null;
    },
    [copy, ext],
  );

  const onCreate = useCallback(
    async (values: CreateTeamValues) => {
      setCreateError(null);
      setCreating(true);
      try {
        // The producer makes the creator the first Lead; the chosen lead takes
        // over, the creator leaves and the others join.
        const teamId = await mutations.create.mutateAsync({
          name: values.name,
          description: values.description,
        });
        if (typeof teamId !== "string" || teamId === "") return;
        let version = 1;
        const add = async (userId: string, role: "Lead" | "Member") => {
          await mutations.addMember.mutateAsync({
            teamId,
            userId,
            role,
            expectedVersion: version,
          });
          version += 1;
        };
        if (viewer.userId !== values.leadId) {
          await add(values.leadId, "Lead");
          if (viewer.userId) {
            await mutations.removeMember.mutateAsync({
              teamId,
              userId: viewer.userId,
              expectedVersion: version,
            });
            version += 1;
          }
        }
        for (const userId of values.memberIds) {
          if (userId !== values.leadId && userId !== viewer.userId)
            await add(userId, "Member");
        }
        toast.success(formatCopy(copy.toasts.created, { name: values.name }));
        setCreateKey((key) => key + 1);
        navigation.onNavigate({ section: "teams", teamId });
      } catch (error) {
        const failure = describeWorkspaceFailure(error);
        if (failure.kind === "rule" || failure.kind === "validation") {
          setCreateError(
            failure.detail ??
              Object.values(failure.fieldErrors).flat()[0] ??
              teamRecoveryHint(copy)(failure),
          );
        } else {
          toastTeamFailure(error, copy);
        }
      } finally {
        setCreating(false);
      }
    },
    [copy, mutations, navigation, viewer.userId],
  );

  const write = useCallback(
    (teamId: string, run: () => Promise<unknown>, success: string) => {
      pending.add(teamId);
      run()
        .then(() => toast.success(success))
        .catch((error: unknown) => toastTeamFailure(error, copy))
        .finally(() => pending.remove(teamId));
    },
    [copy, pending],
  );

  const versionOf = (teamId: string) =>
    teams.data?.find((team) => team.id === teamId)?.version ?? 0;
  const nameOf = (teamId: string) =>
    teams.data?.find((team) => team.id === teamId)?.name ?? "";

  return (
    <WorkspaceTeamsSurface
      key={createKey}
      copy={copy}
      frame={frame}
      teams={sectionState(teams, rows ?? undefined)}
      canManage={canManage}
      admins={ext?.admins ?? []}
      navigation={navigation}
      candidates={candidates}
      creating={creating}
      createError={createError}
      pendingIds={pending.ids}
      lossFor={lossFor}
      formatDay={dayOf}
      onCreate={(values) => void onCreate(values)}
      onArchive={(teamId) =>
        write(
          teamId,
          () =>
            mutations.archive.mutateAsync({
              teamId,
              expectedVersion: versionOf(teamId),
            }),
          formatCopy(copy.toasts.archived, { name: nameOf(teamId) }),
        )
      }
      onRestore={(teamId) =>
        write(
          teamId,
          () =>
            mutations.unarchive.mutateAsync({
              teamId,
              expectedVersion: versionOf(teamId),
            }),
          formatCopy(copy.toasts.unarchived, { name: nameOf(teamId) }),
        )
      }
      onRetry={() => void teams.refetch()}
    />
  );
}
