import { describe, expect, it } from "vitest";
import {
  CHAT_LINKABLE_KINDS,
  isLinkableKind,
  parseLinkInput,
  resourceHref,
} from "../core/model/link-model";

const WS = "w1";

describe("parseLinkInput (UC-CHT-H10)", () => {
  it("reads a Board address, with or without an item or a host, as the Board", () => {
    const board = { kind: "work-management.board", resourceId: "b-1" };
    expect(
      parseLinkInput(`/workspaces/${WS}/boards/b-1`, WS, "documents.page"),
    ).toEqual(board);
    expect(
      parseLinkInput(
        `https://app.test/workspaces/${WS}/boards/b-1?item=i-2`,
        WS,
        "documents.page",
      ),
    ).toEqual(board);
  });

  it("reads a Page address and a bare id with the chosen kind", () => {
    expect(
      parseLinkInput(
        ` /workspaces/${WS}/docs/p-1 `,
        WS,
        "work-management.board",
      ),
    ).toEqual({
      kind: "documents.page",
      resourceId: "p-1",
    });
    expect(parseLinkInput("p-9", WS, "documents.page")).toEqual({
      kind: "documents.page",
      resourceId: "p-9",
    });
  });

  it("reads a Team address as the Team (UC-CHT-H11)", () => {
    expect(
      parseLinkInput(`/workspaces/${WS}/teams/t-1`, WS, "documents.page"),
    ).toEqual({ kind: "governance.team", resourceId: "t-1" });
    expect(parseLinkInput("t-9", WS, "governance.team")).toEqual({
      kind: "governance.team",
      resourceId: "t-9",
    });
  });

  it("refuses another Workspace, another site's path, an unknown route and an empty or oversized text", () => {
    for (const text of [
      "",
      "   ",
      `/workspaces/other/boards/b-1`,
      `/workspaces/${WS}/reports/r-1`,
      `javascript:alert(1)`,
      `/workspaces/${WS}/boards/b-1/extra`,
      "x".repeat(2049),
      "two words",
    ]) {
      expect(
        parseLinkInput(text, WS, "work-management.board"),
        text,
      ).toBeNull();
    }
  });
});

describe("resourceHref and the linkable kinds", () => {
  it("is the product route of a Board, Page or Team, encoded, and none for an item", () => {
    expect(resourceHref("work-management.board", "w 1", "b/1")).toBe(
      "/workspaces/w%201/boards/b%2F1",
    );
    expect(resourceHref("documents.page", WS, "p-1")).toBe(
      `/workspaces/${WS}/docs/p-1`,
    );
    expect(resourceHref("governance.team", WS, "t 1")).toBe(
      `/workspaces/${WS}/teams/t%201`,
    );
    expect(resourceHref("work-management.board-item", WS, "i-1")).toBeNull();
  });

  it("offers a Board, a Page and a Team, never an item", () => {
    expect([...CHAT_LINKABLE_KINDS]).toEqual([
      "work-management.board",
      "documents.page",
      "governance.team",
    ]);
    expect(isLinkableKind("governance.team")).toBe(true);
    expect(isLinkableKind("work-management.board-item")).toBe(false);
  });
});
