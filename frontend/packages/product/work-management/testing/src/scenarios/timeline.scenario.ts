import { fixedIso } from "../support/fixed-clock";
import { createKanbanScenario } from "./kanban.scenario";
import type { TimelineScenarioData } from "./types";

export function createTimelineScenario(
  seed = "timeline",
  cardCount = 10,
): TimelineScenarioData {
  const scenario = createKanbanScenario({
    seed,
    columnCount: 2,
    cardsPerColumn: Math.ceil(cardCount / 2),
  });
  return {
    rangeStartIso: fixedIso(0),
    rangeEndIso: fixedIso(42),
    groups: scenario.columns.map((group, groupIndex) => ({
      ...group,
      cards: group.cards.slice(0, cardCount).map((card, cardIndex) => ({
        ...card,
        startDate: fixedIso(groupIndex + cardIndex),
        dueDate: fixedIso(groupIndex + cardIndex + 5),
      })),
    })),
  };
}

export const timelineDefaultScenario = () => createTimelineScenario();
export const timelineEmptyScenario = () => ({
  rangeStartIso: fixedIso(),
  rangeEndIso: fixedIso(42),
  groups: [],
});
export const timelineEdgeScenario = () =>
  createTimelineScenario("timeline-edge", 4);
export const timelineDenseScenario = () =>
  createTimelineScenario("timeline-dense", 60);
