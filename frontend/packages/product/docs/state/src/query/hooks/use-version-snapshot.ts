import { useQuery } from "@tanstack/react-query";
import { createVersionApi } from "../../api/version.api";
import type { DocsApiClient, PageApiEndpoints } from "../../api/page.api";
import { docsQueryKeys } from "../keys";

/**
 * The content one history entry captured (`Documents.Versions.GetPageVersion`).
 * An entry never changes, so a read stays fresh for a long time.
 */
export function createUseVersionSnapshot(
  api: DocsApiClient,
  endpoints: PageApiEndpoints,
) {
  const versionApi = createVersionApi(api, endpoints);
  return function useVersionSnapshot(
    workspaceId: string,
    pageId: string,
    versionId: string | null,
  ) {
    return useQuery({
      queryKey: docsQueryKeys.versionSnapshot(
        workspaceId,
        pageId,
        versionId ?? "",
      ),
      queryFn: () => versionApi.get(pageId, versionId ?? ""),
      enabled: !!workspaceId && !!pageId && !!versionId,
      staleTime: 5 * 60_000,
    });
  };
}
