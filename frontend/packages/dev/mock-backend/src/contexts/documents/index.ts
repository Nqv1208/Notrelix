/**
 * Documents context — aggregates all Documents handler modules.
 * Plan: 06-HANDLERS-PROJECTIONS.md §Documents split
 */

import { pagesOperations } from "./pages.handlers";
import { blocksOperations } from "./blocks.handlers";
import { commentsOperations } from "./comments.handlers";
import { historyOperations } from "./history.handlers";

export { pagesOperations, blocksOperations, commentsOperations, historyOperations };

export const documentsOperations = [
  ...pagesOperations,
  ...blocksOperations,
  ...commentsOperations,
  ...historyOperations,
];
