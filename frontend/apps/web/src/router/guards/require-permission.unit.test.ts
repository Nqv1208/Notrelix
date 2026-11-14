import { describe, expect, it, vi } from "vitest";
import type { Permission } from "@notrelix/platform/permissions-core";
import { RouteGuardError } from "./errors";
import { requirePermission } from "./require-permission";

const permission = "workspace.settings.manage" as Permission;

describe("requirePermission", () => {
  it("lets a person who holds the permission through and asks with the resource context", () => {
    const can = vi.fn(() => true);
    const context = { workspaceId: "ws-1" } as never;

    expect(() => requirePermission({ permission, context, can })).not.toThrow();
    expect(can).toHaveBeenCalledWith(permission, context);
  });

  it("refuses a person who lacks it with a forbidden guard error", () => {
    const can = vi.fn(() => false);

    expect.assertions(3);
    try {
      requirePermission({ permission, can });
    } catch (error) {
      expect(error).toBeInstanceOf(RouteGuardError);
      expect((error as RouteGuardError).kind).toBe("forbidden");
      expect((error as RouteGuardError).status).toBe(403);
    }
  });
});
