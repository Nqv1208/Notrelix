import { useQuery } from "@tanstack/react-query";
import { createPageExtrasApi } from "../../api/page-extras.api";
import type { DocsApiClient, PageApiEndpoints } from "../../api/page.api";
import { docsQueryKeys } from "../keys";

/** The blocks of a history entry's version (gap `CTR-GAP-DOCS-PAGE-EXT`). */
export function createUseVersionSnapshot(
  api: DocsApiClient,
  endpoints: PageApiEndpoints,
) {
  const extras = createPageExtrasApi(api, endpoints);
  return function useVersionSnapshot(
    workspaceId: string,
    pageId: string,
    entryId: string | null,
  ) {
    return useQuery({
      queryKey: docsQueryKeys.versionSnapshot(
        workspaceId,
        pageId,
        entryId ?? "",
      ),
      queryFn: () => extras.getVersionSnapshot(pageId, entryId ?? ""),
      enabled: !!workspaceId && !!pageId && !!entryId,
      staleTime: 60_000,
    });
  };
}
