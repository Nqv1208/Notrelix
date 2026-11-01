import { describe, expect, it, vi } from "vitest";
import {
  parseActivityPage,
  createActivityService,
} from "../core/api/activity.service";
import {
  createWorkspaceInvitationsService,
  parseInvitations,
} from "../core/api/invitations.service";
import {
  createMembersService,
  parseMembers,
  parseMyMembership,
} from "../core/api/members.service";
import {
  createWorkspaceService,
  createWorkspaceSettingsService,
  parseWorkspaceSettings,
  parseWorkspaceSummary,
} from "../core/api/workspace.service";
import { describeWorkspaceFailure } from "../core/model/errors";
import {
  countOwners,
  invitationState,
  memberCapabilities,
  parseWorkspaceRole,
  pendingInvitationFor,
  pendingInvitations,
} from "../core/model/members";
import {
  groupMyWorkItems,
  myWorkWindow,
  type MyWorkItem,
} from "../core/model/my-work";
import type { WorkspaceMember } from "../core/types/workspace";
import {
  buildLibraryEntries,
  DEFAULT_LIBRARY_QUERY,
  libraryWindow,
  nextLibrarySort,
  queryLibrary,
} from "../core/model/library";

const NOW = Date.UTC(2026, 9, 12, 12, 0, 0);
const DAY = 86_400_000;
const iso = (offset: number) => new Date(NOW + offset).toISOString();

function item(id: string, dueAt: string | null): MyWorkItem {
  return {
    id,
    boardId: "b",
    groupId: "g",
    name: id,
    dueAt,
    startedAt: null,
    updatedAt: null,
    version: 1,
  };
}

const WORKSPACE_ENDPOINTS = {
  workspaces: {
    list: "/workspaces",
    detail: (id: string) => `/workspaces/${id}`,
  },
  accounts: { workspaces: (id: string) => `/accounts/${id}/workspaces` },
};

describe("My work grouping (WX-HOME-001)", () => {
  it("assigns calendar-day due windows", () => {
    expect(myWorkWindow(iso(-2 * DAY), NOW)).toBe("overdue");
    expect(myWorkWindow(iso(0), NOW)).toBe("today");
    expect(myWorkWindow(iso(3 * DAY), NOW)).toBe("this-week");
    expect(myWorkWindow(iso(30 * DAY), NOW)).toBe("later");
    expect(myWorkWindow(null, NOW)).toBe("no-date");
    expect(myWorkWindow("not a date", NOW)).toBe("no-date");
  });

  it("keeps producer order inside windows, drops duplicates and empty windows", () => {
    const groups = groupMyWorkItems(
      [
        item("a", iso(-DAY)),
        item("b", iso(0)),
        item("a", iso(-DAY)),
        item("c", null),
        item("d", iso(-3 * DAY)),
      ],
      NOW,
    );
    expect(groups.map((group) => group.range)).toEqual([
      "overdue",
      "today",
      "no-date",
    ]);
    expect(groups[0]!.items.map((entry) => entry.id)).toEqual(["a", "d"]);
  });
});

function member(
  userId: string,
  role: WorkspaceMember["role"],
  extra: Partial<WorkspaceMember> = {},
): WorkspaceMember {
  return { id: userId, userId, name: userId, role, ...extra };
}

describe("Member capabilities and the last-owner invariant (WSP-008)", () => {
  const owner = { userId: "u-owner", role: "owner" as const };

  it("never offers changing, suspending or removing the last owner", () => {
    const members = [member("u-owner", "owner"), member("u-other", "owner")];
    const last = memberCapabilities(member("u-x", "owner"), owner, 1);
    expect(last.assignableRoles).toEqual([]);
    expect(last.canRemove).toBe(false);
    expect(last.canSuspend).toBe(false);
    expect(last.roleBlockedBy).toBe("last-owner");
    expect(countOwners(members)).toBe(2);
    const second = memberCapabilities(members[1]!, owner, 2);
    expect(second.assignableRoles).toEqual([
      "owner",
      "admin",
      "member",
      "guest",
    ]);
    expect(second.canRemove).toBe(true);
  });

  it("blocks admins from acting on owners, members from acting at all, and everyone on themselves", () => {
    const admin = { userId: "u-admin", role: "admin" as const };
    expect(
      memberCapabilities(member("u-owner", "owner"), admin, 2).roleBlockedBy,
    ).toBe("owner-by-transfer");
    expect(
      memberCapabilities(member("u-m", "member"), admin, 1).assignableRoles,
    ).toEqual(["admin", "member", "guest"]);
    expect(
      memberCapabilities(
        member("u-m", "member"),
        { userId: "u-g", role: "member" },
        1,
      ).roleBlockedBy,
    ).toBe("not-admin");
    expect(
      memberCapabilities(member("u-admin", "admin"), admin, 1).roleBlockedBy,
    ).toBe("self");
  });

  it("offers suspend or reactivate from the producer's status, and neither when it is unknown", () => {
    const unknown = memberCapabilities(member("u-m", "member"), owner, 1);
    expect(unknown.canSuspend).toBe(false);
    expect(unknown.canActivate).toBe(false);
    const suspended = memberCapabilities(
      member("u-m", "member", { membership: "suspended" }),
      owner,
      1,
    );
    expect(suspended.canSuspend).toBe(false);
    expect(suspended.canActivate).toBe(true);
    expect(suspended.assignableRoles).toEqual([]);
    const active = memberCapabilities(
      member("u-m", "member", { membership: "active" }),
      owner,
      1,
    );
    expect(active.canActivate).toBe(false);
    expect(active.canSuspend).toBe(true);
  });

  it("parses producer role names and enum values", () => {
    expect(parseWorkspaceRole("Owner")).toBe("owner");
    expect(parseWorkspaceRole(2)).toBe("admin");
    expect(parseWorkspaceRole("superuser")).toBeNull();
  });
});

describe("Invitations", () => {
  it("derives pending invitations newest first", () => {
    const invitations = parseInvitations([
      {
        id: "1",
        email: "a@x.io",
        role: "Member",
        expiresAt: iso(DAY),
        isAccepted: false,
        createdAt: iso(-2 * DAY),
      },
      {
        id: "2",
        email: "b@x.io",
        role: "Admin",
        expiresAt: iso(-DAY),
        isAccepted: false,
        createdAt: iso(-DAY),
      },
      {
        id: "3",
        email: "c@x.io",
        role: "Guest",
        expiresAt: iso(DAY),
        isAccepted: true,
        createdAt: iso(-DAY),
      },
      {
        id: "4",
        email: "d@x.io",
        role: "Guest",
        expiresAt: iso(DAY),
        isAccepted: false,
        createdAt: iso(-1),
      },
      {
        id: "5",
        email: "e@x.io",
        role: "Member",
        expiresAt: iso(DAY),
        isAccepted: false,
        createdAt: iso(0),
        status: "Revoked",
      },
      { id: 6, email: "bad" },
    ]);
    expect(invitations.map((invitation) => invitation.id)).toEqual([
      "1",
      "2",
      "3",
      "4",
    ]);
    expect(invitationState(invitations[1]!, NOW)).toBe("expired");
    expect(invitationState(invitations[2]!, NOW)).toBe("accepted");
    expect(
      pendingInvitations(invitations, NOW).map((entry) => entry.id),
    ).toEqual(["4", "1"]);
  });

  it("reads the producer status: revoked and declined drop, expired stays expired", () => {
    const invitations = parseInvitations([
      {
        id: "p",
        email: "p@x.io",
        role: "Member",
        expiresAt: iso(DAY),
        isAccepted: false,
        createdAt: iso(-DAY),
        status: "Pending",
      },
      {
        id: "x",
        email: "x@x.io",
        role: "Member",
        expiresAt: iso(DAY),
        isAccepted: false,
        createdAt: iso(-DAY),
        status: "Expired",
      },
      {
        id: "d",
        email: "d@x.io",
        role: "Member",
        expiresAt: iso(DAY),
        isAccepted: false,
        createdAt: iso(-DAY),
        status: "Declined",
      },
    ]);
    expect(invitations.map((invitation) => invitation.id)).toEqual(["p", "x"]);
    expect(invitationState(invitations[1]!, NOW)).toBe("expired");
  });

  it("finds the invitation already waiting at an address, ignoring case and spaces", () => {
    const invitations = parseInvitations([
      {
        id: "pending",
        email: "an@x.io",
        role: "Member",
        expiresAt: iso(DAY),
        isAccepted: false,
        createdAt: iso(-DAY),
        status: "Pending",
      },
      {
        id: "lapsed",
        email: "old@x.io",
        role: "Member",
        expiresAt: iso(-DAY),
        isAccepted: false,
        createdAt: iso(-8 * DAY),
        status: "Pending",
      },
      {
        id: "revoked",
        email: "gone@x.io",
        role: "Member",
        expiresAt: iso(DAY),
        isAccepted: false,
        createdAt: iso(-DAY),
        status: "Revoked",
      },
    ]);
    expect(pendingInvitationFor(invitations, " AN@x.io ", NOW)?.id).toBe(
      "pending",
    );
    // A lapsed or revoked invitation does not block a new one (the producer
    // only rejects a pending, unexpired invitation to the same address).
    expect(pendingInvitationFor(invitations, "old@x.io", NOW)).toBeNull();
    expect(pendingInvitationFor(invitations, "gone@x.io", NOW)).toBeNull();
    expect(pendingInvitationFor(invitations, "  ", NOW)).toBeNull();
  });

  it("invites on the members route with the role name and revokes by id", async () => {
    const api = {
      get: vi.fn(),
      post: vi.fn().mockResolvedValue("inv-9"),
      put: vi.fn(),
      patch: vi.fn(),
      delete: vi.fn().mockResolvedValue(undefined),
    };
    const service = createWorkspaceInvitationsService(api, {
      workspaces: {
        members: (id) => `/workspaces/${id}/members`,
        invitations: (id) => `/workspaces/${id}/invitations`,
        invitation: (id, inv) => `/workspaces/${id}/invitations/${inv}`,
        invitationRole: (id, inv) =>
          `/workspaces/${id}/invitations/${inv}/role`,
        resendInvitation: (id, inv) =>
          `/workspaces/${id}/invitations/${inv}/resend`,
      },
    });
    await service.changeRole("ws", "inv-9", "guest", 2);
    expect(api.patch).toHaveBeenCalledWith(
      "/workspaces/ws/invitations/inv-9/role",
      { newRole: 0, expectedVersion: 2 },
    );
    await service.resend("ws", "inv-9");
    expect(api.post).toHaveBeenCalledWith(
      "/workspaces/ws/invitations/inv-9/resend",
      {},
    );
    api.post.mockClear();
    await expect(
      service.create("ws", { email: " a@x.io ", role: "guest" }),
    ).resolves.toBe("inv-9");
    expect(api.post).toHaveBeenCalledWith(
      "/workspaces/ws/members",
      { email: "a@x.io", role: "Guest" },
      undefined,
    );
    // A retry of the same attempt reuses its key, so no second email goes out.
    await service.create("ws", { email: "a@x.io", role: "guest" }, "attempt-1");
    expect(api.post).toHaveBeenLastCalledWith(
      "/workspaces/ws/members",
      { email: "a@x.io", role: "Guest" },
      { idempotencyKey: "attempt-1" },
    );
    await service.revoke("ws", "inv-9");
    expect(api.delete).toHaveBeenCalledWith("/workspaces/ws/invitations/inv-9");
  });
});

describe("Producer adapters", () => {
  it("parses the caller's own membership row (members/me) and nothing else", () => {
    const me = parseMyMembership({
      userId: "g1",
      name: "Gia",
      role: "Guest",
      status: "Active",
    });
    expect(me).toMatchObject({
      userId: "g1",
      role: "guest",
      membership: "active",
    });
    expect(parseMyMembership({ userId: "g1", role: "Root" })).toBeNull();
    expect(parseMyMembership(null)).toBeNull();
  });

  it("parses members, normalizing role case and dropping invalid or removed rows", () => {
    const members = parseMembers([
      {
        userId: "u1",
        name: "Ada",
        avatar: "https://a/1.png",
        role: "Owner",
        joinedAt: "2026-01-01T00:00:00Z",
      },
      { userId: "u2", name: "", role: "Member" },
      { userId: "u3", name: "Gone", role: "Member", status: "Removed" },
      { userId: "u4", name: "Bad", role: "Root" },
      { userId: "u1", name: "Dup", role: "Admin" },
    ]);
    expect(
      members.map((entry) => [entry.userId, entry.role, entry.name]),
    ).toEqual([
      ["u1", "owner", "Ada"],
      ["u2", "member", "u2"],
    ]);
    expect(members[0]!.avatarUrl).toBe("https://a/1.png");
    expect(members[0]!.membership).toBeUndefined();
  });

  it("reads the member list a page at a time and joins the pages in order", async () => {
    const api = {
      get: vi
        .fn()
        .mockResolvedValueOnce({
          items: [
            { userId: "u1", name: "Ada", role: "Owner", jobTitle: "Founder" },
            { userId: "u2", name: "Alan", role: "Member", jobTitle: "  " },
          ],
          nextCursor: "c1",
        })
        .mockResolvedValueOnce({
          items: [{ userId: "u3", name: "Grace", role: "Guest" }],
          nextCursor: null,
        }),
      post: vi.fn(),
      put: vi.fn(),
      patch: vi.fn(),
      delete: vi.fn(),
    };

    const list = await createMembersService(api).getList("ws");

    expect(api.get.mock.calls.map(([url]) => url)).toEqual([
      "/workspaces/ws/members?limit=200",
      "/workspaces/ws/members?limit=200&cursor=c1",
    ]);
    expect(list.map((member) => member.userId)).toEqual(["u1", "u2", "u3"]);
    expect(list[0]!.jobTitle).toBe("Founder");
    expect(list[1]!.jobTitle).toBeUndefined();
  });

  it("stops reading pages when a cursor never ends", async () => {
    const api = {
      get: vi.fn().mockResolvedValue({
        items: [{ userId: "u1", name: "Ada", role: "Owner" }],
        nextCursor: "again",
      }),
      post: vi.fn(),
      put: vi.fn(),
      patch: vi.fn(),
      delete: vi.fn(),
    };

    const list = await createMembersService(api).getList("ws");

    expect(api.get).toHaveBeenCalledTimes(100);
    expect(list).toHaveLength(1);
  });

  it("reads a list that has no rows as empty", async () => {
    const api = {
      get: vi.fn().mockResolvedValue({ items: null, nextCursor: null }),
      post: vi.fn(),
      put: vi.fn(),
      patch: vi.fn(),
      delete: vi.fn(),
    };

    expect(await createMembersService(api).getList("ws")).toEqual([]);
  });

  it("changes a role with PATCH and the producer enum value", async () => {
    const api = {
      get: vi.fn(),
      post: vi.fn(),
      put: vi.fn(),
      patch: vi.fn(),
      delete: vi.fn(),
    };
    const service = createMembersService(api);
    await service.updateRole("ws", "u2", "admin", 4);
    expect(api.patch).toHaveBeenCalledWith("/workspaces/ws/members/u2", {
      role: 2,
      expectedVersion: 4,
    });
    await service.suspend("ws", "u2", 4);
    expect(api.post).toHaveBeenCalledWith("/workspaces/ws/members/u2/suspend", {
      expectedVersion: 4,
    });
    await service.activate("ws", "u2", 4);
    expect(api.post).toHaveBeenCalledWith(
      "/workspaces/ws/members/u2/activate",
      { expectedVersion: 4 },
    );
  });

  it("keeps only complete activity entries", async () => {
    expect(
      parseActivityPage({ data: [], total: 0, page: 1, pageSize: 20 }),
    ).toEqual({
      entries: [],
      total: 0,
    });
    const api = {
      get: vi.fn().mockResolvedValue({
        data: [
          {
            id: "a",
            actor: "Ada",
            action: "edited",
            target: "Plan",
            createdAt: iso(0),
          },
          { id: "b", action: "edited" },
        ],
        total: 2,
      }),
      post: vi.fn(),
      put: vi.fn(),
      patch: vi.fn(),
      delete: vi.fn(),
    };
    const page = await createActivityService(api, {
      workspaces: { activity: (id) => `/workspaces/${id}/activity` },
    }).getPage("ws", 1, 10);
    expect(api.get).toHaveBeenCalledWith(
      "/workspaces/ws/activity?page=1&pageSize=10",
    );
    expect(page.entries.map((entry) => entry.id)).toEqual(["a"]);
    expect(page.total).toBe(2);
  });

  it("writes the profile to /profile with the expected version and reads settings", async () => {
    const api = {
      get: vi.fn().mockResolvedValue({
        allowPublicSharing: true,
        enforceMfa: false,
        allowGuestInvites: true,
        defaultMemberRole: "Guest",
        invitationExpiryDays: 14,
      }),
      post: vi.fn(),
      put: vi.fn(),
      patch: vi.fn(),
      delete: vi.fn(),
    };
    const service = createWorkspaceSettingsService(api, {
      workspaces: {
        profile: (id) => `/workspaces/${id}/profile`,
        settings: (id) => `/workspaces/${id}/settings`,
        archive: (id) => `/workspaces/${id}/archive`,
      },
    });
    await service.updateProfile("ws", {
      name: " Product ",
      description: " Team space ",
      expectedVersion: 4,
    });
    expect(api.patch).toHaveBeenCalledWith("/workspaces/ws/profile", {
      name: "Product",
      description: "Team space",
      expectedVersion: 4,
    });
    const settings = await service.getSettings("ws");
    expect(settings.defaultMemberRole).toBe("guest");
    await service.updateSettings("ws", settings, 4);
    expect(api.put).toHaveBeenCalledWith(
      "/workspaces/ws/settings",
      expect.objectContaining({
        defaultMemberRole: "Guest",
        expectedVersion: 4,
      }),
    );
    await service.archive("ws", 4);
    expect(api.post).toHaveBeenCalledWith("/workspaces/ws/archive", {
      expectedVersion: 4,
    });
    expect(
      parseWorkspaceSettings({ defaultMemberRole: "Owner" }).defaultMemberRole,
    ).toBe("member");
  });

  it("reads the version on every row of the workspace list", async () => {
    const api = {
      get: vi.fn().mockResolvedValue([
        { id: "a", name: "A", version: 3 },
        { id: "b", name: "B" },
      ]),
      post: vi.fn(),
      put: vi.fn(),
      patch: vi.fn(),
      delete: vi.fn(),
    };
    const service = createWorkspaceService(api, WORKSPACE_ENDPOINTS);
    const list = await service.getList();
    expect(list.map((workspace) => workspace.version)).toEqual([3, undefined]);
    api.get.mockResolvedValueOnce(null);
    await expect(service.getList()).resolves.toEqual([]);
  });

  it("creates a workspace in the Account and reads it back by the id the producer answers", async () => {
    const api = {
      get: vi.fn().mockResolvedValue({ id: "w-new", name: "Acme", version: 1 }),
      post: vi.fn().mockResolvedValue("w-new"),
      put: vi.fn(),
      patch: vi.fn(),
      delete: vi.fn(),
    };
    const service = createWorkspaceService(api, WORKSPACE_ENDPOINTS);
    const input = { name: "Acme", slug: "acme", isPersonal: false };
    const made = await service.create("acc-1", input);
    expect(api.post).toHaveBeenCalledWith(
      "/accounts/acc-1/workspaces",
      input,
      undefined,
    );
    expect(api.post).not.toHaveBeenCalledWith("/workspaces", expect.anything());
    // The same attempt retried reuses its key, so one workspace is made.
    await service.create("acc-1", input, "attempt-1");
    expect(api.post).toHaveBeenLastCalledWith(
      "/accounts/acc-1/workspaces",
      input,
      { idempotencyKey: "attempt-1" },
    );
    expect(api.get).toHaveBeenCalledWith("/workspaces/w-new");
    expect(made).toMatchObject({ id: "w-new", name: "Acme" });
  });

  it("keeps the workspace version only when the producer reports a positive number", () => {
    expect(parseWorkspaceSummary({ id: "w", version: 7 }).version).toBe(7);
    expect("version" in parseWorkspaceSummary({ id: "w" })).toBe(false);
    expect("version" in parseWorkspaceSummary({ id: "w", version: "7" })).toBe(
      false,
    );
  });
});

describe("describeWorkspaceFailure", () => {
  it("classifies from stable fields and keeps the producer detail for display", () => {
    expect(
      describeWorkspaceFailure({
        kind: "validation",
        status: 400,
        details: {
          errorCode: "business_rule.violation",
          detail: "Cannot downgrade the last owner of the workspace.",
        },
      }),
    ).toMatchObject({
      kind: "rule",
      detail: "Cannot downgrade the last owner of the workspace.",
    });
    // A 403 that carries the stable "email not confirmed" code is still
    // forbidden, with a reason the recovery can branch on (never the prose).
    expect(
      describeWorkspaceFailure({
        kind: "forbidden",
        status: 403,
        details: {
          errorCode: "auth.email_not_confirmed",
          detail: "Email must be confirmed before using this feature.",
        },
      }),
    ).toMatchObject({ kind: "forbidden", reason: "email-not-confirmed" });
    expect(
      describeWorkspaceFailure({
        kind: "forbidden",
        status: 403,
        details: { errorCode: "auth.forbidden", detail: "Forbidden access." },
      }).reason,
    ).toBeNull();
    expect(
      describeWorkspaceFailure({
        kind: "forbidden",
        status: 403,
        details: {
          detail: "Email must be confirmed before using this feature.",
        },
      }).reason,
    ).toBeNull();
    expect(
      describeWorkspaceFailure({ kind: "conflict", status: 409 }).kind,
    ).toBe("conflict");
    expect(describeWorkspaceFailure({ kind: "server", status: 412 }).kind).toBe(
      "conflict",
    );
    expect(
      describeWorkspaceFailure({ kind: "forbidden", status: 403 }).kind,
    ).toBe("forbidden");
    expect(
      describeWorkspaceFailure({
        kind: "validation",
        status: 400,
        validationErrors: { email: ["Invalid e-mail."] },
        details: { errorCode: "validation.failed" },
      }).fieldErrors,
    ).toEqual({ email: ["Invalid e-mail."] });
    expect(describeWorkspaceFailure(new Error("boom")).kind).toBe("unknown");
  });
});

describe("Library directory model (WX-DIR-001)", () => {
  const pins = [
    { kind: "documents.page" as const, resourceId: "d1", at: iso(0) },
  ];
  const entries = buildLibraryEntries({
    boards: [
      { id: "b1", name: "Roadmap" },
      { id: "b2", name: "Đội vận hành", updatedAt: iso(-DAY) },
    ],
    docs: [
      {
        id: "d1",
        name: "Spec 10",
        location: "Product",
        updatedAt: iso(-2 * DAY),
      },
      { id: "d2", name: "Spec 9", updatedAt: iso(-3 * DAY) },
    ],
    pins,
  });

  it("marks pinned entries from the member's pins", () => {
    expect(
      entries.filter((entry) => entry.pinned).map((entry) => entry.id),
    ).toEqual(["d1"]);
  });

  it("filters by type and diacritic-insensitive text, and keeps unknown update times last", () => {
    const docs = queryLibrary(entries, {
      ...DEFAULT_LIBRARY_QUERY,
      type: "doc",
    });
    expect(docs.map((entry) => entry.id)).toEqual(["d1", "d2"]);
    expect(
      queryLibrary(entries, { ...DEFAULT_LIBRARY_QUERY, text: "doi van" }).map(
        (e) => e.id,
      ),
    ).toEqual(["b2"]);
    expect(
      queryLibrary(entries, { ...DEFAULT_LIBRARY_QUERY, text: "product" }).map(
        (e) => e.id,
      ),
    ).toEqual(["d1"]);
    const ascending = queryLibrary(entries, {
      ...DEFAULT_LIBRARY_QUERY,
      sort: { field: "updated", direction: "asc" },
    });
    expect(ascending.map((entry) => entry.id)).toEqual([
      "d2",
      "d1",
      "b2",
      "b1",
    ]);
  });

  it("sorts names naturally and toggles sort direction", () => {
    const byName = queryLibrary(entries, {
      ...DEFAULT_LIBRARY_QUERY,
      sort: { field: "name", direction: "asc" },
    });
    expect(byName.map((entry) => entry.name)).toEqual([
      "Đội vận hành",
      "Roadmap",
      "Spec 9",
      "Spec 10",
    ]);
    expect(
      nextLibrarySort({ field: "name", direction: "asc" }, "name"),
    ).toEqual({
      field: "name",
      direction: "desc",
    });
    expect(
      nextLibrarySort({ field: "name", direction: "asc" }, "updated").direction,
    ).toBe("desc");
  });

  it("computes a bounded row window with overscan", () => {
    expect(
      libraryWindow({
        rowCount: 320,
        rowHeight: 40,
        scrollTop: 0,
        viewportHeight: 400,
      }),
    ).toEqual({ start: 0, end: 19 });
    expect(
      libraryWindow({
        rowCount: 320,
        rowHeight: 40,
        scrollTop: 4000,
        viewportHeight: 400,
      }),
    ).toEqual({ start: 92, end: 119 });
    expect(
      libraryWindow({
        rowCount: 320,
        rowHeight: 40,
        scrollTop: 99999,
        viewportHeight: 400,
      }).end,
    ).toBe(320);
  });
});
