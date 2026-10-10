/**
 * Work Management context — aggregates all WM handler modules.
 *
 * Boards, Lists, Cards are split per plan:
 * "Do not keep growing one monolithic handler file."
 *
 * Plan: 06-HANDLERS-PROJECTIONS.md §Work Management split
 */

import { boardsOperations } from "./boards.handlers";
import { listsOperations } from "./lists.handlers";
import { cardsOperations } from "./cards.handlers";
import { myWorkOperations } from "./my-work.handlers";

export { boardsOperations, listsOperations, cardsOperations, myWorkOperations };

export const workManagementOperations = [
  ...boardsOperations,
  ...listsOperations,
  ...cardsOperations,
  ...myWorkOperations,
];
