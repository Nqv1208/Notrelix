import { describe, expect, it } from "vitest";

import {
  PRIMARY_NAV_CONTRIBUTIONS,
  SUPPORT_NAV_CONTRIBUTIONS,
  WORKSPACE_NAV_CONTRIBUTIONS,
  isWorkspaceNavigationItemActive,
  resolveWorkspaceNavigationPath,
} from "../navigation-contributions";

describe("workspace navigation contributions", () => {
  const allItems = [
    ...PRIMARY_NAV_CONTRIBUTIONS,
    ...WORKSPACE_NAV_CONTRIBUTIONS,
    ...SUPPORT_NAV_CONTRIBUTIONS,
  ];

  it("exposes only independently addressable workspace routes", () => {
    const paths = allItems.map((item) =>
      resolveWorkspaceNavigationPath(item, "ws-1"),
    );

    expect(new Set(paths).size).toBe(paths.length);
    expect(paths).not.toContain("/workspaces/ws-1");
    expect(paths).toEqual(
      expect.arrayContaining([
        "/workspaces/ws-1/dashboard",
        "/workspaces/ws-1/chat",
        "/workspaces/ws-1/members",
        "/workspaces/ws-1/billing",
        "/workspaces/ws-1/settings",
      ]),
    );
  });

  it("marks only the matching destination active", () => {
    const overview = PRIMARY_NAV_CONTRIBUTIONS[0]!;
    const chat = PRIMARY_NAV_CONTRIBUTIONS[1]!;

    expect(
      isWorkspaceNavigationItemActive(
        overview,
        "/workspaces/ws-1/dashboard",
        "ws-1",
      ),
    ).toBe(true);
    expect(
      isWorkspaceNavigationItemActive(
        overview,
        "/workspaces/ws-1/chat",
        "ws-1",
      ),
    ).toBe(false);
    expect(
      isWorkspaceNavigationItemActive(
        chat,
        "/workspaces/ws-1/chat/room-1",
        "ws-1",
      ),
    ).toBe(true);
  });
});
