import { useQueries, useQuery } from "@tanstack/react-query";
import {
  createTeamsService,
  type TeamsEndpoints,
  type TeamsService,
} from "../../../core/api/teams.service";
import type { SpacesApiClient } from "../../../core/api/spaces.service";
import { toSpaceTeams } from "../../../core/model/spaces";
import { workspaceQueryKeys } from "../../../query/keys";

interface UseTeamsDeps {
  api: SpacesApiClient;
  endpoints: TeamsEndpoints;
}

/**
 * The ONE query behind `workspaceQueryKeys.teams(ws)`: every Team (active and
 * archived, with status and version). Each consumer derives its own view with
 * `select`; no hook may cache a different shape under this key.
 */
function teamsQuery(service: TeamsService, workspaceId: string) {
  return {
    queryKey: workspaceQueryKeys.teams(workspaceId),
    queryFn: () => service.list(workspaceId),
    staleTime: 60_000,
  };
}

/** Active and archived Teams of the workspace (WX-TEAM-001). */
export function createUseTeams({ api, endpoints }: UseTeamsDeps) {
  const service = createTeamsService(api, endpoints);

  return function useTeams(workspaceId: string, enabled = true) {
    return useQuery({
      ...teamsQuery(service, workspaceId),
      enabled: !!workspaceId && enabled,
    });
  };
}

/**
 * Active Teams of the workspace, offered as Space member subjects. Reads the
 * shared Teams query and narrows it with `select` (an archived Team is not a
 * valid Space subject).
 */
export function createUseWorkspaceTeams({ api, endpoints }: UseTeamsDeps) {
  const service = createTeamsService(api, endpoints);

  return function useWorkspaceTeams(workspaceId: string, enabled = true) {
    return useQuery({
      ...teamsQuery(service, workspaceId),
      enabled: !!workspaceId && enabled,
      select: toSpaceTeams,
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

/**
 * The members of several Teams at once (the expanded rows of a Space's member
 * list), one query per Team under the same key as {@link createUseTeamMembers}.
 */
export function createUseTeamsMembers({ api, endpoints }: UseTeamsDeps) {
  const service = createTeamsService(api, endpoints);

  return function useTeamsMembers(
    workspaceId: string,
    teamIds: readonly string[],
  ) {
    return useQueries({
      queries: teamIds.map((teamId) => ({
        queryKey: workspaceQueryKeys.teamMembers(workspaceId, teamId),
        queryFn: () => service.members(workspaceId, teamId),
        enabled: !!workspaceId,
        staleTime: 30_000,
      })),
    });
  };
}
