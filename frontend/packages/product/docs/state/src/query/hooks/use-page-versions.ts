import { useQuery } from "@tanstack/react-query";
import { createVersionApi } from "../../api/version.api";
import type { DocsApiClient, PageApiEndpoints } from "../../api/page.api";
import { docsQueryKeys } from "../keys";

/** The page's history entries, newest first (`Documents.Versions.ListPageVersions`). */
export function createUsePageVersions(
  api: DocsApiClient,
  endpoints: PageApiEndpoints,
) {
  const versionApi = createVersionApi(api, endpoints);
  return function usePageVersions(
    workspaceId: string,
    pageId: string,
    enabled = true,
  ) {
    return useQuery({
      queryKey: docsQueryKeys.versions(workspaceId, pageId),
      queryFn: () => versionApi.list(pageId),
      enabled: enabled && !!workspaceId && !!pageId,
    });
  };
}
