/**
 * Documents context — aggregates all Documents handler modules.
 * Plan: 06-HANDLERS-PROJECTIONS.md §Documents split
 */

import { pagesOperations } from "./pages.handlers";
import { blocksOperations } from "./blocks.handlers";
import { commentsOperations } from "./comments.handlers";
import { historyOperations } from "./history.handlers";
import { pageExtrasOperations } from "./page-extras.handlers";
import { pageAttachmentsOperations } from "./page-attachments.handlers";

export {
  pagesOperations,
  blocksOperations,
  commentsOperations,
  historyOperations,
  pageExtrasOperations,
  pageAttachmentsOperations,
};

export const documentsOperations = [
  ...pagesOperations,
  ...blocksOperations,
  ...commentsOperations,
  ...historyOperations,
  ...pageExtrasOperations,
  ...pageAttachmentsOperations,
];
