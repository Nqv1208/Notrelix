import { fixedIso } from "../support/fixed-clock";
import { createKanbanScenario } from "./kanban.scenario";
import type { CalendarScenarioData } from "./types";

export function createCalendarScenario(
  seed = "calendar",
  cardCount = 10,
): CalendarScenarioData {
  const scenario = createKanbanScenario({
    seed,
    columnCount: 2,
    cardsPerColumn: Math.ceil(cardCount / 2),
  });
  return {
    weekStartIso: fixedIso(0),
    groups: scenario.columns.map((group, groupIndex) => ({
      ...group,
      cards: group.cards.slice(0, cardCount).map((card, cardIndex) => ({
        ...card,
        dueDate: fixedIso(groupIndex + cardIndex),
      })),
    })),
  };
}

export const calendarDefaultScenario = () => createCalendarScenario();
export const calendarEmptyScenario = () => ({
  weekStartIso: fixedIso(),
  groups: [],
});
export const calendarEdgeScenario = () =>
  createCalendarScenario("calendar-edge", 4);
export const calendarDenseScenario = () =>
  createCalendarScenario("calendar-dense", 60);
