import { describe, expect, it } from "vitest";
import {
  generatedComputedValues,
  generatedFieldValues,
  mapFullBoardDto,
  type GeneratedFullBoardDto,
} from "../api/board-api-mappers";

describe("custom and computed values of a board read", () => {
  it("reads stored values by field id and ignores anything that is not a record", () => {
    expect(generatedFieldValues({ fieldValues: { a: 5, b: ["x"] } })).toEqual({
      a: 5,
      b: ["x"],
    });
    expect(generatedFieldValues({ fieldValues: [1] })).toEqual({});
    expect(generatedFieldValues({})).toEqual({});
  });

  it("maps the three computed states and drops an unknown shape", () => {
    expect(
      generatedComputedValues({
        computedValues: {
          f1: { state: "ok", value: 10 },
          f2: { state: "empty", value: null },
          f3: { state: "error", errorCode: "divide-by-zero" },
          f4: { state: "weird" },
          f5: null,
        },
      }),
    ).toEqual({
      f1: { state: "ok", value: 10 },
      f2: { state: "empty" },
      f3: { state: "error", errorCode: "divide-by-zero" },
    });
    expect(generatedComputedValues({})).toEqual({});
  });

  it("puts both on the card of the board read, and maps the new field types", () => {
    const dto = {
      id: "b1",
      workspaceId: "w1",
      title: "Board",
      columns: [
        {
          id: "n",
          boardId: "b1",
          name: "Effort",
          fieldType: "Number",
          position: "a1",
        },
        {
          id: "l",
          boardId: "b1",
          name: "Notes",
          fieldType: "LongText",
          position: "a2",
        },
        {
          id: "k",
          boardId: "b1",
          name: "Site",
          fieldType: "Link",
          position: "a3",
        },
        {
          id: "r",
          boardId: "b1",
          name: "Sum",
          fieldType: "Rollup",
          position: "a4",
        },
      ],
      lists: [
        {
          id: "g1",
          title: "G",
          position: "a0",
          boardItems: [
            {
              id: "i1",
              name: "Item",
              memberCount: 0,
              members: [],
              labels: [],
              checklistProgress: 0,
              checklistTotal: 0,
              commentCount: 0,
              attachmentCount: 0,
              position: "a0",
              createdAt: "2026-10-12T00:00:00Z",
              version: 1,
              fieldValues: { n: 5 },
              computedValues: { r: { state: "ok", value: 7 } },
            },
          ],
        },
      ],
      members: [],
      version: 1,
    } as unknown as GeneratedFullBoardDto;

    const { board, groups } = mapFullBoardDto(dto, { workspaceId: "w1" });
    const card = groups[0]!.cards[0]!;

    expect(card.fieldValues.n).toBe(5);
    expect(card.computedValues).toEqual({ r: { state: "ok", value: 7 } });
    expect(board.fieldDefinitions.map((field) => field.fieldType)).toEqual([
      "number",
      "long_text",
      "link",
      "rollup",
    ]);
  });
});
