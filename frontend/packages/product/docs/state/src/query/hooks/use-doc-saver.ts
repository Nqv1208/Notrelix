import { useCallback, useEffect, useRef, useState } from "react";
import { useQueryClient } from "@tanstack/react-query";
import {
  draftOfBlock,
  plainTextOf,
  type Block,
  type BlockDraft,
} from "@notrelix/docs-core";
import { createBlockApi } from "../../api/block.api";
import type { DocsApiClient, PageApiEndpoints } from "../../api/page.api";
import { createBatchBlockWriter } from "../../editor/batch-block-writer";
import {
  DocBlockSaver,
  type BlockConflict,
  type SaverStatus,
} from "../../editor/doc-block-saver";
import { docsQueryKeys } from "../keys";

/** A stored draft as the page cache holds it, numbered by its place. */
function toBlock(
  pageId: string,
  draft: BlockDraft,
  index: number,
  known: Block | undefined,
): Block {
  const now = new Date().toISOString();
  return {
    id: draft.id,
    pageId,
    type: draft.type,
    content: draft.content,
    properties: { ...draft.properties, text: plainTextOf(draft.content) },
    ...(draft.version !== undefined ? { version: draft.version } : {}),
    position: (index + 1) * 1000,
    parentId: known?.parentId ?? null,
    createdById: known?.createdById ?? "",
    updatedById: known?.updatedById ?? "",
    createdAt: known?.createdAt ?? now,
    updatedAt: now,
  };
}

export interface DocSaverOptions {
  /** A block was refused as stale: the editor shows the stored one. */
  readonly onConflict?: (conflict: BlockConflict) => void;
  /** The stored document changed outside the member's edits: show `drafts`. */
  readonly onRebased?: (drafts: readonly BlockDraft[]) => void;
}

/**
 * Saves the open page's blocks from the editor: a debounced diff against the
 * last stored snapshot written as one batch, flushed when the page is left or
 * hidden. The page's block cache is kept equal to what was stored (with the
 * versions the producer answered), so the contents and references follow
 * without a refetch. Refetched blocks (realtime, reconnect) merge into the
 * editor when nothing of the member's is pending.
 */
export function createUseDocSaver(
  api: DocsApiClient,
  endpoints: PageApiEndpoints,
) {
  const blockApi = createBlockApi(api, endpoints);

  return function useDocSaver(
    workspaceId: string,
    pageId: string,
    blocks: readonly Block[] | undefined,
    options: DocSaverOptions = {},
  ) {
    const queryClient = useQueryClient();
    const [status, setStatus] = useState<SaverStatus>("saved");
    const optionsRef = useRef(options);
    optionsRef.current = options;
    const saverRef = useRef<{ pageId: string; saver: DocBlockSaver } | null>(
      null,
    );
    const loadedRef = useRef<string | null>(null);
    const staleRef = useRef(false);

    if (!saverRef.current || saverRef.current.pageId !== pageId) {
      saverRef.current?.saver.dispose();
      const key = docsQueryKeys.blocks(workspaceId, pageId);
      saverRef.current = {
        pageId,
        saver: new DocBlockSaver(createBatchBlockWriter(blockApi, pageId), {
          onStatus: setStatus,
          onConflict: (conflict) => optionsRef.current.onConflict?.(conflict),
          onRebased: (drafts) => optionsRef.current.onRebased?.(drafts),
          onSaved: (drafts) => {
            const known = new Map(
              (queryClient.getQueryData<Block[]>(key) ?? []).map((block) => [
                block.id,
                block,
              ]),
            );
            queryClient.setQueryData(
              key,
              drafts.map((draft, index) =>
                toBlock(pageId, draft, index, known.get(draft.id)),
              ),
            );
          },
        }),
      };
      loadedRef.current = null;
    }
    const saver = saverRef.current.saver;

    // The stored document is the baseline when the page's blocks first arrive;
    // later refetches merge only what the member is not editing.
    useEffect(() => {
      if (!blocks) return;
      const stored = [...blocks]
        .sort((a, b) => a.position - b.position)
        .map(draftOfBlock);
      if (loadedRef.current !== pageId) {
        loadedRef.current = pageId;
        saver.reset(stored);
        return;
      }
      staleRef.current = !saver.mergeStored(stored);
    }, [blocks, pageId, saver]);

    // Others' changes that had to wait for the member's pending edits merge
    // once the saver is idle.
    useEffect(() => {
      if (status !== "saved" || !staleRef.current) return;
      staleRef.current = false;
      void queryClient.invalidateQueries({
        queryKey: docsQueryKeys.blocks(workspaceId, pageId),
      });
    }, [status, queryClient, workspaceId, pageId]);

    useEffect(() => {
      const flush = () => void saver.flush();
      const onVisibility = () => {
        if (document.visibilityState === "hidden") flush();
      };
      document.addEventListener("visibilitychange", onVisibility);
      window.addEventListener("pagehide", flush);
      return () => {
        document.removeEventListener("visibilitychange", onVisibility);
        window.removeEventListener("pagehide", flush);
        void saver.flush();
      };
    }, [saver]);

    const change = useCallback(
      (drafts: readonly BlockDraft[]) => saver.change(drafts),
      [saver],
    );
    const flush = useCallback(() => saver.flush(), [saver]);
    const retry = useCallback(() => saver.retry(), [saver]);

    return { saveState: status, change, flush, retry };
  };
}
