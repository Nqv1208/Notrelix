import { describe, expect, it } from "vitest";
import type { FieldDefinition } from "../types/field-types";
import { planLaneDrop, type FilterField } from "./view-state";

const person: FilterField = {
  id: "who",
  name: "Assignee",
  field: {
    id: "who",
    boardId: "b1",
    name: "Assignee",
    fieldType: "person",
    options: [],
    position: 1,
    isHidden: false,
    isSystemField: false,
  } as FieldDefinition,
  builtin: false,
  read: (card) => card.fieldValues?.who,
};

/** Q13: a card of several people shows in each person's lane; a drop swaps only the lane it left for the target. */
describe("planLaneDrop with a person lane field (Q13)", () => {
  it("swaps only the source person for the target, the other assignees stay", () => {
    const plan = planLaneDrop({
      laneField: person,
      fromColumn: "todo",
      toColumn: "todo",
      fromLane: "ada",
      toLane: "cai",
      people: ["ada", "bao"],
    });
    expect(plan).toEqual({
      ok: true,
      column: undefined,
      lane: ["bao", "cai"],
      laneChanged: true,
      columnChanged: false,
    });
  });

  it("writes the column and the lane together when both change", () => {
    const plan = planLaneDrop({
      laneField: person,
      fromColumn: "todo",
      toColumn: "doing",
      fromLane: "bao",
      toLane: "cai",
      people: ["ada", "bao"],
    });
    expect(plan).toMatchObject({
      ok: true,
      column: "doing",
      lane: ["ada", "cai"],
      laneChanged: true,
      columnChanged: true,
    });
  });

  it("drops the source person only when the target person already holds the card", () => {
    const plan = planLaneDrop({
      laneField: person,
      fromColumn: "todo",
      toColumn: "todo",
      fromLane: "ada",
      toLane: "bao",
      people: ["ada", "bao"],
    });
    expect(plan).toMatchObject({ ok: true, lane: ["bao"] });
  });

  it("unassigns only the source person on a drop in the Unassigned lane", () => {
    const plan = planLaneDrop({
      laneField: person,
      fromColumn: "todo",
      toColumn: "todo",
      fromLane: "ada",
      toLane: null,
      people: ["ada", "bao"],
    });
    expect(plan).toMatchObject({ ok: true, lane: ["bao"] });
  });

  it("assigns the target person to an unassigned card", () => {
    const plan = planLaneDrop({
      laneField: person,
      fromColumn: "todo",
      toColumn: "todo",
      fromLane: null,
      toLane: "ada",
      people: [],
    });
    expect(plan).toMatchObject({ ok: true, lane: ["ada"] });
  });

  it("writes no lane when only the column changes, so every assignee stays", () => {
    const plan = planLaneDrop({
      laneField: person,
      fromColumn: "todo",
      toColumn: "doing",
      fromLane: "ada",
      toLane: "ada",
      people: ["ada", "bao"],
    });
    expect(plan).toMatchObject({
      ok: true,
      column: "doing",
      lane: undefined,
      laneChanged: false,
    });
  });
});
