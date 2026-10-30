import { useCallback, useRef, useState } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import {
  convertBlockProperties,
  type Block,
  type CreateBlockPayload,
  type PageDetail,
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

/** The producer refused a rename because the page changed meanwhile (412). */
class PageVersionConflict extends Error {
  constructor(readonly current: PageDetail) {
    super("The page changed since it was read.");
    this.name = "PageVersionConflict";
  }
}

/**
 * Edits of one open document: block text and type, new and removed blocks and
 * the title, with the save state the document bar shows. A failed write keeps
 * the state at `error` until `retry` (or a later write) succeeds; nothing is
 * rolled back locally, the next read shows the authoritative document.
 * A rename carries the page version it read: if the page changed meanwhile
 * the title shows the stored one again and `titleConflict` is set until
 * `clearTitleConflict`, so the screen can tell the person.
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
    const [titleConflict, setTitleConflict] = useState(false);
    const pendingRetry = useRef<(() => void) | null>(null);

    /** The version of a block as the page cache last read it. */
    const cachedVersion = (blockId: string): number | undefined =>
      queryClient
        .getQueryData<Block[]>(docsQueryKeys.blocks(workspaceId, pageId))
        ?.find((block) => block.id === blockId)?.version;

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
        blockApi.update(pageId, input.blockId, {
          ...input.payload,
          expectedVersion:
            input.payload.expectedVersion ?? cachedVersion(input.blockId),
        }),
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
    // The block goes with the blocks under it, against the version this page
    // last read. A block that is already gone, or changed meanwhile, is not a
    // failed save: the page shows what is stored (409/412 here would only
    // repeat the same stale request).
    const remove = useMutation({
      mutationFn: async (blockId: string) => {
        const version =
          cachedVersion(blockId) ??
          (await blockApi.getList(pageId)).find((block) => block.id === blockId)
            ?.version;
        if (version === undefined) return;
        try {
          await blockApi.delete(pageId, blockId, version);
        } catch (error) {
          const status = (error as { status?: unknown }).status;
          if (status !== 404 && status !== 412) throw error;
        }
      },
      onSuccess: () => {
        setFailed(false);
        void refreshBlocks();
      },
    });
    const rename = useMutation({
      mutationFn: async (payload: UpdatePagePayload) => {
        const cached = queryClient.getQueryData<PageDetail>(
          docsQueryKeys.detail(workspaceId, pageId),
        );
        try {
          return await pageApi.update(pageId, {
            ...payload,
            expectedVersion:
              cached?.version ?? (await pageApi.getDetail(pageId)).version,
          });
        } catch (error) {
          if ((error as { status?: unknown }).status !== 412) throw error;
          const current = await pageApi.getDetail(pageId);
          // The same rename already went through (a retry after success).
          if (current.title === payload.title?.trim()) return current;
          throw new PageVersionConflict(current);
        }
      },
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
        const stored = await blockApi.update(pageId, input.block.id, {
          type: input.type,
          properties,
          expectedVersion: input.block.version ?? cachedVersion(input.block.id),
        });
        return { ...input.block, ...stored, position: input.block.position };
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
          rename.mutate(
            { title },
            {
              onError: (error) => {
                if (error instanceof PageVersionConflict) {
                  setTitleConflict(true);
                  refreshPage();
                } else setFailed(true);
              },
            },
          );
        run(attempt);
        attempt();
      },
      [rename, run],
    );
    const clearTitleConflict = useCallback(() => setTitleConflict(false), []);
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
      titleConflict,
      clearTitleConflict,
      retry,
    };
  };
}
