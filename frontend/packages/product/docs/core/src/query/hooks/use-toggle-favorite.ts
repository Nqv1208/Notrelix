import { useMutation, useQueryClient } from '@tanstack/react-query';
import { createFavoriteApi } from '~/api/favorite.api';
import type { DocsApiClient, PageApiEndpoints } from '~/api/page.api';
import { docsQueryKeys } from '../keys';

export function createUseToggleFavorite(api: DocsApiClient, endpoints: PageApiEndpoints) {
  const favoriteApi = createFavoriteApi(api, endpoints);
  return function useToggleFavorite(workspaceId: string, pageId: string) {
    const queryClient = useQueryClient();
    return useMutation({
      mutationFn: () => favoriteApi.toggleFavorite(pageId),
      onSuccess: () => {
        queryClient.invalidateQueries({ queryKey: docsQueryKeys.favorites(workspaceId) });
        queryClient.invalidateQueries({ queryKey: docsQueryKeys.detail(pageId) });
        queryClient.invalidateQueries({ queryKey: docsQueryKeys.list(workspaceId) });
      },
    });
  };
}
