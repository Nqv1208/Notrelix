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
import { boardPreferencesOperations } from "./board-preferences.handlers";
import { formsOperations } from "./forms.handlers";
import { boardSharingOperations } from "./board-sharing.handlers";
import { listsOperations } from "./lists.handlers";
import { cardsOperations } from "./cards.handlers";
import { myWorkOperations } from "./my-work.handlers";
import { workspaceAutomationsOperations } from "./workspace-automations.handlers";
import { itemStatesOperations } from "./item-states.handlers";
import { templatesOperations } from "./templates.handlers";
import { itemCapabilitiesOperations } from "./item-capabilities.handlers";

export {
  workspaceAutomationsOperations,
  boardSharingOperations,
  boardsOperations,
  itemStatesOperations,
  savedFiltersOperations,
  formsOperations,
  listsOperations,
  cardsOperations,
  myWorkOperations,
  templatesOperations,
  itemCapabilitiesOperations,
};

export const workManagementOperations = [
  ...boardsOperations,
  ...boardSharingOperations,
  ...boardViewsOperations,
  ...boardPreferencesOperations,
  ...savedFiltersOperations,
  ...formsOperations,
  ...listsOperations,
  ...cardsOperations,
  ...myWorkOperations,
  ...workspaceAutomationsOperations,
  ...itemStatesOperations,
  ...templatesOperations,
  ...itemCapabilitiesOperations,
];
