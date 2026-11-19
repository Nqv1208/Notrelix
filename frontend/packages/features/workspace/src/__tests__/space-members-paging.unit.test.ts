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
    spacesDeleted: (w) => `/workspaces/${w}/spaces/deleted`,
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

/**
 * Serves a producer that filters by role and kind, then cuts a page of `limit` after the cursor
 * (an offset into the filtered list), as GET .../members does.
 */
function filteringApi(
  all: readonly ReturnType<typeof entry>[],
  urls: string[],
): SpacesApiClient {
  return {
    async get<T>(url: string): Promise<T> {
      urls.push(url);
      const query = new URL(url, "http://producer.test").searchParams;
      const role = query.get("role");
      const kind = query.get("kind");
      const rows = all
        .filter((row) => !role || row.role === role)
        .filter((row) => !kind || row.subjectType === kind);
      const limit = Number(query.get("limit"));
      const start = Number(query.get("cursor") ?? 0);
      const next = start + limit < rows.length ? String(start + limit) : null;
      return {
        items: rows.slice(start, start + limit),
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

describe("Space members tab read (one page at a time)", () => {
  const large = Array.from({ length: 500 }, (_, n) => ({
    ...entry(n, n < 50 ? "Admin" : n < 349 ? "Member" : "Viewer"),
    subjectType: n % 10 === 9 ? "Team" : "User",
  }));

  it("asks for 50 entries by default and carries no filter when none is picked", async () => {
    const urls: string[] = [];
    const page = await createSpacesService(
      filteringApi(large, urls),
      endpoints,
    ).getMembersPage("ws-1", "space-1");

    expect(page.entries).toHaveLength(50);
    expect(page.nextCursor).toBe("50");
    expect(urls).toEqual(["/workspaces/ws-1/spaces/space-1/members?limit=50"]);
  });

  it("walks 500 entries in ten pages: the next page continues, nothing repeats or is skipped", async () => {
    const api = createSpacesService(filteringApi(large, []), endpoints);
    const seen: string[] = [];
    const sizes: number[] = [];
    let cursor: string | null = null;
    do {
      const page: Awaited<ReturnType<typeof api.getMembersPage>> =
        await api.getMembersPage("ws-1", "space-1", { cursor });
      sizes.push(page.entries.length);
      seen.push(...page.entries.map((e) => e.subjectId));
      cursor = page.nextCursor;
    } while (cursor);

    expect(sizes).toEqual(Array.from({ length: 10 }, () => 50));
    expect(new Set(seen).size).toBe(500);
    expect([...seen].sort()).toEqual(large.map((e) => e.subjectId).sort());
  });

  it("sends the role and kind to the producer, and cuts the page from the matching entries", async () => {
    const urls: string[] = [];
    const api = createSpacesService(filteringApi(large, urls), endpoints);

    const members = await api.getMembersPage("ws-1", "space-1", {
      role: "Member",
      kind: "User",
    });

    expect(urls[0]).toContain("role=Member");
    expect(urls[0]).toContain("kind=User");
    expect(members.entries).toHaveLength(50);
    expect(members.entries.every((e) => e.role === "Member")).toBe(true);
    expect(members.entries.every((e) => e.subjectType === "User")).toBe(true);
    expect(members.nextCursor).not.toBeNull();
  });

  it("passes the cursor back and ends with no cursor on the last page", async () => {
    const urls: string[] = [];
    const api = createSpacesService(filteringApi(large, urls), endpoints);

    const last = await api.getMembersPage("ws-1", "space-1", {
      role: "Admin",
      cursor: "0",
      limit: 200,
    });

    expect(urls[0]).toContain("cursor=0");
    expect(urls[0]).toContain("limit=200");
    expect(last.entries).toHaveLength(50);
    expect(last.nextCursor).toBeNull();
  });

  it("lists Admins first within a page and never reorders a page already read", async () => {
    const mixed = [entry(1, "Viewer"), entry(2, "Admin"), entry(3, "Member")];
    const page = await createSpacesService(
      filteringApi(mixed, []),
      endpoints,
    ).getMembersPage("ws-1", "space-1");

    expect(page.entries.map((e) => e.role)).toEqual([
      "Admin",
      "Member",
      "Viewer",
    ]);
  });
});
