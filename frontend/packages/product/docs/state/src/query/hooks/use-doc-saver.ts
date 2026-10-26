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
import {
  DocBlockSaver,
  type BlockWriter,
  type SaverStatus,
} from "../../editor/doc-block-saver";
import { docsQueryKeys } from "../keys";

/** A saved draft as the page cache holds it, numbered by its place. */
function toBlock(pageId: string, draft: BlockDraft, index: number): Block {
  const now = new Date().toISOString();
  return {
    id: draft.id,
    pageId,
    type: draft.type,
    content: draft.content,
    properties: { ...draft.properties, text: plainTextOf(draft.content) },
    position: (index + 1) * 1000,
    parentId: null,
    createdById: "",
    updatedById: "",
    createdAt: now,
    updatedAt: now,
  };
}

export interface DocSaverOptions {
  /** A block was stored under a different id than the editor gave it. */
  readonly onIdAssigned?: (clientId: string, storedId: string) => void;
  /** A write was refused because the block changed meanwhile. */
  readonly onConflict?: (blockId: string) => void;
}

/**
 * Saves the open page's blocks from the editor: a debounced diff against the
 * last stored snapshot, flushed when the page is left or hidden. The page's
 * block cache is kept equal to what was stored, so the contents and
 * references follow without a refetch.
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

    if (!saverRef.current || saverRef.current.pageId !== pageId) {
      saverRef.current?.saver.dispose();
      const writer: BlockWriter = {
        create: (draft, afterId) =>
          blockApi.createDraft(pageId, draft, afterId),
        update: (id, patch, draft) =>
          blockApi.updateDraft(pageId, id, patch, draft),
        remove: (id) => blockApi.delete(pageId, id),
      };
      saverRef.current = {
        pageId,
        saver: new DocBlockSaver(writer, {
          onStatus: setStatus,
          onIdAssigned: (clientId, storedId) =>
            optionsRef.current.onIdAssigned?.(clientId, storedId),
          onConflict: (blockId) => optionsRef.current.onConflict?.(blockId),
          onSaved: (drafts) =>
            queryClient.setQueryData(
              docsQueryKeys.blocks(workspaceId, pageId),
              drafts.map((draft, index) => toBlock(pageId, draft, index)),
            ),
        }),
      };
      loadedRef.current = null;
    }
    const saver = saverRef.current.saver;

    // The stored document is the baseline once, when the page's blocks first
    // arrive; later refetches never overwrite what the editor holds.
    useEffect(() => {
      if (!blocks || loadedRef.current === pageId) return;
      loadedRef.current = pageId;
      saver.reset(
        [...blocks].sort((a, b) => a.position - b.position).map(draftOfBlock),
      );
    }, [blocks, pageId, saver]);

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
