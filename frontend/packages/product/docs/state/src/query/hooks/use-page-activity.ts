import { useQuery } from "@tanstack/react-query";
import type { PageActivityPageDtoApi } from "../../dto";
import type { DocsApiClient, PageApiEndpoints } from "../../api/page.api";
import { docsQueryKeys } from "../keys";

/** One page of the page's activity feed, newest first (`Documents.Pages.GetPageActivity`). */
export function createUsePageActivity(
  api: DocsApiClient,
  endpoints: PageApiEndpoints,
) {
  return function usePageActivity(
    workspaceId: string,
    pageId: string,
    page = 1,
    enabled = true,
  ) {
    return useQuery({
      queryKey: docsQueryKeys.activity(workspaceId, pageId, page),
      queryFn: () =>
        api.get<PageActivityPageDtoApi>(
          `${endpoints.pages.activity(pageId)}?page=${page}`,
        ),
      enabled: enabled && !!workspaceId && !!pageId,
    });
  };
}
