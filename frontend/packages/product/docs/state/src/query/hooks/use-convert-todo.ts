import { useMutation, useQueryClient } from "@tanstack/react-query";
import type { Block } from "@notrelix/docs-core";
import {
  createPageExtrasApi,
  type ConvertTodoTarget,
} from "../../api/page-extras.api";
import type { DocsApiClient, PageApiEndpoints } from "../../api/page.api";
import { docsQueryKeys } from "../keys";

/** Turns a checklist line into a work item in the chosen board group. */
export function createUseConvertTodo(
  api: DocsApiClient,
  endpoints: PageApiEndpoints,
) {
  const extras = createPageExtrasApi(api, endpoints);
  return function useConvertTodo(workspaceId: string, pageId: string) {
    const queryClient = useQueryClient();
    return useMutation({
      mutationFn: (input: { block: Block; target: ConvertTodoTarget }) =>
        extras.convertTodo(pageId, input.block, input.target),
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
