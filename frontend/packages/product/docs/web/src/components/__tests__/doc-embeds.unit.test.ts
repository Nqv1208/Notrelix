import { describe, expect, it } from "vitest";
import type { Block } from "@notrelix/docs-core";
import { toBlockViews } from "../doc-page-mapping";
import type { DocBoardEmbed } from "../doc-page-models";

function block(
  id: string,
  type: Block["type"],
  properties: Block["properties"],
): Block {
  return {
    id,
    pageId: "page-1",
    type,
    properties,
    position: 1,
    parentId: null,
    createdById: "u",
    updatedById: "u",
    createdAt: "",
    updatedAt: "",
  };
}

const context = {
  resolveTask: (id: string) => ({ id, restricted: true as const }),
  board: null,
  quotes: [],
};

const READY: DocBoardEmbed = {
  state: "ready",
  name: "Sprint 12",
  href: "/b/1",
  counts: { todo: 3, doing: 2, review: 0, done: 5 },
  total: 10,
  tasks: [],
};

function liveTable(
  properties: Block["properties"],
  boards?: Map<string, DocBoardEmbed>,
) {
  const view = toBlockViews([block("e", "board_reference", properties)], {
    ...context,
    boards,
  })[0];
  if (view?.kind !== "liveTable") throw new Error("not a live table");
  return view;
}

describe("board embed view", () => {
  it("is unset until a board is chosen", () => {
    expect(liveTable({}).state).toBe("unset");
  });

  it("loads while the host has not read the board yet", () => {
    expect(liveTable({ linkedBoardId: "b1" }).state).toBe("loading");
  });

  it("shows the board's name, counts and link once read", () => {
    const view = liveTable({ linkedBoardId: "b1" }, new Map([["b1", READY]]));
    expect(view).toMatchObject({
      state: "ready",
      boardName: "Sprint 12",
      boardHref: "/b/1",
      total: 10,
      counts: { todo: 3, doing: 2, review: 0, done: 5 },
    });
  });

  it("reports a board the viewer cannot read as unavailable without its name", () => {
    const view = liveTable(
      { linkedBoardId: "b1" },
      new Map([
        ["b1", { ...READY, state: "unavailable" as const, name: "", total: 0 }],
      ]),
    );
    expect(view.state).toBe("unavailable");
    expect(view.boardName).toBe("");
    expect(view.counts).toBeUndefined();
  });
});

describe("page reference view", () => {
  it("never says whether an unreadable page exists", () => {
    const view = toBlockViews(
      [block("r", "page_reference", { linkedPageId: "gone" })],
      {
        ...context,
        pages: new Map(),
      },
    )[0];
    expect(view).toMatchObject({
      kind: "pageReference",
      pageId: "gone",
      title: null,
      unavailable: true,
    });
  });

  it("shows the title and icon of a readable page", () => {
    const view = toBlockViews(
      [block("r", "page_reference", { linkedPageId: "p2" })],
      {
        ...context,
        pages: new Map([["p2", { title: "Policy", icon: "📄" }]]),
        pageHref: (id) => `/docs/${id}`,
      },
    )[0];
    expect(view).toMatchObject({
      title: "Policy",
      icon: "📄",
      href: "/docs/p2",
      unavailable: false,
    });
  });
});
