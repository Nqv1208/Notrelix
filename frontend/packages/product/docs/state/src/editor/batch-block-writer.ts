import { draftOfBlock, type BlockDraft } from "@notrelix/docs-core";
import { createBlockApi } from "../api/block.api";
import type { BatchBlockItemApi, BlockDtoApi } from "../dto";
import { mapBlock, toProducerBlockType } from "../model/block.mapper";
import { encodeContent, encodeProperties } from "../model/block-wire";
import {
  BlockVersionConflict,
  type BlockWrite,
  type BlockWriter,
  type SavedBlock,
} from "./doc-block-saver";

export const VERSION_CONFLICT_CODE = "documents.block.version-conflict";

type BlockApi = ReturnType<typeof createBlockApi>;

/** The batch operation for one write of the saver. */
export function toBatchItem(write: BlockWrite): BatchBlockItemApi {
  if (write.op === "delete") {
    return {
      id: write.id,
      op: "delete",
      ...(write.expectedVersion !== undefined
        ? { expectedVersion: write.expectedVersion }
        : {}),
    };
  }
  const draft = write.draft as BlockDraft;
  const placement = write.afterId ? { afterBlockId: write.afterId } : {};
  if (!write.patch) {
    // A new block: the client's id, everything it holds.
    return {
      id: write.id,
      op: "upsert",
      type: toProducerBlockType(draft.type),
      content: encodeContent(draft),
      properties: encodeProperties(draft),
      ...placement,
    };
  }
  const { patch } = write;
  const retyped = patch.type !== undefined;
  return {
    id: write.id,
    op: "upsert",
    ...(retyped ? { type: toProducerBlockType(draft.type) } : {}),
    ...(patch.content || retyped ? { content: encodeContent(draft) } : {}),
    ...(patch.properties || retyped
      ? { properties: encodeProperties(draft) }
      : {}),
    ...(write.expectedVersion !== undefined
      ? { expectedVersion: write.expectedVersion }
      : {}),
    ...placement,
  };
}

/** The block the producer sent with a 412, when the error is one. */
export function conflictBlockOf(error: unknown): BlockDraft | null {
  if (!error || typeof error !== "object") return null;
  const { status, details } = error as { status?: unknown; details?: unknown };
  if (status !== 412 || !details || typeof details !== "object") return null;
  const problem = details as Record<string, unknown>;
  const extensions = problem.extensions as Record<string, unknown> | undefined;
  const current = (extensions?.currentBlock ?? problem.currentBlock) as
    BlockDtoApi | undefined;
  if (!current || typeof current !== "object" || !current.id) return null;
  return draftOfBlock(mapBlock(current, 0));
}

/**
 * Writes a save as one `POST /pages/{pageId}/blocks/batch`: client ids for the
 * creates, `expectedVersion` for the edits and deletes, placement after the
 * predecessor. A 412 becomes a `BlockVersionConflict` with the stored block.
 */
export function createBatchBlockWriter(
  blockApi: Pick<BlockApi, "batch">,
  pageId: string,
): BlockWriter {
  return {
    async write(ops): Promise<readonly SavedBlock[]> {
      try {
        const saved = await blockApi.batch(pageId, ops.map(toBatchItem));
        return saved.map((dto) => ({ id: dto.id, version: dto.version }));
      } catch (error) {
        const server = conflictBlockOf(error);
        if (server) throw new BlockVersionConflict(server);
        throw error;
      }
    },
  };
}
