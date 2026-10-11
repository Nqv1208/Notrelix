import { describe, expect, it } from "vitest";
import {
  addableTeamMembers,
  countLeads,
  parseTeam,
  parseTeamMember,
  teamCapabilities,
  teamMemberCapabilities,
  type Team,
  type TeamMember,
} from "../core/model/teams";
import {
  createTeamsService,
  parseTeamList,
  parseTeamMembers,
  type TeamsEndpoints,
} from "../core/api/teams.service";
import type { SpacesApiClient } from "../core/api/spaces.service";
import { workspaceQueryKeys } from "../query/keys";
import { memberFixture as workspaceMember } from "../verification/workspace-members-fixtures";

const team = (overrides: Partial<Team> = {}): Team => ({
  id: "t-1",
  name: "Platform",
  description: null,
  status: "Active",
  memberCount: 2,
  version: 3,
  createdAt: "2026-01-01T00:00:00Z",
  ...overrides,
});

const teamMember = (
  userId: string,
  role: TeamMember["role"] = "Member",
): TeamMember => ({
  userId,
  displayName: userId.toUpperCase(),
  email: null,
  role,
});

describe("Team parsing", () => {
  it("maps status, version and the nullable description", () => {
    expect(
      parseTeam({
        id: "t-1",
        name: "Platform",
        description: "Core",
        status: "Archived",
        memberCount: 4,
        version: 7,
        createdAt: "2026-02-02T00:00:00Z",
      }),
    ).toEqual({
      id: "t-1",
      name: "Platform",
      description: "Core",
      status: "Archived",
      memberCount: 4,
      version: 7,
      createdAt: "2026-02-02T00:00:00Z",
    });
  });

  it("defaults a missing description, version and count", () => {
    const parsed = parseTeam({ id: "t-2", name: "X", status: "Active" });
    expect(parsed.description).toBeNull();
    expect(parsed.version).toBe(0);
    expect(parsed.memberCount).toBe(0);
    expect(parsed.status).toBe("Active");
  });

  it("keeps archived teams in the list and drops rows without an id", () => {
    const teams = parseTeamList([
      { id: "a", name: "A", status: "Active" },
      { id: "b", name: "B", status: "Archived" },
      { name: "no id", status: "Active" },
    ]);
    expect(teams.map((item) => [item.id, item.status])).toEqual([
      ["a", "Active"],
      ["b", "Archived"],
    ]);
    expect(parseTeamList(null)).toEqual([]);
  });

  it("parses members with a null email and defaults an unknown role to Member", () => {
    expect(
      parseTeamMember({
        userId: "u1",
        displayName: "Ada",
        email: null,
        role: "Lead",
        status: "Active",
      }),
    ).toEqual({ userId: "u1", displayName: "Ada", email: null, role: "Lead" });
    expect(parseTeamMember({ userId: "u2", role: "Weird" }).role).toBe(
      "Member",
    );
    expect(
      parseTeamMembers([{ userId: "u1", role: "Lead" }, { role: "Lead" }]),
    ).toHaveLength(1);
  });
});

describe("Team capabilities", () => {
  it("lets workspace admins manage an active Team", () => {
    expect(teamCapabilities(team(), "admin")).toEqual({
      canManage: true,
      canEditMembers: true,
      blockedBy: null,
    });
    expect(teamCapabilities(team(), "owner").canManage).toBe(true);
  });

  it("blocks non-admins", () => {
    for (const role of ["member", "guest", null] as const) {
      expect(teamCapabilities(team(), role)).toEqual({
        canManage: false,
        canEditMembers: false,
        blockedBy: "not-admin",
      });
    }
  });

  it("keeps an archived Team manageable (unarchive) but not editable", () => {
    expect(teamCapabilities(team({ status: "Archived" }), "admin")).toEqual({
      canManage: true,
      canEditMembers: false,
      blockedBy: "archived",
    });
  });
});

describe("Team member capabilities", () => {
  const members = [teamMember("a", "Lead"), teamMember("b"), teamMember("c")];

  it("counts leads", () => {
    expect(countLeads(members)).toBe(1);
    expect(countLeads([...members, teamMember("d", "Lead")])).toBe(2);
  });

  it("pins the last Lead: no demotion, no removal", () => {
    expect(teamMemberCapabilities(members[0]!, members, team(), true)).toEqual({
      roles: ["Lead"],
      canRemove: false,
      blockedBy: "last-lead",
    });
  });

  it("allows demoting or removing a Lead when another Lead exists", () => {
    const two = [...members, teamMember("d", "Lead")];
    expect(teamMemberCapabilities(two[0]!, two, team(), true)).toEqual({
      roles: ["Lead", "Member"],
      canRemove: true,
      blockedBy: null,
    });
  });

  it("allows a plain Member to be changed or removed", () => {
    expect(teamMemberCapabilities(members[1]!, members, team(), true)).toEqual({
      roles: ["Lead", "Member"],
      canRemove: true,
      blockedBy: null,
    });
  });

  it("reports an archived Team before anything else", () => {
    expect(
      teamMemberCapabilities(
        members[1]!,
        members,
        team({ status: "Archived" }),
        false,
      ),
    ).toEqual({ roles: [], canRemove: false, blockedBy: "archived" });
  });

  it("reports non-admin viewers", () => {
    expect(teamMemberCapabilities(members[1]!, members, team(), false)).toEqual(
      { roles: [], canRemove: false, blockedBy: "not-admin" },
    );
  });
});

describe("addableTeamMembers", () => {
  it("excludes existing Team members and suspended workspace members", () => {
    const workspace = [
      workspaceMember({ userId: "a", name: "A", role: "member" }),
      workspaceMember({ userId: "b", name: "B", role: "member" }),
      workspaceMember({
        userId: "c",
        name: "C",
        role: "member",
        membership: "suspended",
      }),
      workspaceMember({
        userId: "d",
        name: "D",
        role: "guest",
        membership: "active",
      }),
    ];
    expect(
      addableTeamMembers(workspace, [teamMember("a")]).map(
        (member) => member.userId,
      ),
    ).toEqual(["b", "d"]);
  });
});

describe("Teams service", () => {
  const endpoints: TeamsEndpoints = {
    workspaces: {
      teams: (ws) => `/w/${ws}/teams`,
      team: (ws, t) => `/w/${ws}/teams/${t}`,
      teamDescription: (ws, t) => `/w/${ws}/teams/${t}/description`,
      teamArchive: (ws, t) => `/w/${ws}/teams/${t}/archive`,
      teamUnarchive: (ws, t) => `/w/${ws}/teams/${t}/unarchive`,
      teamMembers: (ws, t) => `/w/${ws}/teams/${t}/members`,
      teamMember: (ws, t, u) => `/w/${ws}/teams/${t}/members/${u}`,
      teamMemberRole: (ws, t, u) => `/w/${ws}/teams/${t}/members/${u}/role`,
    },
  };

  const recorder = (responses: Record<string, unknown> = {}) => {
    const calls: Array<{ method: string; url: string; body?: unknown }> = [];
    const record =
      (method: string) =>
      async (url: string, body?: unknown): Promise<never> => {
        calls.push({ method, url, body });
        return responses[`${method} ${url}`] as never;
      };
    const api: SpacesApiClient = {
      get: record("GET"),
      post: record("POST"),
      put: record("PUT"),
      patch: record("PATCH"),
      delete: record("DELETE"),
    };
    return { api, calls };
  };

  it("removeMember carries expectedVersion in the query string", async () => {
    const { api, calls } = recorder();
    await createTeamsService(api, endpoints).removeMember("ws", "t1", "u1", {
      expectedVersion: 3,
    });
    expect(calls).toEqual([
      {
        method: "DELETE",
        url: "/w/ws/teams/t1/members/u1?expectedVersion=3",
        body: undefined,
      },
    ]);
  });

  it("list returns archived teams too", async () => {
    const { api } = recorder({
      "GET /w/ws/teams": [
        { id: "a", name: "A", status: "Active" },
        { id: "b", name: "B", status: "Archived" },
      ],
    });
    const teams = await createTeamsService(api, endpoints).list("ws");
    expect(teams.map((item) => item.status)).toEqual(["Active", "Archived"]);
  });

  it("create returns the id from the 201 body", async () => {
    const { api, calls } = recorder({ "POST /w/ws/teams": "new-id" });
    const id = await createTeamsService(api, endpoints).create("ws", {
      name: "N",
      description: null,
    });
    expect(id).toBe("new-id");
    expect(calls[0]?.body).toEqual({ name: "N", description: null });
  });

  it("sends expectedVersion in the body of every versioned write", async () => {
    const { api, calls } = recorder();
    const service = createTeamsService(api, endpoints);
    await service.rename("ws", "t1", { name: "R", expectedVersion: 1 });
    await service.describe("ws", "t1", {
      description: "D",
      expectedVersion: 2,
    });
    await service.archive("ws", "t1", { expectedVersion: 3 });
    await service.unarchive("ws", "t1", { expectedVersion: 4 });
    await service.addMember("ws", "t1", {
      userId: "u1",
      role: "Lead",
      expectedVersion: 5,
    });
    await service.changeMemberRole("ws", "t1", "u1", {
      newRole: "Member",
      expectedVersion: 6,
    });
    expect(calls).toEqual([
      {
        method: "PUT",
        url: "/w/ws/teams/t1",
        body: { name: "R", expectedVersion: 1 },
      },
      {
        method: "PUT",
        url: "/w/ws/teams/t1/description",
        body: { description: "D", expectedVersion: 2 },
      },
      {
        method: "POST",
        url: "/w/ws/teams/t1/archive",
        body: { expectedVersion: 3 },
      },
      {
        method: "POST",
        url: "/w/ws/teams/t1/unarchive",
        body: { expectedVersion: 4 },
      },
      {
        method: "POST",
        url: "/w/ws/teams/t1/members",
        body: { userId: "u1", role: "Lead", expectedVersion: 5 },
      },
      {
        method: "PUT",
        url: "/w/ws/teams/t1/members/u1/role",
        body: { newRole: "Member", expectedVersion: 6 },
      },
    ]);
  });
});

describe("Team query keys", () => {
  it("scope team and team members by workspace", () => {
    expect(workspaceQueryKeys.team("ws-a", "t1")).not.toEqual(
      workspaceQueryKeys.team("ws-b", "t1"),
    );
    const root = workspaceQueryKeys.team("ws-a", "t1");
    const members = workspaceQueryKeys.teamMembers("ws-a", "t1");
    expect(members.slice(0, root.length)).toEqual(root);
    const teams = workspaceQueryKeys.teams("ws-a");
    expect(root.slice(0, teams.length)).toEqual(teams);
  });
});
