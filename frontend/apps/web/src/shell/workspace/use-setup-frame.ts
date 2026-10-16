import { useMemo } from "react";
import type {
  SetupFrameProps,
  SetupSection,
} from "@notrelix/features-workspace/web";
import { buildSpaceIndex } from "@notrelix/features-workspace/core";
import { useWorkspaceContext } from "@/providers/workspace-provider";
import { useShellNavigation } from "./use-shell-navigation";
import { useTeamHooks } from "./use-workspace-teams";
import { useSpaceHooks } from "./use-workspace-spaces";

/**
 * The frame of "Thiết lập workspace" for a page: the workspace, the active
 * section, the counts beside the sections and where each section lives.
 * General, Members and Plans and billing point at the pages that exist;
 * Departments and Integrations have none yet and are drawn inert.
 */
export function useSetupFrame(active: SetupSection): SetupFrameProps {
  const { workspaceId, workspace } = useWorkspaceContext();
  const navigation = useShellNavigation();
  const teams = useTeamHooks().useTeams(workspaceId);
  const tree = useSpaceHooks().useSpaceTree(workspaceId);
  return useMemo(() => {
    const link = (
      destination: Parameters<typeof navigation.getHref>[0],
    ): SetupFrameProps["links"][SetupSection] => ({
      href: navigation.getHref(destination),
      onNavigate: () => navigation.onNavigate(destination),
    });
    const departments = (
      buildSpaceIndex(tree.data ?? []).children.get(null) ?? []
    ).filter((space) => !space.isArchived).length;
    return {
      workspaceId,
      workspaceName: workspace?.name ?? "",
      active,
      counts: {
        members: workspace?.memberCount ?? null,
        teams: teams.data
          ? teams.data.filter((team) => team.status === "Active").length
          : null,
        departments: tree.data ? departments : null,
      },
      links: {
        general: link({ section: "settings" }),
        members: link({ section: "members" }),
        teams: link({ section: "teams" }),
        departments: null,
        sharing: link({ section: "security" }),
        integrations: null,
        billing: link({ section: "billing" }),
      },
    };
  }, [active, navigation, teams.data, tree.data, workspace, workspaceId]);
}
