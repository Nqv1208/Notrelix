import { useQuery } from "@tanstack/react-query";
import { createPageExtrasApi } from "../../api/page-extras.api";
import type { DocsApiClient, PageApiEndpoints } from "../../api/page.api";
import { docsQueryKeys } from "../keys";

/** Permissions given on the page itself (gap `CTR-GAP-DOCS-PAGE-EXT`). */
export function createUsePagePermissions(
  api: DocsApiClient,
  endpoints: PageApiEndpoints,
) {
  const extras = createPageExtrasApi(api, endpoints);
  return function usePagePermissions(workspaceId: string, pageId: string) {
    return useQuery({
      queryKey: docsQueryKeys.permissions(workspaceId, pageId),
      queryFn: () => extras.getPermissions(pageId),
      enabled: !!workspaceId && !!pageId,
    });
  };
}
