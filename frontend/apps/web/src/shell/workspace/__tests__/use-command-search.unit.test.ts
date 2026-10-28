import { describe, expect, it } from "vitest";
import { toCommandSearch } from "../use-command-search";

describe("toCommandSearch", () => {
  it("answers empty lists for no body", () => {
    expect(toCommandSearch(null)).toEqual({ work: [], docs: [], boards: [] });
    expect(toCommandSearch({})).toEqual({ work: [], docs: [], boards: [] });
  });

  it("maps the producer rows and fills what the row leaves out", () => {
    const dto = toCommandSearch({
      work: [
        {
          id: "i1",
          boardId: "b1",
          title: "Hóa đơn",
          boardName: "Kế toán",
          key: "ERP-1",
          mine: true,
          statusName: "Doing",
          statusCategory: "in_progress",
          match: { kind: "title", snippet: null },
        },
        {
          id: "i2",
          boardId: "b1",
          title: "Prepare launch",
          match: { kind: "child", snippet: "Invoice template" },
        },
        // No id: dropped.
        { title: "Broken", boardId: "b1" },
      ],
      docs: [
        {
          id: "d1",
          title: "Quy trình",
          location: "Handbook / Finance",
          editedByName: "An",
          match: { kind: "title" },
        },
      ],
      boards: [{ id: "b1", name: "Kế toán", matchedItems: 2 }, { name: "x" }],
    });

    expect(dto.work).toHaveLength(2);
    expect(dto.work[0]).toMatchObject({
      id: "i1",
      key: "ERP-1",
      mine: true,
      statusCategory: "in_progress",
      assigneeName: null,
      dueAt: null,
      match: { kind: "title" },
    });
    expect(dto.work[1]).toMatchObject({
      mine: false,
      boardName: "",
      match: { kind: "child", snippet: "Invoice template" },
    });
    expect(dto.docs).toEqual([
      {
        id: "d1",
        title: "Quy trình",
        spaceId: null,
        location: "Handbook / Finance",
        editedAt: null,
        editedByName: "An",
        match: { kind: "title" },
      },
    ]);
    expect(dto.boards).toEqual([
      { id: "b1", name: "Kế toán", spaceId: null, matchedItems: 2 },
    ]);
  });

  it("treats a child match without text as a title match", () => {
    const dto = toCommandSearch({
      work: [{ id: "i", boardId: "b", match: { kind: "child" } }],
    });
    expect(dto.work[0]?.match).toEqual({ kind: "title" });
  });
});
