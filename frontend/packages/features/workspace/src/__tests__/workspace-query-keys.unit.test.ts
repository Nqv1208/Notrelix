import { describe, expect, it } from "vitest";
import { assertNotrelixQueryKey } from "@notrelix/query";
import { workspaceQueryKeys } from "../query/keys";

describe("workspaceQueryKeys Q7 purity tests", () => {
  it("every workspaceQueryKey satisfies assertNotrelixQueryKey", () => {
    const wsId = "ws-555";

    const keys = [
      workspaceQueryKeys.all("acc-1"),
      workspaceQueryKeys.detail(wsId),
      workspaceQueryKeys.snapshot(wsId),
      workspaceQueryKeys.members(wsId),
      workspaceQueryKeys.views(wsId),
      workspaceQueryKeys.activeView(wsId, "v1"),
      workspaceQueryKeys.invitations(wsId),
      workspaceQueryKeys.invitationByToken("tok-123"),
      workspaceQueryKeys.pendingInvitations("acc-1"),
      workspaceQueryKeys.activity(wsId),
      workspaceQueryKeys.navigation(wsId),
      workspaceQueryKeys.recents(wsId),
      workspaceQueryKeys.pins(wsId),
      workspaceQueryKeys.spaces(wsId),
      workspaceQueryKeys.spaceTree(wsId),
      workspaceQueryKeys.spaceMembers(wsId, "space-1"),
      workspaceQueryKeys.teams(wsId),
      workspaceQueryKeys.team(wsId, "team-1"),
      workspaceQueryKeys.teamMembers(wsId, "team-1"),
    ];

    for (const key of keys) {
      expect(() => assertNotrelixQueryKey(key)).not.toThrow();
    }
  });

  it("navigation keys are scoped by workspace and nest under the navigation root", () => {
    const a = workspaceQueryKeys.recents("ws-a");
    const b = workspaceQueryKeys.recents("ws-b");
    expect(a).not.toEqual(b);
    expect(JSON.stringify(a)).toContain("ws-a");
    const root = workspaceQueryKeys.navigation("ws-a");
    expect(a.slice(0, root.length)).toEqual(root);
    expect(workspaceQueryKeys.pins("ws-a").slice(0, root.length)).toEqual(root);
  });

  it("space keys are scoped by workspace and nest under the spaces root", () => {
    const root = workspaceQueryKeys.spaces("ws-a");
    const tree = workspaceQueryKeys.spaceTree("ws-a");
    const members = workspaceQueryKeys.spaceMembers("ws-a", "space-1");
    expect(tree).not.toEqual(workspaceQueryKeys.spaceTree("ws-b"));
    expect(tree.slice(0, root.length)).toEqual(root);
    expect(members.slice(0, root.length)).toEqual(root);
    expect(JSON.stringify(members)).toContain("space-1");
  });
});
