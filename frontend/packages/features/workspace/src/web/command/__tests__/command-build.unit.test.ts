import { describe, expect, it } from "vitest";
import {
  commandBoardsFixture,
  commandCopyFixture,
  commandDocsFixture,
  commandPeopleFixture,
  commandRecentsFixture,
  commandWorkFixture,
} from "../../../verification/workspace-command-fixtures";
import {
  buildPalette,
  firstEnabledId,
  flattenRows,
  type PaletteCapabilities,
} from "../command-build";

const copy = commandCopyFixture("vi");
const sources = {
  work: commandWorkFixture,
  docs: commandDocsFixture,
  boards: commandBoardsFixture,
  people: commandPeopleFixture,
  recents: commandRecentsFixture,
};
const capabilities: PaletteCapabilities = {
  appearance: "light",
  canCreateWorkspace: false,
  canCreateDoc: true,
  canSwitchWorkspace: true,
  otherWorkspaces: ["Minh An Đà Nẵng", "Tân Lộc Logistics"],
};

function build(query: string, filter: "all" | "work" | "doc" = "all") {
  return buildPalette({
    copy,
    query,
    filter,
    sources,
    capabilities,
    workspaceName: "Minh An Group",
  });
}

describe("buildPalette", () => {
  it("lists recents then quick commands when nothing is typed", () => {
    const view = build("");
    expect(view.state).toBe("recent");
    expect(view.groups.map((group) => group.label)).toEqual([
      "Mở gần đây",
      "Lệnh nhanh",
    ]);
    const quick = view.groups[1]!.rows;
    expect(quick.map((row) => row.title)).toEqual([
      "Tạo công việc",
      "Tạo tài liệu",
      "Mở Hộp thư",
      "Đổi giao diện sang Đêm",
    ]);
    // Only a producer-less command is inert, and it says why.
    expect(quick[0]!.unavailable).toBe(copy.commands.unavailableCreateItem);
    expect(quick[1]!.selection).toEqual({
      type: "action",
      action: "create-doc",
    });
    // Only shortcuts that exist are shown.
    expect(quick[2]!.keys).toEqual(["G", "I"]);
    expect(quick[0]!.keys).toEqual([]);
  });

  it("searches items, Docs and Boards ignoring diacritics, with counts", () => {
    const view = build("doi chieu");
    expect(view.state).toBe("results");
    if (view.state !== "results") return;
    expect(view.groups.map((group) => group.id)).toEqual([
      "work",
      "doc",
      "board",
      "command",
    ]);
    expect(view.counts).toMatchObject({
      all: 7,
      work: 3,
      doc: 2,
      board: 1,
      command: 1,
    });
    // The item row opens its Board with the item.
    expect(view.groups[0]!.rows[0]!.selection).toEqual({
      type: "navigate",
      destination: {
        section: "board",
        boardId: "board-dong-so",
        item: "item-kt-88",
      },
    });
    // A command row for the typed text exists but cannot run.
    const create = view.groups[3]!.rows[0]!;
    expect(create.unavailable).not.toBeNull();
    expect(create.selection).toBeNull();
  });

  it("narrows to one kind when a filter tab is chosen", () => {
    const view = build("doi chieu", "doc");
    expect(view.state).toBe("results");
    if (view.state !== "results") return;
    expect(view.groups.map((group) => group.id)).toEqual(["doc"]);
    expect(view.filtered).toBe(true);
    // The counts still describe the whole result.
    expect(view.counts.all).toBe(7);
  });

  it("reads > as commands only and filters them by label", () => {
    const view = build(">chuyen");
    expect(view.state).toBe("commands");
    const titles = flattenRows(view.groups).map((row) => row.title);
    expect(titles).toContain("Chuyển trạng thái sang…");
    expect(titles).toContain("Chuyển tới Trang chủ");
    expect(titles).not.toContain("Đổi giao diện sang Đêm");
    expect(view.groups[0]!.label).toContain("chuyen");
  });

  it("reads @ as people and # as Boards", () => {
    const people = build("@ha");
    expect(flattenRows(people.groups).map((row) => row.title)).toEqual([
      "Nguyễn Thu Hà",
    ]);
    const boards = build("#dong");
    expect(flattenRows(boards.groups).map((row) => row.title)).toEqual([
      "Đóng sổ tháng 10",
    ]);
  });

  it("offers inert suggestions when nothing matches", () => {
    const view = build("hợp đồng bảo trì thang máy");
    expect(view.state).toBe("none");
    const rows = flattenRows(view.groups);
    expect(rows.map((row) => row.title)).toEqual([
      "Tạo việc “hợp đồng bảo trì thang máy”",
      "Tìm ở mọi workspace",
    ]);
    expect(rows.every((row) => row.unavailable && !row.selection)).toBe(true);
  });

  it("starts the selection on the first row that can run", () => {
    const view = build(">chuyen");
    const first = firstEnabledId(view.groups);
    expect(first).toBe("command:home");
    expect(firstEnabledId([])).toBeNull();
  });

  it("drops commands the host does not offer", () => {
    const view = buildPalette({
      copy,
      query: ">",
      filter: "all",
      sources,
      capabilities: { ...capabilities, commandIds: ["home"] },
      workspaceName: "Minh An Group",
    });
    expect(flattenRows(view.groups).map((row) => row.id)).toEqual([
      "command:home",
    ]);
  });

  it("caps each kind while all kinds show", () => {
    const many = Array.from({ length: 10 }, (_, index) => ({
      ...commandWorkFixture[0]!,
      id: `w-${index}`,
      title: `Báo cáo ${index}`,
    }));
    const view = buildPalette({
      copy,
      query: "bao cao",
      filter: "all",
      sources: { ...sources, work: many },
      capabilities,
      workspaceName: "Minh An Group",
    });
    if (view.state !== "results") throw new Error("expected results");
    expect(view.groups[0]!.rows).toHaveLength(6);
    expect(view.groups[0]!.count).toBe(10);
  });
});
