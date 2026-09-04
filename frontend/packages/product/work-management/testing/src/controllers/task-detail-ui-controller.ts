import type { CardDetailTab } from "@notrelix/work-management-core";
import type { TaskDetailScenarioData } from "../scenarios/types";
import { cloneScenario } from "./clone";

export interface TaskDetailUiController {
  readonly state: TaskDetailScenarioData & { activeTab: CardDetailTab };
  renameTitle(title: string): void;
  editField(fieldId: string, value: unknown): void;
  selectTab(tab: CardDetailTab): void;
  addUpdate(body: string): void;
  editUpdate(updateId: string, body: string): void;
  deleteUpdate(updateId: string): void;
  setWatched(watched: boolean): void;
}

export function createTaskDetailUiController(
  scenario: TaskDetailScenarioData,
): TaskDetailUiController {
  const state = {
    ...cloneScenario(scenario),
    activeTab: "updates" as CardDetailTab,
  };

  return {
    state,
    renameTitle(title) {
      if (!state.card) throw new Error("No card loaded");
      state.card.title = title;
    },
    editField(fieldId, value) {
      if (!state.card) throw new Error("No card loaded");
      state.card.fieldValues[fieldId] = value;
    },
    selectTab(tab) {
      state.activeTab = tab;
    },
    addUpdate(body) {
      if (!state.card) throw new Error("No card loaded");
      state.card.updates.push({
        id: `local-update-${state.card.updates.length + 1}`,
        cardId: state.card.id,
        author: state.card.members[0]!,
        body,
        mentionUserIds: [],
        attachmentIds: [],
        createdAt: state.card.createdAt,
      });
    },
    editUpdate(updateId, body) {
      if (!state.card) throw new Error("No card loaded");
      const update = state.card.updates.find((item) => item.id === updateId);
      if (!update) return;
      update.body = body;
      update.updatedAt = state.card.updatedAt ?? state.card.createdAt;
    },
    deleteUpdate(updateId) {
      if (!state.card) throw new Error("No card loaded");
      state.card.updates = state.card.updates.filter(
        (item) => item.id !== updateId,
      );
    },
    setWatched(watched) {
      if (!state.card) throw new Error("No card loaded");
      state.card.isWatched = watched;
    },
  };
}
