import { describe, expect, it } from "vitest";
import type { Block, PageComment, PageVersion } from "@notrelix/docs-core";

import { docPageCopy } from "../doc-page.copy";
import {
  editedAtLabel,
  markVersionDiff,
  positionAfter,
  referencesOf,
  splitQuote,
  toBlockViews,
  toCommentViews,
  toMarkdown,
  toTocEntries,
  toVersionViews,
} from "../doc-page-mapping";
import { taskToken } from "../doc-page-text";
import { HEADER, KL, TASK_144 } from "../../verification/docs-ui-fixtures";

const copy = docPageCopy("vi");
const NOW = new Date("2026-10-12T10:40:00+07:00");

function block(
  id: string,
  type: Block["type"],
  position: number,
  properties: Block["properties"],
): Block {
  return {
    id,
    pageId: "page-1",
    type,
    properties,
    position,
    parentId: null,
    createdById: "user-kl",
    updatedById: "user-kl",
    createdAt: "2026-10-05T07:20:00.000Z",
    updatedAt: "2026-10-05T07:20:00.000Z",
  };
}

const BLOCKS: Block[] = [
  block("h", "heading_2", 1000, { text: "Phạm vi" }),
  block("p", "paragraph", 2000, { text: `Xem ${taskToken("t-144")}.` }),
  block("n", "numbered_list", 3000, { text: "Bước một" }),
  block("t", "todo", 4000, {
    text: "Chốt danh mục",
    checked: true,
    linkedTaskId: "t-129",
  }),
  block("r", "board_reference", 5000, {
    linkedBoardId: "board-erp",
    items: ["t-142", "t-144"],
    caption: "Phân hệ Kế toán",
  }),
];

describe("document page mapping", () => {
  it("collects task and board references in reading order without duplicates", () => {
    expect(referencesOf(BLOCKS)).toEqual({
      taskIds: ["t-144", "t-129", "t-142"],
      boardIds: ["board-erp"],
    });
  });

  it("maps stored blocks to the view model", () => {
    const views = toBlockViews(BLOCKS, {
      resolveTask: (id) => ({ ...TASK_144, id }),
      board: { name: "Triển khai ERP Q4", href: "/boards/erp" },
      quotes: [],
    });

    expect(views.map((view) => view.kind)).toEqual([
      "heading",
      "paragraph",
      "numbered",
      "todo",
      "liveTable",
    ]);
    const table = views[4];
    expect(table?.kind === "liveTable" && table.boardName).toBe(
      "Triển khai ERP Q4",
    );
  });

  it("derives the contents from the headings under the page title", () => {
    expect(toTocEntries("Tiêu đề", BLOCKS)).toEqual([
      { id: "title", level: 1, text: "Tiêu đề" },
      { id: "h", level: 2, text: "Phạm vi" },
    ]);
  });

  it("splits an anchored quote from a comment body", () => {
    expect(splitQuote("> trong 2 ngày\nĐề xuất 3 ngày")).toEqual({
      quote: "trong 2 ngày",
      text: "Đề xuất 3 ngày",
    });
    expect(splitQuote("Không có neo")).toEqual({
      quote: null,
      text: "Không có neo",
    });
  });

  it("labels times against the calendar day", () => {
    expect(editedAtLabel(copy, "2026-10-11T09:40:00.000Z", NOW)).toBe(
      "16:40 hôm qua",
    );
    expect(editedAtLabel(copy, "2026-10-12T02:15:00.000Z", NOW)).toBe(
      "09:15 hôm nay",
    );
    expect(editedAtLabel(copy, "2026-10-08T02:15:00.000Z", NOW)).toBe(
      "09:15 08/10",
    );
  });

  it("orders comments and versions and marks the newest version current", () => {
    const comments: PageComment[] = [
      {
        id: "c2",
        pageId: "page-1",
        blockId: null,
        authorId: "user-kl",
        body: "Sau",
        mentionIds: [],
        anchor: null,
        resolved: false,
        createdAt: "2026-10-11T04:00:00.000Z",
        updatedAt: "2026-10-11T04:00:00.000Z",
      },
      {
        id: "c1",
        pageId: "page-1",
        blockId: null,
        authorId: "user-kl",
        body: "> neo\nTrước",
        mentionIds: [],
        anchor: null,
        resolved: false,
        createdAt: "2026-10-11T03:00:00.000Z",
        updatedAt: "2026-10-11T03:00:00.000Z",
      },
    ];
    const views = toCommentViews(comments, copy, NOW, () => KL);
    expect(views.map((view) => view.id)).toEqual(["c1", "c2"]);
    expect(views[0]?.quote).toBe("neo");
    expect(views[0]?.anchored).toBeUndefined();
    expect(views[0]?.when).toBe("Hôm qua");

    // Resolve and reopen: the thread starter or an editor, nobody else.
    const canResolve = (viewer?: string, editor?: boolean) =>
      toCommentViews(comments, copy, NOW, () => KL, viewer, editor)[0]
        ?.canResolve;
    expect(canResolve("user-kl", false)).toBe(true);
    expect(canResolve("user-other", true)).toBe(true);
    expect(canResolve("user-other", false)).toBeUndefined();

    // A stored anchor names the quote; the body is the comment as written.
    const [anchored] = toCommentViews(
      [
        {
          ...(comments[0] as PageComment),
          id: "c3",
          body: "> not a quote\nWhy?",
          anchor: { blockId: "b1", quote: "brave", start: 6, end: 11 },
        },
      ],
      copy,
      NOW,
      () => KL,
    );
    expect(anchored).toMatchObject({
      quote: "brave",
      anchored: true,
      body: "> not a quote\nWhy?",
    });

    const history: PageVersion[] = [
      {
        id: "a1",
        pageId: "page-1",
        versionNumber: 1,
        kind: "automatic",
        name: null,
        createdBy: "user-kl",
        createdAt: "2026-10-05T07:20:00.000Z",
      },
      {
        id: "a2",
        pageId: "page-1",
        versionNumber: 2,
        kind: "named",
        name: "Bản trình Ban giám đốc",
        createdBy: "user-kl",
        createdAt: "2026-10-11T08:05:00.000Z",
      },
      {
        id: "a3",
        pageId: "page-1",
        versionNumber: 3,
        kind: "restore",
        name: "Restored from version 1",
        createdBy: "user-kl",
        createdAt: "2026-10-11T09:05:00.000Z",
      },
    ];
    const versions = toVersionViews(history, copy, NOW, () => KL);
    expect(versions.map((version) => version.id)).toEqual(["a3", "a2", "a1"]);
    // Only a version a person named carries a name; the others read by kind.
    expect(versions.map((version) => version.name)).toEqual([
      undefined,
      "Bản trình Ban giám đốc",
      undefined,
    ]);
    expect(versions.map((version) => version.summary)).toEqual([
      copy.versions.kinds.restore,
      copy.versions.kinds.named,
      copy.versions.kinds.automatic,
    ]);
    expect(versions[0]?.stamp).toMatch(/11\/10 /);
  });

  it("places a new block after another one, between neighbours or at the end", () => {
    expect(positionAfter(BLOCKS, "h")).toBe(1500);
    expect(positionAfter(BLOCKS, "r")).toBe(6000);
    expect(positionAfter([], null)).toBe(1000);
  });

  it("exports markdown with list numbering and checklist state", () => {
    const markdown = toMarkdown(HEADER.title, BLOCKS, (id) => ({
      ...TASK_144,
      id,
    }));

    expect(markdown).toContain("# Quy trình nhập hoá đơn đầu vào");
    expect(markdown).toContain("## Phạm vi");
    expect(markdown).toContain("1. Bước một");
    expect(markdown).toContain("- [x] Chốt danh mục");
  });
});

describe("version compare", () => {
  const current = [
    block("b1", "paragraph", 1, { text: "Hoá đơn quá 0,5% duyệt." }),
    block("b2", "todo", 2, { text: "Đào tạo", checked: false }),
    block("b3", "todo", 3, { text: "Thông báo", checked: false }),
  ];
  const snapshot = [
    block("b1", "paragraph", 1, { text: "Hoá đơn quá 1% duyệt." }),
  ];
  const views = toBlockViews(current, {
    resolveTask: () => TASK_144,
    board: null,
    quotes: [],
  });

  it("marks the blocks the version did not have and the text that changed", () => {
    const marked = markVersionDiff(views, current, snapshot);
    expect(marked[1]).toMatchObject({ kind: "todo", inserted: true });
    expect(marked[2]).toMatchObject({ kind: "todo", inserted: true });
    const first = marked[0] as unknown as { segments: { kind: string }[] };
    expect(first.segments.map((segment) => segment.kind)).toEqual([
      "text",
      "del",
      "text",
      "ins",
      "text",
    ]);
  });

  it("links a converted checklist line to its task", () => {
    const linked = toBlockViews(
      [
        block("c1", "todo", 1, {
          text: "x",
          checked: false,
          linkedTaskId: "t-9",
        }),
      ],
      { resolveTask: () => TASK_144, board: null, quotes: [] },
    );
    expect(linked[0]).toMatchObject({ kind: "todo", linkedTaskId: "t-9" });
  });

  it("maps a page reference to the page it names, or to unavailable when the viewer cannot read it", () => {
    const context = {
      resolveTask: () => ({ id: "x", restricted: true as const }),
      board: null,
      quotes: [],
      pages: new Map([["p-2", { title: "Chính sách", icon: "📄" }]]),
      pageHref: (id: string) => `/docs/${id}`,
    };
    const views = toBlockViews(
      [
        block("r1", "page_reference", 1000, { linkedPageId: "p-2" }),
        block("r2", "page_reference", 2000, { linkedPageId: "p-hidden" }),
        block("r3", "page_reference", 3000, {}),
      ],
      context,
    );
    expect(views[0]).toMatchObject({
      kind: "pageReference",
      title: "Chính sách",
      href: "/docs/p-2",
      unavailable: false,
    });
    expect(views[1]).toMatchObject({ title: null, unavailable: true });
    expect(views[2]).toMatchObject({ pageId: null, unavailable: false });
  });
});
