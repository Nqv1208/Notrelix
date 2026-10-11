import { useCallback, useState } from "react";
import { useNavigate, useSearch } from "@tanstack/react-router";
import { toast } from "sonner";
import {
  describeWorkspaceFailure,
  isWorkspaceAdmin,
} from "@notrelix/features-workspace/core";
import {
  CreateTeamDialog,
  formatCopy,
  useWorkspaceTeamsCopy,
  WorkspaceTeamsSurface,
  type CreateTeamValues,
  type TeamsFilter,
} from "@notrelix/features-workspace/web";
import { useWorkspaceContext } from "@/providers/workspace-provider";
import { sectionState } from "@/shell/workspace/section-state";
import { useShellNavigation } from "@/shell/workspace/use-shell-navigation";
import {
  teamRecoveryHint,
  toastTeamFailure,
  useTeamHooks,
} from "@/shell/workspace/use-workspace-teams";
import { useWorkspaceViewer } from "@/shell/workspace/use-workspace-viewer";

/**
 * The workspace's Teams (WX-TEAM-001): composes the Workspaces Team producer
 * into the pure list surface. Every member can read; only workspace admins
 * and owners create (the producer authorizes each write).
 */
export function TeamsPage() {
  const { workspaceId } = useWorkspaceContext();
  const copy = useWorkspaceTeamsCopy();
  const navigation = useShellNavigation();
  const navigate = useNavigate();
  const search = useSearch({ strict: false }) as { filter?: TeamsFilter };
  const filter: TeamsFilter =
    search.filter === "archived" ? "archived" : "active";

  const hooks = useTeamHooks();
  const teams = hooks.useTeams(workspaceId);
  const mutations = hooks.useTeamMutations(workspaceId);
  const viewer = useWorkspaceViewer(workspaceId);
  const canManage = isWorkspaceAdmin(viewer.role);

  const [createOpen, setCreateOpen] = useState(false);
  const [createError, setCreateError] = useState<string | null>(null);

  const onFilterChange = useCallback(
    (next: TeamsFilter) =>
      void navigate({
        to: "/workspaces/$workspaceId/teams",
        params: { workspaceId },
        search: next === "archived" ? { filter: "archived" } : {},
        replace: true,
      } as never),
    [navigate, workspaceId],
  );

  const onCreate = useCallback(
    (values: CreateTeamValues) => {
      setCreateError(null);
      mutations.create.mutate(values, {
        onSuccess: (teamId) => {
          setCreateOpen(false);
          toast.success(formatCopy(copy.toasts.created, { name: values.name }));
          if (typeof teamId === "string" && teamId !== "") {
            navigation.onNavigate({ section: "teams", teamId });
          }
        },
        onError: (error) => {
          const failure = describeWorkspaceFailure(error);
          if (failure.kind === "rule" || failure.kind === "validation") {
            // Shown next to the field; the typed values are kept.
            setCreateError(
              failure.detail ??
                Object.values(failure.fieldErrors).flat()[0] ??
                teamRecoveryHint(copy)(failure),
            );
            return;
          }
          toastTeamFailure(error, copy);
        },
      });
    },
    [copy, mutations.create, navigation],
  );

  return (
    <>
      <WorkspaceTeamsSurface
        copy={copy}
        teams={sectionState(teams, teams.data)}
        filter={filter}
        onFilterChange={onFilterChange}
        canManage={canManage}
        readOnlyReason={
          viewer.role !== null && !canManage ? copy.readOnly : null
        }
        navigation={{
          getHref: (teamId) => navigation.getHref({ section: "teams", teamId }),
          onNavigate: (teamId) =>
            navigation.onNavigate({ section: "teams", teamId }),
        }}
        onCreate={() => {
          setCreateError(null);
          setCreateOpen(true);
        }}
        onRetry={() => void teams.refetch()}
      />
      <CreateTeamDialog
        copy={copy}
        open={createOpen}
        pending={mutations.create.isPending}
        error={createError}
        onOpenChange={(open) => {
          setCreateOpen(open);
          if (!open) setCreateError(null);
        }}
        onSubmit={onCreate}
      />
    </>
  );
}
