import { describe, expect, it } from "vitest";
import type { Block, PageActivity, PageComment } from "@notrelix/docs-core";

import { docPageCopy } from "../doc-page.copy";
import {
  editedAtLabel,
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
        resolved: false,
        createdAt: "2026-10-11T03:00:00.000Z",
        updatedAt: "2026-10-11T03:00:00.000Z",
      },
    ];
    const views = toCommentViews(comments, copy, NOW, () => KL);
    expect(views.map((view) => view.id)).toEqual(["c1", "c2"]);
    expect(views[0]?.quote).toBe("neo");
    expect(views[0]?.when).toBe("Hôm qua");

    const history: PageActivity[] = [
      {
        id: "a1",
        pageId: "page-1",
        actorId: "user-kl",
        action: "created",
        targetLabel: "",
        createdAt: "2026-10-05T07:20:00.000Z",
      },
      {
        id: "a2",
        pageId: "page-1",
        actorId: "user-kl",
        action: "edited",
        targetLabel: "Sửa bước 3",
        createdAt: "2026-10-11T08:05:00.000Z",
      },
    ];
    const versions = toVersionViews(history, copy, NOW, () => KL);
    expect(versions.map((version) => version.id)).toEqual(["a2", "a1"]);
    expect(versions[0]?.current).toBe(true);
    expect(versions[0]?.summary).toBe("Sửa bước 3");
    expect(versions[1]?.summary).toBe(copy.versions.actions.created);
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
