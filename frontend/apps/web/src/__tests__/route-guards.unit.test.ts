import { describe, expect, it } from "vitest";
import { RouteGuardError } from "../router/guards/errors";
import { requireEntitlement } from "../router/guards/require-entitlement";
import { requireFeatureFlag } from "../router/guards/require-feature-flag";
import { requirePermission } from "../router/guards/require-permission";
import {
  requireWorkspaceId,
  requireWorkspaceMembership,
} from "../router/guards/require-workspace-membership";

describe("route guard utilities", () => {
  it("validates workspace route params before component render", () => {
    expect(requireWorkspaceId({ workspaceId: "workspace_1" })).toBe(
      "workspace_1",
    );

    try {
      requireWorkspaceId({ workspaceId: "../escape" });
      throw new Error("Expected invalid workspace id to throw");
    } catch (error) {
      expect(error).toBeDefined();
    }
  });

  it("throws typed guard errors for missing membership, permissions, entitlements and feature flags", () => {
    expect(() =>
      requireWorkspaceMembership({
        workspaceId: "ws-2",
        memberWorkspaceIds: ["ws-1"],
      }),
    ).toThrow(RouteGuardError);

    expect(() =>
      requirePermission({
        permission: "board.update",
        can: () => false,
      }),
    ).toThrow(RouteGuardError);

    expect(() =>
      requireEntitlement({
        entitlement: "workManagement.timeline",
        hasEntitlement: () => false,
      }),
    ).toThrow(RouteGuardError);

    expect(() =>
      requireFeatureFlag({
        flag: "timeline-v2",
        isFeatureEnabled: () => false,
      }),
    ).toThrow(RouteGuardError);
  });
});
