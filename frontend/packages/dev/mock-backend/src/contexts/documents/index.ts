/**
 * Documents context — aggregates all Documents handler modules.
 * Plan: 06-HANDLERS-PROJECTIONS.md §Documents split
 */

import { pagesOperations } from "./pages.handlers";

export { pagesOperations };

export const documentsOperations = [...pagesOperations];
