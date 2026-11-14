import { describe, expect, it } from "vitest";
import {
  createSpacesService,
  type SpacesApiClient,
  type SpacesEndpoints,
} from "../core/api/spaces.service";

const endpoints: SpacesEndpoints = {
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
    spaceMove: (w, s) => `/workspaces/${w}/spaces/${s}/move`,
    spaceMembers: (w, s) => `/workspaces/${w}/spaces/${s}/members`,
    spaceMember: (w, s, type, id) =>
      `/workspaces/${w}/spaces/${s}/members/${type}/${id}`,
  },
};

const unused = async (): Promise<never> => {
  throw new Error("not used by these tests");
};

/** Serves the member list in pages; the cursor is the offset of the next page. */
function membersApi(all: readonly unknown[], urls: string[]): SpacesApiClient {
  return {
    async get<T>(url: string): Promise<T> {
      urls.push(url);
      const query = new URL(url, "http://producer.test").searchParams;
      const limit = Number(query.get("limit"));
      const start = Number(query.get("cursor") ?? 0);
      const next = start + limit < all.length ? String(start + limit) : null;
      return {
        items: all.slice(start, start + limit),
        nextCursor: next,
        hasNextPage: next !== null,
      } as T;
    },
    post: unused,
    put: unused,
    patch: unused,
    delete: unused,
  };
}

const entry = (n: number, role: string) => ({
  subjectType: "User",
  subjectId: `user-${String(n).padStart(4, "0")}`,
  role,
  displayName: `Person ${n}`,
  avatarUrl: null,
  addedAt: "2026-01-01T00:00:00Z",
});

describe("Space members read", () => {
  it("follows nextCursor until every page is read", async () => {
    const all = Array.from({ length: 450 }, (_, n) => entry(n, "Member"));
    const urls: string[] = [];
    const members = await createSpacesService(
      membersApi(all, urls),
      endpoints,
    ).getMembers("ws-1", "space-1");

    expect(members).toHaveLength(450);
    expect(urls).toHaveLength(3);
    expect(urls.every((url) => url.includes("limit=200"))).toBe(true);
    expect(urls[1]).toContain("cursor=200");
  });

  it("orders people by role, Admins first, keeping the producer order within a role", async () => {
    const all = [
      entry(1, "Viewer"),
      entry(2, "Admin"),
      entry(3, "Member"),
      entry(4, "Admin"),
    ];
    const members = await createSpacesService(
      membersApi(all, []),
      endpoints,
    ).getMembers("ws-1", "space-1");

    expect(members.map((m) => m.role)).toEqual([
      "Admin",
      "Admin",
      "Member",
      "Viewer",
    ]);
    expect(members.map((m) => m.subjectId)).toEqual([
      "user-0002",
      "user-0004",
      "user-0003",
      "user-0001",
    ]);
  });
});
