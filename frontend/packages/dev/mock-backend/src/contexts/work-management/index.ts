/**
 * Work Management context — aggregates all WM handler modules.
 *
 * Boards, Lists, Cards are split per plan:
 * "Do not keep growing one monolithic handler file."
 *
 * Plan: 06-HANDLERS-PROJECTIONS.md §Work Management split
 */

import { boardsOperations } from "./boards.handlers";
import { boardViewsOperations } from "./board-views.handlers";
import { savedFiltersOperations } from "./saved-filters.handlers";
import { boardSharingOperations } from "./board-sharing.handlers";
import { listsOperations } from "./lists.handlers";
import { cardsOperations } from "./cards.handlers";
import { myWorkOperations } from "./my-work.handlers";
import { workspaceAutomationsOperations } from "./workspace-automations.handlers";
import { itemStatesOperations } from "./item-states.handlers";

export {
  workspaceAutomationsOperations,
  boardSharingOperations,
  boardsOperations,
  itemStatesOperations,
  savedFiltersOperations,
  listsOperations,
  cardsOperations,
  myWorkOperations,
};

export const workManagementOperations = [
  ...boardsOperations,
  ...boardSharingOperations,
  ...boardViewsOperations,
  ...savedFiltersOperations,
  ...listsOperations,
  ...cardsOperations,
  ...myWorkOperations,
  ...workspaceAutomationsOperations,
  ...itemStatesOperations,
];
