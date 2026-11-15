import { describe, expect, it, vi } from "vitest";
import {
  createSpacesService,
  type SpacesApiClient,
  type SpacesEndpoints,
} from "../core/api/spaces.service";
import {
  createTeamsService,
  type TeamsEndpoints,
} from "../core/api/teams.service";

const W = "ws-1";

function fakeApi(): SpacesApiClient & {
  [K in keyof SpacesApiClient]: ReturnType<typeof vi.fn>;
} {
  return {
    get: vi.fn(),
    post: vi.fn().mockResolvedValue(undefined),
    put: vi.fn(),
    patch: vi.fn(),
    delete: vi.fn().mockResolvedValue(undefined),
  } as never;
}

const spaceEndpoints: SpacesEndpoints = {
  workspaces: {
    spaces: (w) => `/workspaces/${w}/spaces`,
    spaceTree: (w) => `/workspaces/${w}/spaces/tree`,
    space: (w, s) => `/workspaces/${w}/spaces/${s}`,
    spaceDescription: (w, s) => `/workspaces/${w}/spaces/${s}/description`,
    spaceVisibility: (w, s) => `/workspaces/${w}/spaces/${s}/visibility`,
    spaceType: (w, s) => `/workspaces/${w}/spaces/${s}/type`,
    spaceArchive: (w, s) => `/workspaces/${w}/spaces/${s}/archive`,
    spaceUnarchive: (w, s) => `/workspaces/${w}/spaces/${s}/unarchive`,
    spaceRestore: (w, s) => `/workspaces/${w}/spaces/${s}/restore`,
    spacesDeleted: (w) => `/workspaces/${w}/spaces/deleted`,
    spaceMove: (w, s) => `/workspaces/${w}/spaces/${s}/move`,
    spaceMembers: (w, s) => `/workspaces/${w}/spaces/${s}/members`,
    spaceMember: (w, s, t, id) =>
      `/workspaces/${w}/spaces/${s}/members/${t}/${id}`,
  },
};

const teamEndpoints: TeamsEndpoints = {
  workspaces: {
    teams: (w) => `/workspaces/${w}/teams`,
    team: (w, t) => `/workspaces/${w}/teams/${t}`,
    teamDescription: (w, t) => `/workspaces/${w}/teams/${t}/description`,
    teamArchive: (w, t) => `/workspaces/${w}/teams/${t}/archive`,
    teamUnarchive: (w, t) => `/workspaces/${w}/teams/${t}/unarchive`,
    teamRestore: (w, t) => `/workspaces/${w}/teams/${t}/restore`,
    teamMembers: (w, t) => `/workspaces/${w}/teams/${t}/members`,
    teamMember: (w, t, u) => `/workspaces/${w}/teams/${t}/members/${u}`,
    teamMemberRole: (w, t, u) =>
      `/workspaces/${w}/teams/${t}/members/${u}/role`,
  },
};

describe("Space delete and restore calls", () => {
  it("deletes the Space with its version in the query string", async () => {
    const api = fakeApi();
    await createSpacesService(api, spaceEndpoints).remove(W, "sp-1", 4);
    expect(api.delete).toHaveBeenCalledWith(
      "/workspaces/ws-1/spaces/sp-1?expectedVersion=4",
    );
    expect(api.post).not.toHaveBeenCalled();
  });

  it("reads the deleted Spaces with their restore deadline", async () => {
    const api = fakeApi();
    api.get.mockResolvedValue([
      {
        id: "sp-1",
        name: "Kế toán",
        parentSpaceId: null,
        deletedAt: "2026-11-01T02:00:00Z",
        restorableUntil: "2026-12-01T02:00:00Z",
      },
    ]);
    const rows = await createSpacesService(api, spaceEndpoints).listDeleted(W);
    expect(api.get).toHaveBeenCalledWith("/workspaces/ws-1/spaces/deleted");
    expect(rows).toEqual([
      {
        id: "sp-1",
        name: "Kế toán",
        parentSpaceId: null,
        deletedAt: "2026-11-01T02:00:00Z",
        restorableUntil: "2026-12-01T02:00:00Z",
      },
    ]);
  });

  it("restores the Space with an empty body and no expected version", async () => {
    const api = fakeApi();
    await createSpacesService(api, spaceEndpoints).restore(W, "sp-1");
    expect(api.post).toHaveBeenCalledWith(
      "/workspaces/ws-1/spaces/sp-1/restore",
      {},
    );
    expect(api.delete).not.toHaveBeenCalled();
  });
});

describe("Team delete and restore calls", () => {
  it("deletes the Team with its version in the query string", async () => {
    const api = fakeApi();
    await createTeamsService(api, teamEndpoints).remove(W, "tm-1", {
      expectedVersion: 4,
    });
    expect(api.delete).toHaveBeenCalledWith(
      "/workspaces/ws-1/teams/tm-1?expectedVersion=4",
    );
  });

  it("restores the Team with an empty body and no version", async () => {
    const api = fakeApi();
    await createTeamsService(api, teamEndpoints).restore(W, "tm-1");
    expect(api.post).toHaveBeenCalledWith(
      "/workspaces/ws-1/teams/tm-1/restore",
      {},
    );
    expect(api.delete).not.toHaveBeenCalled();
  });
});
