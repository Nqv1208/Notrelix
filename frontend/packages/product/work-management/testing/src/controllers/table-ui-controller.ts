import type { Card } from "@notrelix/work-management-core";
import { cardFixture } from "../fixtures/card.fixture";
import type { TableScenarioData } from "../scenarios/types";
import { cloneScenario } from "./clone";

export interface TableUiController {
  readonly state: TableScenarioData & {
    selectedCardIds: string[];
    openCardId: string | null;
  };
  addTask(groupId: string, title: string): Card;
  toggleRow(cardId: string, selected: boolean): void;
  openRow(cardId: string): void;
  renameGroup(groupId: string, title: string): void;
  editCell(cardId: string, fieldId: string, value: unknown): void;
}

export function createTableUiController(
  scenario: TableScenarioData,
): TableUiController {
  const state = {
    ...cloneScenario(scenario),
    selectedCardIds: [] as string[],
    openCardId: null as string | null,
  };
  let nextTask = 1;

  return {
    state,
    addTask(groupId, title) {
      const group = state.groups.find((candidate) => candidate.id === groupId);
      if (!group) throw new Error(`Unknown group: ${groupId}`);
      const card = cardFixture({
        id: `local-table-card-${nextTask++}`,
        boardId: state.board.id,
        workspaceId: state.board.workspaceId,
        listId: groupId,
        title,
        position: group.cards.length + 1,
      });
      group.cards.push(card);
      return card;
    },
    toggleRow(cardId, selected) {
      const next = new Set(state.selectedCardIds);
      if (selected) next.add(cardId);
      else next.delete(cardId);
      state.selectedCardIds = [...next].sort();
    },
    openRow(cardId) {
      state.openCardId = cardId;
    },
    renameGroup(groupId, title) {
      const group = state.groups.find((candidate) => candidate.id === groupId);
      if (!group) throw new Error(`Unknown group: ${groupId}`);
      group.title = title;
    },
    editCell(cardId, fieldId, value) {
      const card = state.groups
        .flatMap((group) => group.cards)
        .find((candidate) => candidate.id === cardId);
      if (!card) throw new Error(`Unknown card: ${cardId}`);
      card.fieldValues[fieldId] = value;
    },
  };
}
