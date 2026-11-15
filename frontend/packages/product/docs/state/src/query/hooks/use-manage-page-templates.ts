import { useMutation, useQueryClient } from "@tanstack/react-query";
import type {
  PageTemplateVersionPayload,
  UpdatePageTemplatePayload,
} from "@notrelix/docs-core";
import { createTemplateApi } from "../../api/template.api";
import type { DocsApiClient, PageApiEndpoints } from "../../api/page.api";
import { docsQueryKeys } from "../keys";

/**
 * Rename, archive and delete a template. The list is read again after every answer, a refusal too: a stale
 * version (412) means the list the person saw is out of date.
 */
export function createUseManagePageTemplates(
  api: DocsApiClient,
  endpoints: PageApiEndpoints,
) {
  const templateApi = createTemplateApi(api, endpoints);
  return function useManagePageTemplates(workspaceId: string) {
    const queryClient = useQueryClient();
    const refresh = () =>
      queryClient.invalidateQueries({
        queryKey: docsQueryKeys.templates(workspaceId),
      });
    const rename = useMutation({
      mutationFn: (payload: UpdatePageTemplatePayload) =>
        templateApi.update(workspaceId, payload),
      onSettled: refresh,
    });
    const archive = useMutation({
      mutationFn: (payload: PageTemplateVersionPayload) =>
        templateApi.archive(workspaceId, payload),
      onSettled: refresh,
    });
    const remove = useMutation({
      mutationFn: (payload: PageTemplateVersionPayload) =>
        templateApi.remove(workspaceId, payload),
      onSettled: refresh,
    });
    return { rename, archive, remove };
  };
}
