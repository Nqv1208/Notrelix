import { useCallback, useRef, useState } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import {
  convertBlockProperties,
  type Block,
  type CreateBlockPayload,
  type UpdateBlockPayload,
  type UpdatePagePayload,
} from "@notrelix/docs-core";
import { createBlockApi } from "../../api/block.api";
import {
  createPageApi,
  type DocsApiClient,
  type PageApiEndpoints,
} from "../../api/page.api";
import { docsQueryKeys } from "../keys";

export type DocSaveState = "saved" | "saving" | "error";

/**
 * Edits of one open document: block text and type, new and removed blocks and
 * the title, with the save state the document bar shows. A failed write keeps
 * the state at `error` until `retry` (or a later write) succeeds; nothing is
 * rolled back locally, the next read shows the authoritative document.
 */
export function createUseDocEditor(
  api: DocsApiClient,
  endpoints: PageApiEndpoints,
) {
  const blockApi = createBlockApi(api, endpoints);
  const pageApi = createPageApi(api, endpoints);

  return function useDocEditor(workspaceId: string, pageId: string) {
    const queryClient = useQueryClient();
    const [failed, setFailed] = useState(false);
    const pendingRetry = useRef<(() => void) | null>(null);

    const refreshBlocks = () =>
      queryClient.invalidateQueries({
        queryKey: docsQueryKeys.blocks(workspaceId, pageId),
      });
    const refreshPage = () => {
      void queryClient.invalidateQueries({
        queryKey: docsQueryKeys.detail(workspaceId, pageId),
      });
      void queryClient.invalidateQueries({
        queryKey: docsQueryKeys.list(workspaceId),
      });
      void queryClient.invalidateQueries({
        queryKey: docsQueryKeys.breadcrumb(workspaceId, pageId),
      });
    };

    const update = useMutation({
      mutationFn: (input: { blockId: string; payload: UpdateBlockPayload }) =>
        blockApi.update(pageId, input.blockId, input.payload),
      onSuccess: () => {
        setFailed(false);
        void refreshBlocks();
      },
    });
    const create = useMutation({
      mutationFn: (payload: CreateBlockPayload): Promise<Block> =>
        blockApi.create(pageId, payload),
      onSuccess: () => {
        setFailed(false);
        void refreshBlocks();
      },
    });
    const remove = useMutation({
      mutationFn: (blockId: string) => blockApi.delete(pageId, blockId),
      onSuccess: () => {
        setFailed(false);
        void refreshBlocks();
      },
    });
    const rename = useMutation({
      mutationFn: (payload: UpdatePagePayload) =>
        pageApi.update(pageId, payload),
      onSuccess: () => {
        setFailed(false);
        refreshPage();
      },
    });

    // The producer retypes a block in place: it keeps its id, position and
    // parent, and converts the content the way `convertBlockProperties` does.
    const convert = useMutation({
      mutationFn: async (input: {
        block: Block;
        type: Block["type"];
        properties: Block["properties"];
      }): Promise<Block> => {
        const properties = convertBlockProperties(
          input.block.type,
          input.type,
          input.properties,
        );
        await blockApi.update(pageId, input.block.id, {
          type: input.type,
          properties,
        });
        return { ...input.block, type: input.type, properties };
      },
      onSuccess: () => {
        setFailed(false);
        void refreshBlocks();
      },
    });

    const run = useCallback((retry: () => void) => {
      pendingRetry.current = retry;
    }, []);

    const updateBlock = useCallback(
      (blockId: string, payload: UpdateBlockPayload) => {
        const attempt = () =>
          update.mutate(
            { blockId, payload },
            { onError: () => setFailed(true) },
          );
        run(attempt);
        attempt();
      },
      [run, update],
    );
    const createBlock = useCallback(
      (payload: CreateBlockPayload) => {
        const attempt = () =>
          create.mutateAsync(payload).catch((error: unknown) => {
            setFailed(true);
            throw error;
          });
        run(() => void attempt().catch(() => undefined));
        return attempt();
      },
      [create, run],
    );
    const convertBlock = useCallback(
      (
        block: Block,
        type: Block["type"],
        properties: Block["properties"],
      ): Promise<Block> => {
        const attempt = () =>
          convert
            .mutateAsync({ block, type, properties })
            .catch((error: unknown) => {
              setFailed(true);
              throw error;
            });
        run(() => void attempt().catch(() => undefined));
        return attempt();
      },
      [convert, run],
    );
    const deleteBlock = useCallback(
      (blockId: string) => {
        const attempt = () =>
          remove.mutate(blockId, { onError: () => setFailed(true) });
        run(attempt);
        attempt();
      },
      [remove, run],
    );
    const updateTitle = useCallback(
      (title: string) => {
        const attempt = () =>
          rename.mutate({ title }, { onError: () => setFailed(true) });
        run(attempt);
        attempt();
      },
      [rename, run],
    );
    const retry = useCallback(() => {
      setFailed(false);
      pendingRetry.current?.();
    }, []);

    const saving =
      update.isPending ||
      create.isPending ||
      remove.isPending ||
      convert.isPending ||
      rename.isPending;
    const saveState: DocSaveState = failed
      ? "error"
      : saving
        ? "saving"
        : "saved";

    return {
      saveState,
      updateBlock,
      createBlock,
      convertBlock,
      deleteBlock,
      updateTitle,
      retry,
    };
  };
}
