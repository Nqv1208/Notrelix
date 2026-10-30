import type {
  OperationRequestBody,
  OperationResponse,
} from "@notrelix/contracts";
import {
  parseTeam,
  parseTeamMember,
  type Team,
  type TeamMember,
  type TeamRole,
} from "../model/teams";
import type { SpacesApiClient } from "./spaces.service";

type CreateTeamBody = OperationRequestBody<"Workspaces.Teams.CreateTeam">;
type TeamPage = OperationResponse<"Workspaces.Teams.GetWorkspaceTeams", 200>;
type RenameTeamBody = OperationRequestBody<"Workspaces.Teams.RenameTeam">;
type DescribeTeamBody =
  OperationRequestBody<"Workspaces.Teams.UpdateTeamDescription">;
type ArchiveTeamBody = OperationRequestBody<"Workspaces.Teams.ArchiveTeam">;
type UnarchiveTeamBody = OperationRequestBody<"Workspaces.Teams.UnarchiveTeam">;
type AddTeamMemberBody = OperationRequestBody<"Workspaces.Teams.AddTeamMember">;
type ChangeTeamMemberRoleBody =
  OperationRequestBody<"Workspaces.Teams.ChangeTeamMemberRole">;

/** Structural subset of the runtime `endpoints` object. */
export interface TeamsEndpoints {
  workspaces: {
    teams: (workspaceId: string) => string;
    team: (workspaceId: string, teamId: string) => string;
    teamDescription: (workspaceId: string, teamId: string) => string;
    teamArchive: (workspaceId: string, teamId: string) => string;
    teamUnarchive: (workspaceId: string, teamId: string) => string;
    teamMembers: (workspaceId: string, teamId: string) => string;
    teamMember: (workspaceId: string, teamId: string, userId: string) => string;
    teamMemberRole: (
      workspaceId: string,
      teamId: string,
      userId: string,
    ) => string;
  };
}

/** Archived Teams are kept (the management list shows them); id-less rows are dropped. */
export function parseTeamList(page: unknown): Team[] {
  const items = Array.isArray(page)
    ? page
    : (page as { items?: unknown } | null)?.items;
  if (!Array.isArray(items)) return [];
  return items
    .filter((raw) => Boolean((raw as { id?: string } | null)?.id))
    .map(parseTeam);
}

export function parseTeamMembers(items: unknown): TeamMember[] {
  if (!Array.isArray(items)) return [];
  return items
    .filter((raw) => Boolean((raw as { userId?: string } | null)?.userId))
    .map(parseTeamMember);
}

/**
 * Adapter for the Workspaces Team producer. Every write carries the Team
 * version read from the producer (body, or query string for removal); a
 * stale one is rejected and never retried automatically. Delete is not
 * offered: Teams are archived, like Spaces.
 */
export function createTeamsService(
  api: SpacesApiClient,
  endpoints: TeamsEndpoints,
) {
  const e = endpoints.workspaces;
  return {
    /** Active and archived Teams; reads every page, 200 Teams per request. */
    async list(workspaceId: string): Promise<Team[]> {
      const teams: Team[] = [];
      let cursor: string | null = null;
      do {
        const query: string = `limit=200${cursor ? `&cursor=${encodeURIComponent(cursor)}` : ""}`;
        const page: TeamPage = await api.get<TeamPage>(
          `${e.teams(workspaceId)}?${query}`,
        );
        teams.push(...parseTeamList(page));
        cursor = page?.nextCursor ?? null;
      } while (cursor);
      return teams;
    },

    async get(workspaceId: string, teamId: string): Promise<Team> {
      return parseTeam(await api.get<unknown>(e.team(workspaceId, teamId)));
    },

    async members(workspaceId: string, teamId: string): Promise<TeamMember[]> {
      return parseTeamMembers(
        await api.get<unknown>(e.teamMembers(workspaceId, teamId)),
      );
    },

    /**
     * Returns the new Team id (201 with the id). A retry under the same
     * `idempotencyKey` returns the Team the first attempt made.
     */
    async create(
      workspaceId: string,
      input: { name: string; description: string | null },
      idempotencyKey?: string,
    ): Promise<string> {
      const body: CreateTeamBody = {
        name: input.name,
        description: input.description,
      };
      return api.post<string>(
        e.teams(workspaceId),
        body,
        idempotencyKey ? { idempotencyKey } : undefined,
      );
    },

    async rename(
      workspaceId: string,
      teamId: string,
      input: { name: string; expectedVersion: number },
    ): Promise<void> {
      const body: RenameTeamBody = {
        name: input.name,
        expectedVersion: input.expectedVersion,
      };
      await api.put<void>(e.team(workspaceId, teamId), body);
    },

    async describe(
      workspaceId: string,
      teamId: string,
      input: { description: string | null; expectedVersion: number },
    ): Promise<void> {
      const body: DescribeTeamBody = {
        description: input.description,
        expectedVersion: input.expectedVersion,
      };
      await api.put<void>(e.teamDescription(workspaceId, teamId), body);
    },

    async archive(
      workspaceId: string,
      teamId: string,
      input: { expectedVersion: number },
    ): Promise<void> {
      const body: ArchiveTeamBody = { expectedVersion: input.expectedVersion };
      await api.post<void>(e.teamArchive(workspaceId, teamId), body);
    },

    async unarchive(
      workspaceId: string,
      teamId: string,
      input: { expectedVersion: number },
    ): Promise<void> {
      const body: UnarchiveTeamBody = {
        expectedVersion: input.expectedVersion,
      };
      await api.post<void>(e.teamUnarchive(workspaceId, teamId), body);
    },

    async addMember(
      workspaceId: string,
      teamId: string,
      input: { userId: string; role: TeamRole; expectedVersion: number },
    ): Promise<void> {
      const body: AddTeamMemberBody = {
        userId: input.userId,
        role: input.role,
        expectedVersion: input.expectedVersion,
      };
      await api.post<void>(e.teamMembers(workspaceId, teamId), body);
    },

    /** No body; the producer reads `expectedVersion` from the query string. */
    async removeMember(
      workspaceId: string,
      teamId: string,
      userId: string,
      input: { expectedVersion: number },
    ): Promise<void> {
      await api.delete<void>(
        `${e.teamMember(workspaceId, teamId, userId)}?expectedVersion=${input.expectedVersion}`,
      );
    },

    async changeMemberRole(
      workspaceId: string,
      teamId: string,
      userId: string,
      input: { newRole: TeamRole; expectedVersion: number },
    ): Promise<void> {
      const body: ChangeTeamMemberRoleBody = {
        newRole: input.newRole,
        expectedVersion: input.expectedVersion,
      };
      await api.put<void>(e.teamMemberRole(workspaceId, teamId, userId), body);
    },
  };
}

export type TeamsService = ReturnType<typeof createTeamsService>;
