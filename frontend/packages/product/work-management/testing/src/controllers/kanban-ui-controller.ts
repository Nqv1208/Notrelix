import type { BoardGroup, Card } from "@notrelix/work-management-core";
import { cardFixture } from "../fixtures/card.fixture";
import type { KanbanScenarioData } from "../scenarios/types";
import { cloneScenario } from "./clone";

export interface KanbanUiController {
  readonly state: KanbanScenarioData & { selectedCardId: string | null };
  createCard(groupId: string, title: string): Card;
  moveCard(cardId: string, targetGroupId: string, position: number): void;
  renameGroup(groupId: string, title: string): void;
  deleteCard(cardId: string): void;
  openDetail(cardId: string): void;
}

function findGroup(groups: BoardGroup[], groupId: string): BoardGroup {
  const group = groups.find((candidate) => candidate.id === groupId);
  if (!group) throw new Error(`Unknown group: ${groupId}`);
  return group;
}

export function createKanbanUiController(
  scenario: KanbanScenarioData,
): KanbanUiController {
  const state: KanbanUiController["state"] = {
    ...cloneScenario(scenario),
    selectedCardId: null,
  };
  let nextCard = 1;

  return {
    state,
    createCard(groupId, title) {
      const group = findGroup(state.columns, groupId);
      const card = cardFixture({
        id: `local-card-${nextCard++}`,
        boardId: state.board.id,
        workspaceId: state.workspaceId,
        listId: groupId,
        title,
        position: group.cards.length + 1,
      });
      group.cards.push(card);
      return card;
    },
    moveCard(cardId, targetGroupId, position) {
      const source = state.columns.find((group) =>
        group.cards.some((card) => card.id === cardId),
      );
      if (!source) throw new Error(`Unknown card: ${cardId}`);
      const target = findGroup(state.columns, targetGroupId);
      const card = source.cards.find((candidate) => candidate.id === cardId);
      if (!card) throw new Error(`Unknown card: ${cardId}`);
      source.cards = source.cards.filter(
        (candidate) => candidate.id !== cardId,
      );
      target.cards.push({ ...card, listId: targetGroupId, position });
      target.cards.sort((a, b) => a.position - b.position);
    },
    renameGroup(groupId, title) {
      findGroup(state.columns, groupId).title = title;
    },
    deleteCard(cardId) {
      for (const group of state.columns) {
        group.cards = group.cards.filter((card) => card.id !== cardId);
      }
      if (state.selectedCardId === cardId) state.selectedCardId = null;
    },
    openDetail(cardId) {
      if (
        !state.columns.some((group) =>
          group.cards.some((card) => card.id === cardId),
        )
      ) {
        throw new Error(`Unknown card: ${cardId}`);
      }
      state.selectedCardId = cardId;
    },
  };
}
