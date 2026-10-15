import { describe, expect, it } from "vitest";
import { defaultConfig } from "../config/mock-config";
import { mockIds } from "../state/mock-ids";
import { spaceOverview } from "../state/mock-space-overview";
import { MockStore } from "../state/mock-store";

function overview(
  spaceId: string,
  asMember = false,
  canRead: (boardId: string) => boolean = () => true,
) {
  const store = new MockStore({ ...defaultConfig });
  const result = spaceOverview(store, mockIds.workspaces.primary, spaceId, {
    canReadBoard: canRead,
    asMember,
  });
  return { store, result: result! };
}

describe("department summary in the mock world (CTR-GAP-WS-SPACE-OVERVIEW)", () => {
  it("counts the 24 people of Công nghệ once, teams expanded", () => {
    const { result } = overview(mockIds.spaces.engineering);
    expect(result.peopleTotal).toBe(24);
    expect(result.access.admins.map((admin) => admin.name)).toEqual([
      "Lê Hoàng Phúc",
      "Nguyễn Thu Hà",
    ]);
    expect(result.access.members.people).toBe(19);
    expect(result.access.members.teams).toEqual([
      { name: "Kỹ sư ERP", count: 4 },
    ]);
    expect(result.access.viewers.people).toBe(3);
    expect(result.access.viewers.teams).toEqual([
      { name: "Ban giám đốc", count: 3 },
    ]);
    expect(result.people.map((person) => person.name)).toEqual([
      "Lê Hoàng Phúc",
      "Nguyễn Thu Hà",
      "Trần Minh Quân",
      "Phan Ngọc Anh",
    ]);
  });

  it("lists the guest of the ERP Board who is not in the department", () => {
    const { result } = overview(mockIds.spaces.engineering);
    expect(result.access.guests).toEqual([
      {
        id: "mock-user-erp-tuan",
        name: "Đặng Minh Tuấn",
        boards: [{ id: mockIds.boards.erp, name: "Triển khai ERP Q4" }],
      },
    ]);
  });

  it("derives the ERP Board's figures from its Items", () => {
    const { result } = overview(mockIds.spaces.engineering);
    const erp = result.boards.find((board) => board.id === mockIds.boards.erp)!;
    expect(erp.kind).toBe("kanban");
    expect(erp.progress).toEqual({ kind: "bar", done: 5, total: 16 });
    expect(erp.meta).toBe("Kanban · Go-live 01/11 · có 1 khách");
    expect(result.boards.map((board) => board.name)).toEqual([
      "Triển khai ERP Q4",
      "Hạ tầng & bảo mật",
      "Hỗ trợ nội bộ",
      "Ngân sách CNTT 2027",
    ]);
    expect(result.boardTotal).toBe(6);
    const budget = result.boards.find((board) => board.isPrivate)!;
    expect(budget.addedCount).toBe(4);
  });

  it("lists the department's Docs with editor and total", () => {
    const { result } = overview(mockIds.spaces.engineering);
    expect(result.docTotal).toBe(3);
    expect(result.docs.map((doc) => doc.editorName)).toEqual([
      "Trần Minh Quân",
      "Vũ Thanh",
    ]);
  });

  it("previews a member: no private Board or sub-department, not counted", () => {
    const own = overview(mockIds.spaces.engineering).result;
    const asMember = overview(mockIds.spaces.engineering, true).result;
    expect(own.children).toHaveLength(2);
    expect(asMember.children).toHaveLength(1);
    expect(asMember.boards.some((board) => board.isPrivate)).toBe(false);
    expect(asMember.boardTotal).toBe(own.boardTotal - 1);
  });

  it("leaves out the Boards the viewer cannot read", () => {
    const { result } = overview(
      mockIds.spaces.engineering,
      false,
      (id) => id !== mockIds.boards.erp,
    );
    expect(result.boards.some((board) => board.id === mockIds.boards.erp)).toBe(
      false,
    );
  });

  it("offers templates only to a department with nothing in it", () => {
    expect(overview(mockIds.spaces.hr).result.templates).toHaveLength(3);
    expect(overview(mockIds.spaces.engineering).result.templates).toEqual([]);
  });
});
