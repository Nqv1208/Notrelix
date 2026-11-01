import { useRef } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import type { Block } from "@notrelix/docs-core";
import {
  createPageExtrasApi,
  type ConvertTodoTarget,
} from "../../api/page-extras.api";
import type { DocsApiClient, PageApiEndpoints } from "../../api/page.api";
import { docsQueryKeys } from "../keys";

/**
 * Turns a checklist line into a work item in the chosen board group. A failed
 * conversion keeps its attempt key until it succeeds, so asking again for the
 * same line finishes the conversion instead of creating another item.
 */
export function createUseConvertTodo(
  api: DocsApiClient,
  endpoints: PageApiEndpoints,
) {
  const extras = createPageExtrasApi(api, endpoints);
  return function useConvertTodo(workspaceId: string, pageId: string) {
    const queryClient = useQueryClient();
    const attempts = useRef(new Map<string, string>());
    return useMutation({
      mutationFn: async (input: {
        block: Block;
        target: ConvertTodoTarget;
      }) => {
        const key = `${input.block.id}:${input.target.boardId}:${input.target.groupId}`;
        const attempt = attempts.current.get(key) ?? crypto.randomUUID();
        attempts.current.set(key, attempt);
        const converted = await extras.convertTodo(
          pageId,
          input.block,
          input.target,
          attempt,
        );
        attempts.current.delete(key);
        return converted;
      },
      onSettled: () => {
        void queryClient.invalidateQueries({
          queryKey: docsQueryKeys.blocks(workspaceId, pageId),
        });
        void queryClient.invalidateQueries({
          queryKey: docsQueryKeys.backlinks(workspaceId, pageId),
        });
      },
    });
  };
}
