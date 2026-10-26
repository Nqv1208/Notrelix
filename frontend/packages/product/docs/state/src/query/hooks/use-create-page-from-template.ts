import { useMutation, useQueryClient } from "@tanstack/react-query";
import type { CreatePageFromTemplatePayload } from "@notrelix/docs-core";
import { createTemplateApi } from "../../api/template.api";
import type { DocsApiClient, PageApiEndpoints } from "../../api/page.api";
import { docsQueryKeys } from "../keys";

/** "New from template": an ordinary page with copies of the template's blocks. */
export function createUseCreatePageFromTemplate(
  api: DocsApiClient,
  endpoints: PageApiEndpoints,
) {
  const templateApi = createTemplateApi(api, endpoints);
  return function useCreatePageFromTemplate(workspaceId: string) {
    const queryClient = useQueryClient();
    return useMutation({
      mutationFn: (payload: CreatePageFromTemplatePayload) =>
        templateApi.createPage(workspaceId, payload),
      onSuccess: () => {
        void queryClient.invalidateQueries({
          queryKey: docsQueryKeys.list(workspaceId),
        });
        void queryClient.invalidateQueries({
          queryKey: docsQueryKeys.tree(workspaceId),
        });
      },
    });
  };
}
