import { describe, expect, it } from "vitest";
import {
  destinationHref,
  resolveShellLocation,
  routerTarget,
  type MatchedRoute,
} from "../shell/workspace/workspace-destinations";
import { resolveWorkspaceScope } from "../shell/workspace/workspace-scope";

const root: MatchedRoute = { routeId: "__root__", params: {}, search: {} };
const workspace: MatchedRoute = {
  routeId: "/workspaces/$workspaceId",
  params: { workspaceId: "ws-1" },
  search: {},
};

describe("workspace shell routing (WX-SHELL-001/002)", () => {
  it("maps every shell destination to a real route, never the workspace root", () => {
    expect(destinationHref("ws-1", { section: "home" })).toBe(
      "/workspaces/ws-1",
    );
    expect(destinationHref("ws-1", { section: "my-work" })).toBe(
      "/workspaces/ws-1/my-work",
    );
    expect(destinationHref("ws-1", { section: "inbox" })).toBe(
      "/workspaces/ws-1/inbox",
    );
    expect(destinationHref("ws-1", { section: "search" })).toBe(
      "/workspaces/ws-1/search",
    );
    expect(destinationHref("ws-1", { section: "members" })).toBe(
      "/workspaces/ws-1/members",
    );
    expect(destinationHref("ws-1", { section: "settings" })).toBe(
      "/workspaces/ws-1/settings",
    );
    expect(
      destinationHref("ws-1", { section: "account", page: "appearance" }),
    ).toBe("/workspaces/ws-1/account/appearance");
    expect(
      destinationHref("ws-1", {
        section: "board",
        boardId: "b 1",
        view: "table",
      }),
    ).toBe("/workspaces/ws-1/boards/b%201?view=table");
    expect(routerTarget("ws-1", { section: "doc", docId: "d1" })).toEqual({
      to: "/workspaces/$workspaceId/docs/$docId",
      params: { workspaceId: "ws-1", docId: "d1" },
    });
  });

  it("resolves the location from matched route ids and params, not from the URL", () => {
    expect(
      resolveShellLocation([
        root,
        workspace,
        {
          routeId: "/workspaces/$workspaceId/boards/$boardId",
          params: { workspaceId: "ws-1", boardId: "b1" },
          search: { view: "timeline" },
        },
      ]),
    ).toEqual({ section: "board", boardId: "b1", view: "timeline" });

    expect(
      resolveShellLocation([
        root,
        workspace,
        {
          routeId: "/workspaces/$workspaceId/docs/$docId",
          params: { docId: "d1" },
          search: {},
        },
      ]),
    ).toEqual({ section: "doc", docId: "d1" });

    expect(
      resolveShellLocation([
        root,
        workspace,
        { routeId: "/workspaces/$workspaceId/", params: {}, search: {} },
      ]),
    ).toEqual({ section: "home" });
    expect(
      resolveShellLocation([
        root,
        workspace,
        { routeId: "/workspaces/$workspaceId/account", params: {}, search: {} },
        {
          routeId: "/workspaces/$workspaceId/account/profile",
          params: {},
          search: {},
        },
      ]),
    ).toEqual({ section: "account" });
    expect(resolveShellLocation([root, workspace])).toBeNull();
  });
});

describe("resolveWorkspaceScope (WX-SHELL-004)", () => {
  const ready = {
    workspaceId: "ws-1",
    list: { isLoading: false, isError: false, ids: ["ws-1", "ws-2"] },
    detail: {
      isLoading: false,
      isError: false,
      hasData: true,
      errorKind: undefined,
    },
  };

  it("is ready when the workspace is listed and loaded", () => {
    expect(resolveWorkspaceScope(ready)).toBeNull();
  });

  it("treats a workspace missing from the member's list as lost access", () => {
    expect(
      resolveWorkspaceScope({
        ...ready,
        list: { ...ready.list, ids: ["ws-2"] },
      }),
    ).toBe("forbidden");
  });

  it("separates forbidden and not-found answers from a failed load", () => {
    const failed = (errorKind: string) =>
      resolveWorkspaceScope({
        ...ready,
        detail: { isLoading: false, isError: true, hasData: false, errorKind },
      });
    expect(failed("forbidden")).toBe("forbidden");
    expect(failed("not_found")).toBe("forbidden");
    expect(failed("network")).toBe("error");
    expect(failed("server")).toBe("error");
  });

  it("waits while either the list or the record loads", () => {
    expect(
      resolveWorkspaceScope({
        ...ready,
        list: { isLoading: true, isError: false, ids: undefined },
      }),
    ).toBe("loading");
    expect(
      resolveWorkspaceScope({
        ...ready,
        detail: {
          isLoading: true,
          isError: false,
          hasData: false,
          errorKind: undefined,
        },
      }),
    ).toBe("loading");
  });

  it("reports a failed list as an error", () => {
    expect(
      resolveWorkspaceScope({
        ...ready,
        list: { isLoading: false, isError: true, ids: undefined },
      }),
    ).toBe("error");
  });
});
