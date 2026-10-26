import { useMutation, useQueryClient } from "@tanstack/react-query";
import type { CreatePageTemplatePayload } from "@notrelix/docs-core";
import { createTemplateApi } from "../../api/template.api";
import type { DocsApiClient, PageApiEndpoints } from "../../api/page.api";
import { docsQueryKeys } from "../keys";

/** "Save as template": a page's content becomes a workspace template. */
export function createUseCreatePageTemplate(
  api: DocsApiClient,
  endpoints: PageApiEndpoints,
) {
  const templateApi = createTemplateApi(api, endpoints);
  return function useCreatePageTemplate(workspaceId: string) {
    const queryClient = useQueryClient();
    return useMutation({
      mutationFn: (payload: CreatePageTemplatePayload) =>
        templateApi.create(workspaceId, payload),
      onSuccess: () =>
        queryClient.invalidateQueries({
          queryKey: docsQueryKeys.templates(workspaceId),
        }),
    });
  };
}
