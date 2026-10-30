import { describe, expect, it } from "vitest";
import {
  createOnboardingApi,
  type OnboardingApiClient,
} from "@notrelix/features-workspace/web";

const WS = "w1";

interface Call {
  readonly method: string;
  readonly url: string;
  readonly body?: unknown;
}

/** A client that answers the reads the join page is composed from. */
function stub(overrides: Record<string, unknown> = {}) {
  const calls: Call[] = [];
  const answers: Record<string, unknown> = {
    [`/workspaces/${WS}`]: { id: WS, name: "Minh An Group", memberCount: 86 },
    [`/workspaces/${WS}/members/me`]: { role: "Member" },
    [`/workspaces/${WS}/spaces/tree`]: [
      {
        id: "s1",
        parentSpaceId: null,
        name: "Công nghệ",
        visibility: "Private",
        isArchived: false,
        accessLevel: "Editor",
      },
      {
        id: "s2",
        parentSpaceId: null,
        name: "Marketing",
        visibility: "Workspace",
        isArchived: false,
        accessLevel: null,
      },
      {
        id: "s3",
        parentSpaceId: "s1",
        name: "Nhóm Hạ tầng",
        visibility: "Private",
        isArchived: false,
        accessLevel: "Editor",
      },
      {
        id: "s4",
        parentSpaceId: null,
        name: "Cũ",
        visibility: "Private",
        isArchived: true,
        accessLevel: "Manager",
      },
    ],
    [`/workspaces/${WS}/boards`]: [
      {
        id: "b1",
        title: "Triển khai ERP Q4",
        spaceId: "s1",
        isArchived: false,
      },
      { id: "b2", title: "Lưu trữ", spaceId: "s1", isArchived: true },
      { id: "b3", title: "Chiến dịch", spaceId: "s2", isArchived: false },
    ],
    "/auth/me": { id: "u1", name: "Lý Thu Trang" },
    [`/workspaces/${WS}/notification-preferences`]: {
      version: 4,
      rows: [{ type: "digest", inApp: true, email: true }],
    },
    ...overrides,
  };
  const client: OnboardingApiClient = {
    get: async (url) => {
      calls.push({ method: "GET", url });
      if (!(url in answers)) throw new Error(`no answer for ${url}`);
      const answer = answers[url];
      if (answer instanceof Error) throw answer;
      return answer as never;
    },
    post: async (url, body) => {
      calls.push({ method: "POST", url, body });
      return undefined as never;
    },
    put: async (url, body) => {
      calls.push({ method: "PUT", url, body });
      return undefined as never;
    },
    patch: async (url, body) => {
      calls.push({ method: "PATCH", url, body });
      return undefined as never;
    },
    delete: async (url) => {
      calls.push({ method: "DELETE", url });
      return undefined as never;
    },
  };
  return { api: createOnboardingApi(client), calls };
}

describe("the first visit of an invited person", () => {
  it("lists the top-level Spaces the person can see with their active Boards", async () => {
    const join = await stub().api.getJoin(WS);
    expect(join.workspace).toEqual({
      id: WS,
      name: "Minh An Group",
      memberCount: 86,
    });
    expect(join.role).toBe("member");
    expect(join.inviterName).toBeNull();
    expect(join.profile).toEqual({
      displayName: "Lý Thu Trang",
      notifyEmail: "digest",
    });
    // The sub-space and the archived Space are not offered as a way in.
    expect(join.spaces.map((space) => space.name)).toEqual([
      "Công nghệ",
      "Marketing",
    ]);
    expect(join.spaces[0]).toMatchObject({
      open: false,
      boards: [{ id: "b1", title: "Triển khai ERP Q4", level: "editor" }],
    });
    // Without a role in an open Space, the page never promises more than a view.
    expect(join.spaces[1]).toMatchObject({
      open: true,
      boards: [{ id: "b3", level: "viewer" }],
    });
  });

  it("reads the digest email as off from the member's own matrix", async () => {
    const join = await stub({
      [`/workspaces/${WS}/notification-preferences`]: {
        version: 5,
        rows: [{ type: "digest", inApp: true, email: false }],
      },
    }).api.getJoin(WS);
    expect(join.profile.notifyEmail).toBe("mentions");
  });

  it("still opens when the notification matrix cannot be read", async () => {
    const join = await stub({
      [`/workspaces/${WS}/notification-preferences`]: new Error("down"),
    }).api.getJoin(WS);
    expect(join.profile.notifyEmail).toBe("digest");
  });

  it("saves the name to the profile and the email choice to the matrix at the version it read", async () => {
    const { api, calls } = stub();
    await api.saveJoin(WS, { displayName: "Trang", notifyEmail: "mentions" });
    expect(calls.filter((call) => call.method !== "GET")).toEqual([
      { method: "PATCH", url: "/profile", body: { name: "Trang" } },
      {
        method: "PATCH",
        url: `/workspaces/${WS}/notification-preferences`,
        body: {
          expectedVersion: 4,
          rows: [
            { type: "digest", email: false },
            { type: "mention", email: true },
          ],
        },
      },
    ]);
  });
});
