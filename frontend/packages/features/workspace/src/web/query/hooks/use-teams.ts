import { useQuery } from "@tanstack/react-query";
import {
  createTeamsService,
  type TeamsEndpoints,
} from "../../../core/api/teams.service";
import type { SpacesApiClient } from "../../../core/api/spaces.service";
import { workspaceQueryKeys } from "../../../query/keys";

interface UseTeamsDeps {
  api: SpacesApiClient;
  endpoints: TeamsEndpoints;
}

/** Active and archived Teams of the workspace (WX-TEAM-001). */
export function createUseTeams({ api, endpoints }: UseTeamsDeps) {
  const service = createTeamsService(api, endpoints);

  return function useTeams(workspaceId: string, enabled = true) {
    return useQuery({
      queryKey: workspaceQueryKeys.teams(workspaceId),
      queryFn: () => service.list(workspaceId),
      enabled: !!workspaceId && enabled,
      staleTime: 60_000,
    });
  };
}

/** One Team, carrying the `version` echoed back as `expectedVersion`. */
export function createUseTeam({ api, endpoints }: UseTeamsDeps) {
  const service = createTeamsService(api, endpoints);

  return function useTeam(workspaceId: string, teamId: string | null) {
    return useQuery({
      queryKey: workspaceQueryKeys.team(workspaceId, teamId ?? ""),
      queryFn: () => service.get(workspaceId, teamId!),
      enabled: !!workspaceId && !!teamId,
      staleTime: 30_000,
    });
  };
}

/** The Team's members with their Team role. */
export function createUseTeamMembers({ api, endpoints }: UseTeamsDeps) {
  const service = createTeamsService(api, endpoints);

  return function useTeamMembers(workspaceId: string, teamId: string | null) {
    return useQuery({
      queryKey: workspaceQueryKeys.teamMembers(workspaceId, teamId ?? ""),
      queryFn: () => service.members(workspaceId, teamId!),
      enabled: !!workspaceId && !!teamId,
      staleTime: 30_000,
    });
  };
}
