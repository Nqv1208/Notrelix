import { describe, expect, it } from "vitest";
import {
  previewMoveToBoard,
  type MovePreviewSource,
  type MovePreviewTarget,
} from "./move-preview";

const source: MovePreviewSource = {
  groupTitle: "Đang chạy",
  statusName: "Đang làm",
  fields: [
    { name: "Ngân sách", type: "number" },
    { name: "Ghi chú", type: "text" },
  ],
  labelNames: ["Dữ liệu", "Gấp"],
  assignees: [
    { id: "u1", name: "Lan Anh" },
    { id: "u2", name: "Hoàng Nam" },
  ],
};

const target: MovePreviewTarget = {
  groupTitles: ["Cần làm", "Đang chạy"],
  defaultGroupTitle: "Cần làm",
  statusNames: ["Cần làm", "Đang làm"],
  fields: [{ name: "ghi chú", type: "text" }],
  labelNames: ["gấp"],
  visibleTo: new Set(["u1"]),
};

describe("previewMoveToBoard (mirrors MoveBoardItemToBoard; the server decides)", () => {
  it("matches the group and the status by name, ignoring case", () => {
    const preview = previewMoveToBoard(source, target);
    expect(preview.group).toEqual({ title: "Đang chạy", matched: true });
    expect(preview.status).toEqual({ name: "Đang làm", matched: true });
  });

  it("names what is lost: a field with no same-name same-type twin, a label by name, an assignee who cannot see the board", () => {
    const preview = previewMoveToBoard(source, target);
    expect(preview.lostFields).toEqual(["Ngân sách"]);
    expect(preview.lostLabels).toEqual(["Dữ liệu"]);
    expect(preview.lostAssignees).toEqual(["Hoàng Nam"]);
    expect(preview.lostCount).toBe(3);
  });

  it("does not carry a value to a field of the same name but another type", () => {
    const preview = previewMoveToBoard(source, {
      ...target,
      fields: [{ name: "Ghi chú", type: "number" }],
    });
    expect(preview.lostFields).toContain("Ghi chú");
  });

  it("falls back to the default group, else the first, and to the first status", () => {
    const noMatch = { ...source, groupTitle: "Khác", statusName: "Khác" };
    expect(previewMoveToBoard(noMatch, target).group).toEqual({
      title: "Cần làm",
      matched: false,
    });
    expect(previewMoveToBoard(noMatch, target).status).toEqual({
      name: "Cần làm",
      matched: false,
    });
    expect(
      previewMoveToBoard(noMatch, { ...target, defaultGroupTitle: null }).group
        .title,
    ).toBe("Cần làm");
  });

  it("drops nobody when everyone in the workspace sees the board", () => {
    expect(
      previewMoveToBoard(source, { ...target, visibleTo: null }).lostAssignees,
    ).toEqual([]);
  });

  it("loses nothing when the boards match", () => {
    const preview = previewMoveToBoard(
      { ...source, labelNames: [], assignees: [], fields: [] },
      target,
    );
    expect(preview.lostCount).toBe(0);
  });
});
