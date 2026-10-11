import { useMutation, useQueryClient } from "@tanstack/react-query";
import type { SpacesApiClient } from "../../../core/api/spaces.service";
import {
  createTeamsService,
  type TeamsEndpoints,
} from "../../../core/api/teams.service";
import type { TeamMember, TeamRole } from "../../../core/model/teams";
import { workspaceQueryKeys } from "../../../query/keys";

interface TeamMutationDeps {
  api: SpacesApiClient;
  endpoints: TeamsEndpoints;
}

/** Every Team write is scoped to a Team and carries the version read for it. */
export interface TeamWrite {
  readonly teamId: string;
  /** The Team version read from the producer (`expectedVersion`). */
  readonly expectedVersion: number;
}

export interface CreateTeamInput {
  readonly name: string;
  readonly description: string | null;
}

export interface TeamMemberWrite extends TeamWrite {
  readonly userId: string;
}

/**
 * Team writes (WX-TEAM-001). Only a role change is optimistic: it is a
 * reversible local edit of the cached member list, rolled back when the
 * producer rejects it. Everything else waits for the producer. No write is
 * retried automatically; a stale version (409/412) surfaces as a conflict
 * and every write refreshes the Team, its members and the Team list, so the
 * next attempt carries the current version.
 */
export function createUseTeamMutations({ api, endpoints }: TeamMutationDeps) {
  const service = createTeamsService(api, endpoints);

  return function useTeamMutations(workspaceId: string) {
    const queryClient = useQueryClient();
    // `teams(ws)` is a prefix of `team(ws, id)` and `teamMembers(ws, id)`, so
    // one invalidation covers the list, the Team and its members.
    const refresh = () =>
      queryClient.invalidateQueries({
        queryKey: workspaceQueryKeys.teams(workspaceId),
      });

    const create = useMutation({
      mutationFn: (input: CreateTeamInput) =>
        service.create(workspaceId, input),
      retry: false,
      onSettled: refresh,
    });

    const rename = useMutation({
      mutationFn: ({ teamId, ...input }: TeamWrite & { name: string }) =>
        service.rename(workspaceId, teamId, input),
      retry: false,
      onSettled: refresh,
    });

    const describe = useMutation({
      mutationFn: ({
        teamId,
        ...input
      }: TeamWrite & { description: string | null }) =>
        service.describe(workspaceId, teamId, input),
      retry: false,
      onSettled: refresh,
    });

    const archive = useMutation({
      mutationFn: ({ teamId, ...input }: TeamWrite) =>
        service.archive(workspaceId, teamId, input),
      retry: false,
      onSettled: refresh,
    });

    const unarchive = useMutation({
      mutationFn: ({ teamId, ...input }: TeamWrite) =>
        service.unarchive(workspaceId, teamId, input),
      retry: false,
      onSettled: refresh,
    });

    const addMember = useMutation({
      mutationFn: ({
        teamId,
        ...input
      }: TeamMemberWrite & { role: TeamRole }) =>
        service.addMember(workspaceId, teamId, input),
      retry: false,
      onSettled: refresh,
    });

    const removeMember = useMutation({
      mutationFn: ({ teamId, userId, ...input }: TeamMemberWrite) =>
        service.removeMember(workspaceId, teamId, userId, input),
      retry: false,
      onSettled: refresh,
    });

    const changeRole = useMutation({
      mutationFn: ({
        teamId,
        userId,
        role,
        expectedVersion,
      }: TeamMemberWrite & { role: TeamRole }) =>
        service.changeMemberRole(workspaceId, teamId, userId, {
          newRole: role,
          expectedVersion,
        }),
      retry: false,
      onMutate: async ({ teamId, userId, role }) => {
        const key = workspaceQueryKeys.teamMembers(workspaceId, teamId);
        await queryClient.cancelQueries({ queryKey: key });
        const previous = queryClient.getQueryData<TeamMember[]>(key);
        if (previous) {
          queryClient.setQueryData<TeamMember[]>(
            key,
            previous.map((member) =>
              member.userId === userId ? { ...member, role } : member,
            ),
          );
        }
        return { key, previous };
      },
      onError: (_error, _input, context) => {
        if (context?.previous) {
          queryClient.setQueryData(context.key, context.previous);
        }
      },
      onSettled: refresh,
    });

    return {
      create,
      rename,
      describe,
      archive,
      unarchive,
      addMember,
      removeMember,
      changeRole,
    };
  };
}
