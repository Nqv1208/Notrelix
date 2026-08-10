import { useQuery } from "@tanstack/react-query";
import { createFavoriteApi } from "../../api/favorite.api";
import type { DocsApiClient, PageApiEndpoints } from "../../api/page.api";
import { docsQueryKeys } from "../keys";

export function createUseDocsFavorites(
  api: DocsApiClient,
  endpoints: PageApiEndpoints,
) {
  const favoriteApi = createFavoriteApi(api, endpoints);
  return function useDocsFavorites(workspaceId: string) {
    return useQuery({
      queryKey: docsQueryKeys.favorites(workspaceId),
      queryFn: () => favoriteApi.getFavorites(workspaceId),
      enabled: !!workspaceId,
    });
  };
}
