import { describe, expect, it } from "vitest";
import { assertNotrelixQueryKey } from "@notrelix/query";
import { workspaceQueryKeys } from "../query/keys";

describe("workspaceQueryKeys Q7 purity tests", () => {
  it("every workspaceQueryKey satisfies assertNotrelixQueryKey", () => {
    const wsId = "ws-555";

    const keys = [
      workspaceQueryKeys.all,
      workspaceQueryKeys.detail(wsId),
      workspaceQueryKeys.snapshot(wsId),
      workspaceQueryKeys.members(wsId),
      workspaceQueryKeys.views(wsId),
      workspaceQueryKeys.activeView(wsId, "v1"),
      workspaceQueryKeys.invitations(wsId),
      workspaceQueryKeys.invitationByToken("tok-123"),
      workspaceQueryKeys.pendingInvitations,
      workspaceQueryKeys.activity(wsId),
    ];

    for (const key of keys) {
      expect(() => assertNotrelixQueryKey(key)).not.toThrow();
    }
  });
});
