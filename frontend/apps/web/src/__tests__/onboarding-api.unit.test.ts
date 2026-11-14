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
    [`/workspaces/${WS}/spaces/tree?limit=200`]: {
      items: [
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
      nextCursor: null,
    },
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

describe("the onboarding creates keep their Idempotency-Key across a retry", () => {
  const lostAnswer = Object.assign(new Error("network"), { status: 0 });
  const refused = Object.assign(new Error("conflict"), { status: 409 });
  const input = {
    name: "Minh An",
    slug: "minh-an",
    size: "m" as const,
    locale: "vi" as const,
  };

  /** A client whose POSTs answer from a script: an Error is thrown, anything else is the answer. */
  function scripted(answers: readonly unknown[]) {
    const keys: (string | undefined)[] = [];
    let next = 0;
    const client: OnboardingApiClient = {
      get: async () => ({ accountId: "acc1" }) as never,
      post: async (_url, _body, options) => {
        keys.push(options?.idempotencyKey);
        const answer = answers[next++];
        if (answer instanceof Error) throw answer;
        return answer as never;
      },
      put: async () => undefined as never,
      patch: async () => undefined as never,
      delete: async () => undefined as never,
    };
    return { api: createOnboardingApi(client), keys };
  }

  it("sends the same key when the workspace create is retried after a lost answer", async () => {
    const { api, keys } = scripted([lostAnswer, "ws-1"]);

    await expect(api.createWorkspace(input)).rejects.toThrow("network");
    await expect(api.createWorkspace(input)).resolves.toEqual({
      id: "ws-1",
      slug: "minh-an",
    });

    expect(keys).toHaveLength(2);
    expect(keys[0]).toBeTruthy();
    expect(keys[1]).toBe(keys[0]);
  });

  it("forgets the key once the create succeeds, so the next identical create is a new request", async () => {
    const { api, keys } = scripted(["ws-1", "ws-2"]);

    await api.createWorkspace(input);
    await api.createWorkspace(input);

    expect(keys[1]).not.toBe(keys[0]);
  });

  it("keeps the invitation key on a lost answer and forgets it on a refused row", async () => {
    const invite = [
      { email: "lan@example.com", spaceId: null, role: "Member" as const },
    ];
    const { api, keys } = scripted([lostAnswer, undefined, refused]);

    await expect(api.sendInvites("w1", invite)).rejects.toThrow("network");
    await expect(api.sendInvites("w1", invite)).resolves.toEqual({
      sent: 1,
      skipped: [],
    });
    expect(keys[1]).toBe(keys[0]);

    // The sent invitation forgot its key, so a refused row is a new request; the refused row forgets its key too.
    await expect(api.sendInvites("w1", invite)).resolves.toEqual({
      sent: 0,
      skipped: ["lan@example.com"],
    });
    expect(keys[2]).not.toBe(keys[1]);
    await api.sendInvites("w1", invite);
    expect(keys[3]).not.toBe(keys[2]);
  });

  it("drops the workspace key when the create is refused, so the corrected retry is a new request", async () => {
    const { api, keys } = scripted([refused, "ws-1"]);

    await expect(api.createWorkspace(input)).rejects.toThrow("conflict");
    await expect(api.createWorkspace(input)).resolves.toEqual({
      id: "ws-1",
      slug: "minh-an",
    });

    expect(keys[0]).toBeTruthy();
    expect(keys[1]).not.toBe(keys[0]);
  });

  const department = { name: "Công nghệ", color: "blue" as const };

  it("sends the same department key when the create is retried after a lost answer", async () => {
    const { api, keys } = scripted([lostAnswer, "sp-1"]);

    await expect(api.createDepartments("w1", [department])).rejects.toThrow(
      "network",
    );
    await expect(api.createDepartments("w1", [department])).resolves.toEqual({
      departments: [{ id: "sp-1", name: "Công nghệ", color: "blue" }],
    });

    expect(keys).toHaveLength(2);
    expect(keys[0]).toBeTruthy();
    expect(keys[1]).toBe(keys[0]);
  });

  it("drops the department key when the create is refused, so the corrected retry is a new request", async () => {
    const { api, keys } = scripted([refused, "sp-1"]);

    await expect(api.createDepartments("w1", [department])).rejects.toThrow(
      "conflict",
    );
    await api.createDepartments("w1", [department]);

    expect(keys[0]).toBeTruthy();
    expect(keys[1]).not.toBe(keys[0]);
  });

  const board = { templateId: "blank", name: "Kế hoạch Q4", spaceId: null };

  it("sends the same board key when the create is retried after a lost answer", async () => {
    const { api, keys } = scripted([lostAnswer, "b-1"]);

    await expect(api.createBoard("w1", board)).rejects.toThrow("network");
    await expect(api.createBoard("w1", board)).resolves.toMatchObject({
      id: "b-1",
      templateId: "blank",
    });

    expect(keys).toHaveLength(2);
    expect(keys[0]).toBeTruthy();
    expect(keys[1]).toBe(keys[0]);
  });

  it("drops the board key when the create is refused, so the corrected retry is a new request", async () => {
    const { api, keys } = scripted([refused, "b-1"]);

    await expect(api.createBoard("w1", board)).rejects.toThrow("conflict");
    await api.createBoard("w1", board);

    expect(keys[0]).toBeTruthy();
    expect(keys[1]).not.toBe(keys[0]);
  });
});
