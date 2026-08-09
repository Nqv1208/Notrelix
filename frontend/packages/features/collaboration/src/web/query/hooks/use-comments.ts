import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import {
  createCommentService,
  type CollaborationApiClient,
  type CollaborationEndpoints,
} from "../../../core/api/comment.service";
import { collaborationQueryKeys } from "../../../core/query/keys";

export function createUseComments(
  api: CollaborationApiClient,
  endpoints: CollaborationEndpoints,
) {
  const service = createCommentService(api, endpoints);
  return function useComments(resourceId: string) {
    return useQuery({
      queryKey: collaborationQueryKeys.comments(resourceId),
      queryFn: () => service.list(resourceId),
      enabled: !!resourceId,
    });
  };
}

export function createUseCreateComment(
  api: CollaborationApiClient,
  endpoints: CollaborationEndpoints,
) {
  const service = createCommentService(api, endpoints);
  const queryClient = useQueryClient();
  return function useCreateComment() {
    return useMutation({
      mutationFn: ({
        resourceId,
        body,
        authorId,
        authorName,
      }: {
        resourceId: string;
        body: string;
        authorId: string;
        authorName: string;
      }) => service.create(resourceId, body, authorId, authorName),
      onSuccess: (
        _data: unknown,
        variables: {
          resourceId: string;
          body: string;
          authorId: string;
          authorName: string;
        },
      ) => {
        queryClient.invalidateQueries({
          queryKey: collaborationQueryKeys.comments(variables.resourceId),
        });
      },
    });
  };
}

export function createUseDeleteComment(
  api: CollaborationApiClient,
  endpoints: CollaborationEndpoints,
) {
  const service = createCommentService(api, endpoints);
  const queryClient = useQueryClient();
  return function useDeleteComment(resourceId: string) {
    return useMutation({
      mutationFn: (commentId: string) => service.remove(commentId),
      onSuccess: () => {
        queryClient.invalidateQueries({
          queryKey: collaborationQueryKeys.comments(resourceId),
        });
      },
    });
  };
}
