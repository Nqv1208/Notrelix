import type { BlockDraft, ImportedBlock } from "@notrelix/docs-core";
import { createBatchBlockWriter } from "../editor/batch-block-writer";
import {
  MAX_BATCH_OPERATIONS,
  planBlockWrites,
} from "../editor/doc-block-saver";
import { createBlockApi } from "./block.api";
import { createPageApi } from "./page.api";
import type { DocsApiClient, PageApiEndpoints } from "./page.api";

export interface ImportPageInput {
  readonly title: string;
  readonly blocks: readonly ImportedBlock[];
  readonly parentId?: string | null;
  readonly spaceId?: string | null;
}

/**
 * Imports a file as a NEW page: the page is created, then its blocks are
 * written through the block batch (client ids, each placed after the one
 * before, at most `MAX_BATCH_OPERATIONS` per request). If a write fails the
 * new page is moved to the trash, so a half-imported page does not stay.
 */
export function createImportApi(
  api: DocsApiClient,
  endpoints: PageApiEndpoints,
  newId: () => string = () => crypto.randomUUID(),
) {
  const pageApi = createPageApi(api, endpoints);
  const blockApi = createBlockApi(api, endpoints);
  return {
    /** Answers the id of the new page. */
    async importPage(
      workspaceId: string,
      input: ImportPageInput,
    ): Promise<string> {
      const page = await pageApi.create({
        title: input.title,
        workspaceId,
        parentId: input.parentId ?? null,
        spaceId: input.spaceId ?? null,
      });
      try {
        const drafts: BlockDraft[] = input.blocks.map((entry) => ({
          id: newId(),
          type: entry.type,
          content: entry.content,
          properties: entry.properties,
        }));
        const writer = createBatchBlockWriter(blockApi, page.id);
        const writes = planBlockWrites([], drafts);
        for (let at = 0; at < writes.length; at += MAX_BATCH_OPERATIONS) {
          await writer.write(writes.slice(at, at + MAX_BATCH_OPERATIONS));
        }
      } catch (error) {
        await pageApi.archive(page.id).catch(() => undefined);
        throw error;
      }
      return page.id;
    },
  };
}
