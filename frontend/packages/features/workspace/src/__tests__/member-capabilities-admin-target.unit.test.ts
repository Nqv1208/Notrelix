import { describe, expect, it } from "vitest";
import { memberCapabilities } from "../core/model/members";
import type { WorkspaceMember } from "../core/types/workspace";

const member = (userId: string, role: WorkspaceMember["role"]) =>
  ({
    id: userId,
    userId,
    name: userId,
    role,
    membership: "active",
    version: 1,
  }) satisfies WorkspaceMember;

describe("member capabilities against the producer's actor-versus-target rule", () => {
  const admin = { userId: "u-admin", role: "admin" as const };

  it("offers an admin no remove or suspend on another admin (only an owner may)", () => {
    const caps = memberCapabilities(member("u-other-admin", "admin"), admin, 1);
    expect(caps.canRemove).toBe(false);
    expect(caps.canSuspend).toBe(false);
    expect(caps.canActivate).toBe(false);
  });

  it("still offers an admin remove and suspend on a member or a guest", () => {
    for (const role of ["member", "guest"] as const) {
      const caps = memberCapabilities(member("u-x", role), admin, 1);
      expect(caps.canRemove).toBe(true);
      expect(caps.canSuspend).toBe(true);
    }
  });

  it("offers an owner remove and suspend on an admin", () => {
    const caps = memberCapabilities(
      member("u-a", "admin"),
      { userId: "u-owner", role: "owner" },
      1,
    );
    expect(caps.canRemove).toBe(true);
    expect(caps.canSuspend).toBe(true);
  });
});
