import { useMutation, useQueryClient } from '@tanstack/react-query';
import { createCommentApi } from '../../api/comment.api';
import type { DocsApiClient, PageApiEndpoints } from '../../api/page.api';
import { docsQueryKeys } from '../keys';

export function createUseDeleteComment(api: DocsApiClient, endpoints: PageApiEndpoints) {
  const commentApi = createCommentApi(api, endpoints);
  return function useDeleteComment(pageId: string) {
    const queryClient = useQueryClient();
    return useMutation({
      mutationFn: commentApi.delete,
      onSuccess: () => {
        queryClient.invalidateQueries({ queryKey: docsQueryKeys.comments(pageId) });
      },
    });
  };
}
