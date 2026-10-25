import { describe, expect, it, vi } from "vitest";
import {
  AVATAR_MAX_BYTES,
  checkAvatarFile,
  createAccountService,
  leaveFailureOf,
  photoFailureOf,
  toWorkspaceEntry,
  type AccountApiClient,
  type AccountEndpoints,
} from "../core";

const endpoints = {
  auth: {},
  users: { avatar: "/profile/avatar" },
  account: {
    workspaces: "/account/workspaces",
    defaultWorkspace: "/account/default-workspace",
    leaveWorkspace: (id: string) => `/account/workspaces/${id}`,
  },
  workspaces: {},
} as unknown as AccountEndpoints;

type Mocked = Record<
  "get" | "post" | "put" | "patch" | "delete" | "upload",
  ReturnType<typeof vi.fn>
>;

function client(overrides: Partial<Mocked> = {}) {
  const api: Mocked = {
    get: vi.fn(),
    post: vi.fn(),
    put: vi.fn(async () => undefined),
    patch: vi.fn(),
    delete: vi.fn(async () => undefined),
    upload: vi.fn(),
    ...overrides,
  };
  return {
    api,
    service: createAccountService(
      api as unknown as AccountApiClient,
      endpoints,
    ),
  };
}

describe("the workspace adapter", () => {
  it("lowercases the role and keeps the counts nullable, with nothing invented", () => {
    expect(
      toWorkspaceEntry({
        id: "w1",
        name: "Minh An",
        role: "Owner",
        memberCount: 12,
        departmentCount: null,
        isDefault: true,
        canLeave: false,
      }),
    ).toEqual({
      id: "w1",
      name: "Minh An",
      role: "owner",
      memberCount: 12,
      departmentCount: null,
      isDefault: true,
      canLeave: false,
    });
    expect(toWorkspaceEntry({ id: "w2", role: "Guest" })).toMatchObject({
      role: "guest",
      memberCount: null,
      departmentCount: null,
    });
    // An unknown role is shown as an ordinary member, never as an owner.
    expect(toWorkspaceEntry({ id: "w3", role: "Auditor" }).role).toBe("member");
  });

  it("lists, sets the default and leaves through the producer's routes", async () => {
    const { api, service } = client({
      get: vi.fn(async () => [
        {
          id: "w1",
          name: "A",
          role: "Admin",
          memberCount: 3,
          departmentCount: 1,
        },
      ]),
    });
    expect(await service.listMyWorkspaces()).toEqual([
      expect.objectContaining({ id: "w1", role: "admin", memberCount: 3 }),
    ]);
    expect(api.get).toHaveBeenCalledWith("/account/workspaces", {
      skipGlobalErrorToast: true,
    });
    await service.setDefaultWorkspace("w1");
    expect(api.put).toHaveBeenCalledWith(
      "/account/default-workspace",
      { workspaceId: "w1" },
      { skipGlobalErrorToast: true },
    );
    await service.leaveWorkspace("w1");
    expect(api.delete).toHaveBeenCalledWith("/account/workspaces/w1", {
      skipGlobalErrorToast: true,
    });
  });

  it("reads the last-owner refusal by its code, nothing else", () => {
    expect(
      leaveFailureOf({ code: "workspaces.member.last-owner-cannot-leave" }),
    ).toBe("last-owner");
    expect(leaveFailureOf({ code: "workspaces.member.not-found" })).toBe(
      "failed",
    );
    expect(leaveFailureOf(new Error("last owner"))).toBe("failed");
    expect(leaveFailureOf(null)).toBe("failed");
  });
});

describe("the profile photo", () => {
  it("is checked on type and size before it is sent", () => {
    expect(checkAvatarFile({ type: "image/png", size: 10 })).toBeNull();
    expect(
      checkAvatarFile({ type: "image/webp", size: AVATAR_MAX_BYTES }),
    ).toBe(null);
    expect(checkAvatarFile({ type: "image/gif", size: 10 })).toBe(
      "unsupported",
    );
    expect(checkAvatarFile({ type: "image/svg+xml", size: 10 })).toBe(
      "unsupported",
    );
    expect(
      checkAvatarFile({ type: "image/jpeg", size: AVATAR_MAX_BYTES + 1 }),
    ).toBe("too-large");
  });

  it("sends the File as the part `file` with its name over PUT, and answers the relative address", async () => {
    const { api, service } = client({
      upload: vi.fn(async () => ({ avatarUrl: "/api/v1/users/u/avatar?v=1" })),
    });
    const file = new File(["x"], "me.png", { type: "image/png" });
    expect(await service.uploadAvatar(file)).toBe("/api/v1/users/u/avatar?v=1");
    const [url, form, options] = api.upload.mock.calls[0] as unknown as [
      string,
      FormData,
      unknown,
    ];
    expect(url).toBe("/profile/avatar");
    expect(options).toEqual({ method: "PUT" });
    expect(Array.from(form.keys())).toEqual(["file"]);
    expect((form.get("file") as File).name).toBe("me.png");
    expect(form.get("file")).toBeInstanceOf(File);
  });

  it("removes through DELETE", async () => {
    const { api, service } = client();
    await service.removeAvatar();
    expect(api.delete).toHaveBeenCalledWith("/profile/avatar", {
      skipGlobalErrorToast: true,
    });
  });

  it("maps the producer's two refusals by code", () => {
    expect(
      photoFailureOf({ status: 413, code: "identity.avatar.too-large" }),
    ).toBe("too-large");
    expect(
      photoFailureOf({
        status: 415,
        code: "identity.avatar.unsupported-format",
      }),
    ).toBe("unsupported");
    expect(photoFailureOf({ status: 500 })).toBe("failed");
    expect(photoFailureOf(new TypeError("offline"))).toBe("failed");
  });
});
