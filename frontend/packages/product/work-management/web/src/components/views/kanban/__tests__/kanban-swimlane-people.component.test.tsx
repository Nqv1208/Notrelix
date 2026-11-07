import { describe, expect, it, vi } from "vitest";
import { fireEvent, renderPureUi } from "@notrelix/testing";
import { groupCardsByField } from "@notrelix/work-management-core";
import { erpSwimlaneScenario } from "../../../../verification/kanban-swimlane-fixtures";
import { KanbanSwimlaneBoard } from "../kanban-swimlane-board";

/**
 * Q13 (UC-WM-J03, UC-WM-J04): swimlanes by a person field show a card of two people in both people's lanes, and a
 * move from one of those lanes changes only that person; the other assignee stays.
 */
function twoPeopleScenario() {
  const base = erpSwimlaneScenario();
  const fieldId = base.laneField.id;
  const people = base.swimlanes
    .map((lane) => lane.optionId)
    .filter((id): id is string => id !== null);
  if (people.length < 3) throw new Error("the fixture needs three people");
  const [ada, bao] = people as [string, string, string];
  const target = base.columns[0]!.cards[0]!;
  const columns = base.columns.map((column) => ({
    ...column,
    cards: column.cards.map((card) =>
      card.id === target.id
        ? {
            ...card,
            fieldValues: { ...card.fieldValues, [fieldId]: [ada, bao] },
          }
        : card,
    ),
  }));
  const swimlanes = groupCardsByField(
    columns.flatMap((column) => column.cards),
    base.laneField,
    undefined,
    { person: (id) => id, bucket: (key) => key },
  );
  return {
    scenario: { ...base, columns, swimlanes },
    card: target,
    ada,
    bao,
    order: swimlanes.map((lane) => lane.optionId),
  };
}

describe("KanbanSwimlaneBoard with a card of two people (Q13)", () => {
  it("shows the card in each person's lane", () => {
    const { scenario, card } = twoPeopleScenario();
    renderPureUi(
      <KanbanSwimlaneBoard
        {...scenario}
        onOpenDetails={() => undefined}
        onDrop={() => undefined}
        onRefused={() => undefined}
      />,
    );
    const copies = document.querySelectorAll(
      `[data-card-id="${card.id}"][aria-label]`,
    );
    expect(copies.length).toBe(2);
  });

  it("moves it out of one person's lane and keeps the other assignee", () => {
    const { scenario, card, ada, bao, order } = twoPeopleScenario();
    const onDrop = vi.fn();
    renderPureUi(
      <KanbanSwimlaneBoard
        {...scenario}
        onOpenDetails={() => undefined}
        onDrop={onDrop}
        onRefused={() => undefined}
      />,
    );
    // The copy in Ada's lane, moved one lane down with the keyboard.
    const adaIndex = order.indexOf(ada);
    const next = order[adaIndex + 1];
    const inAda = document.querySelector<HTMLElement>(
      `[data-swimlane="${ada}"] [data-card-id="${card.id}"][aria-label]`,
    );
    expect(inAda).not.toBeNull();
    fireEvent.keyDown(inAda!, { key: "ArrowDown", altKey: true });
    expect(onDrop).toHaveBeenCalledTimes(1);
    const { plan, toLane } = onDrop.mock.calls[0]![0] as {
      plan: { lane: unknown; laneChanged: boolean; columnChanged: boolean };
      toLane: string | null;
    };
    expect(toLane).toBe(next ?? null);
    expect(plan.laneChanged).toBe(true);
    expect(plan.columnChanged).toBe(false);
    // Ada leaves, Bảo stays, the lane's person joins (once).
    const expected = next === bao || next === null ? [bao] : [bao, next];
    expect(plan.lane).toEqual(expected);
  });
});
