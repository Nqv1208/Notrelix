import { useQuery } from "@tanstack/react-query";
import { createPageExtrasApi } from "../../api/page-extras.api";
import type { DocsApiClient, PageApiEndpoints } from "../../api/page.api";
import { docsQueryKeys } from "../keys";

/** What mentions the page (gap `CTR-GAP-DOCS-PAGE-EXT`). */
export function createUsePageBacklinks(
  api: DocsApiClient,
  endpoints: PageApiEndpoints,
) {
  const extras = createPageExtrasApi(api, endpoints);
  return function usePageBacklinks(workspaceId: string, pageId: string) {
    return useQuery({
      queryKey: docsQueryKeys.backlinks(workspaceId, pageId),
      queryFn: () => extras.getBacklinks(pageId),
      enabled: !!workspaceId && !!pageId,
    });
  };
}
