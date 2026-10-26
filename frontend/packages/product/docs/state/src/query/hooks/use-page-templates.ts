import { useQuery } from "@tanstack/react-query";
import { createTemplateApi } from "../../api/template.api";
import type { DocsApiClient, PageApiEndpoints } from "../../api/page.api";
import { docsQueryKeys } from "../keys";

/** The workspace's page templates by name (`Documents.Templates.ListPageTemplates`). */
export function createUsePageTemplates(
  api: DocsApiClient,
  endpoints: PageApiEndpoints,
) {
  const templateApi = createTemplateApi(api, endpoints);
  return function usePageTemplates(workspaceId: string, enabled = true) {
    return useQuery({
      queryKey: docsQueryKeys.templates(workspaceId),
      queryFn: () => templateApi.list(workspaceId),
      enabled: enabled && !!workspaceId,
    });
  };
}
