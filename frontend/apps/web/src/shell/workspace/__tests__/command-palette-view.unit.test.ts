import { describe, expect, it } from "vitest";
import { NAVIGATION_RESOURCE_KIND } from "@notrelix/features-workspace/core";
import { workspaceCommandCopy } from "@notrelix/features-workspace/web";
import {
  paletteDue,
  toPaletteBoards,
  toPaletteDocs,
  toPaletteRecents,
  toPaletteWork,
  toSearchBoards,
  toSearchDocs,
  toSearchWork,
} from "../command-palette-view";

const copy = workspaceCommandCopy("vi");
// 15 January 2025, 10:40 in the member's calendar.
const NOW = new Date(2025, 0, 15, 10, 40).getTime();
const at = (day: number, hour = 0, minute = 0) =>
  new Date(2025, 0, day, hour, minute).toISOString();

describe("paletteDue", () => {
  it("has no label without a due time", () => {
    expect(paletteDue(null, NOW, copy)).toBeNull();
    expect(paletteDue("not a date", NOW, copy)).toBeNull();
  });

  it("calls an earlier day overdue", () => {
    expect(paletteDue(at(14, 17), NOW, copy)).toEqual({
      tone: "overdue",
      label: "Quá hạn",
    });
  });

  it("names today with the time, and overdue once the time has passed", () => {
    expect(paletteDue(at(15, 17), NOW, copy)).toEqual({
      tone: "today",
      label: "Hôm nay 17:00",
    });
    expect(paletteDue(at(15, 9), NOW, copy)).toEqual({
      tone: "overdue",
      label: "Quá hạn",
    });
    // A date-only due day has no time to show.
    expect(paletteDue(at(15), NOW, copy)).toMatchObject({ tone: "overdue" });
  });

  it("shows later days as dd/MM", () => {
    expect(paletteDue(at(20, 8), NOW, copy)).toEqual({
      tone: "later",
      label: "20/01",
    });
  });
});

const item = {
  id: "i1",
  boardId: "b1",
  groupId: "g1",
  name: "Kiểm thử phân hệ công nợ",
  dueAt: at(16, 9),
  startedAt: null,
  updatedAt: null,
  version: 1,
};

describe("toPaletteWork", () => {
  it("opens the item on its Board and states only what the read states", () => {
    const [entry] = toPaletteWork(
      [item],
      new Map([["b1", "Triển khai ERP Q4"]]),
      new Map([["b1", { name: "Công nghệ", markTone: "blue" as const }]]),
      NOW,
      copy,
    );
    expect(entry).toMatchObject({
      id: "i1",
      title: "Kiểm thử phân hệ công nợ",
      boardName: "Triển khai ERP Q4",
      mine: true,
      department: { name: "Công nghệ" },
      due: { tone: "later", label: "16/01" },
      destination: { section: "board", boardId: "b1", item: "i1" },
    });
    // No key, status or assignee exists in the read.
    expect(entry!.key).toBeUndefined();
    expect(entry!.status).toBeUndefined();
    expect(entry!.assignee).toBeUndefined();
  });

  it("leaves the Board and department out when the member cannot see them", () => {
    const [entry] = toPaletteWork([item], new Map(), new Map(), NOW, copy);
    expect(entry!.boardName).toBeNull();
    expect(entry!.department).toBeNull();
  });
});

describe("toPaletteDocs, toPaletteBoards and toPaletteRecents", () => {
  const relative = (value: string) => `rel(${new Date(value).getDate()})`;
  const dept = { name: "Kế toán", markTone: "gold" as const };

  it("maps a Doc with its department, parent and change time", () => {
    const [doc] = toPaletteDocs(
      [
        {
          id: "d1",
          name: "Quy trình",
          parentName: "Kế toán",
          parentId: "d0",
          updatedAt: at(12, 8),
          spaceId: "s1",
        },
      ],
      (spaceId) => (spaceId === "s1" ? dept : null),
      relative,
      NOW,
    );
    expect(doc).toMatchObject({
      id: "d1",
      title: "Quy trình",
      department: dept,
      location: "Kế toán",
      editedLabel: "rel(12)",
    });
    expect(doc!.editedBy).toBeUndefined();
  });

  it("maps Boards with the department of their placement", () => {
    const boards = toPaletteBoards(
      [
        {
          id: "b1",
          name: "A",
          parentName: null,
          parentId: null,
          updatedAt: null,
          spaceId: null,
        },
        {
          id: "b2",
          name: "B",
          parentName: null,
          parentId: null,
          updatedAt: null,
          spaceId: null,
        },
      ],
      new Map([["b1", dept]]),
    );
    expect(boards).toEqual([
      { id: "b1", name: "A", department: dept },
      { id: "b2", name: "B", department: null },
    ]);
  });

  it("keeps the newest recents, three at most, with their kind and department", () => {
    const kinds = [
      NAVIGATION_RESOURCE_KIND.board,
      NAVIGATION_RESOURCE_KIND.doc,
      NAVIGATION_RESOURCE_KIND.board,
      NAVIGATION_RESOURCE_KIND.doc,
    ];
    const entries = kinds.map((kind, index) => ({
      kind,
      resourceId: `r${index}`,
      name: `Name ${index}`,
      at: at(14 - index),
      pinned: false,
    }));
    const recents = toPaletteRecents(
      entries,
      new Map([["r0", dept]]),
      relative,
      NOW,
    );
    expect(recents.map((entry) => [entry.kind, entry.id])).toEqual([
      ["board", "r0"],
      ["doc", "r1"],
      ["board", "r2"],
    ]);
    expect(recents[0]!.department).toEqual(dept);
    // The design marks a recent Board with its department, not a Doc.
    expect(recents[1]!.department).toBeNull();
    expect(recents[2]!.department).toBeNull();
    expect(recents[0]!.whenLabel).toBe("rel(14)");
  });
});

describe("search result mappers", () => {
  const dept = { name: "Kế toán", markTone: "amber" } as never;
  const departments = new Map([["b1", dept]]);

  it("tells a sub-item match by its note and hides the board and assignee", () => {
    const [entry] = toSearchWork(
      [
        {
          id: "w1",
          key: "ERP-142",
          title: "Chuyển dữ liệu",
          boardId: "b1",
          boardName: "Triển khai ERP Q4",
          assigneeName: "Lý Thu Trang",
          mine: false,
          statusName: "Đang làm",
          statusCategory: "in_progress",
          dueAt: at(14, 17),
          match: { kind: "child", snippet: "Đối chiếu công nợ đầu kỳ" },
        },
      ],
      departments,
      NOW,
      copy,
    );
    expect(entry!.boardName).toBeNull();
    expect(entry!.assignee).toBeNull();
    expect(entry!.matchNote).toContain("Đối chiếu công nợ đầu kỳ");
    expect(entry!.status).toEqual({ bucket: "doing", label: "Đang làm" });
    expect(entry!.due).toEqual({ tone: "overdue", label: "Quá hạn" });
  });

  it("keeps the board and assignee for a title match", () => {
    const [entry] = toSearchWork(
      [
        {
          id: "w2",
          key: "KT-91",
          title: "Đối chiếu công nợ",
          boardId: "b1",
          boardName: "Công nợ nhà cung cấp",
          assigneeName: "Lý Thu Trang",
          mine: false,
          statusName: null,
          statusCategory: null,
          dueAt: null,
          match: { kind: "title" },
        },
      ],
      departments,
      NOW,
      copy,
    );
    expect(entry!.boardName).toBe("Công nợ nhà cung cấp");
    expect(entry!.assignee).toBe("Lý Thu Trang");
    expect(entry!.status).toBeNull();
  });

  it("hides the edit of a Doc whose text matched and counts Board matches", () => {
    const [doc] = toSearchDocs(
      [
        {
          id: "d1",
          title: "Quy trình nhập hoá đơn đầu vào",
          spaceId: null,
          location: null,
          editedAt: at(14),
          editedByName: "Đỗ Khánh Linh",
          match: { kind: "content", snippet: "… đối chiếu với …" },
        },
      ],
      () => null,
      (value) => value,
      NOW,
      copy,
    );
    expect(doc!.editedLabel).toBeNull();
    expect(doc!.editedBy).toBeNull();
    expect(doc!.matchNote).toContain("đối chiếu với");

    const [board] = toSearchBoards(
      [{ id: "b1", name: "Đóng sổ tháng 10", spaceId: null, matchedItems: 4 }],
      departments,
      "đối chiếu",
      copy,
    );
    expect(board!.matchNote).toContain("4");
    expect(board!.department).toEqual(dept);
  });
});
