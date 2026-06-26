"use client"

import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query"
import { queryKeys } from "@/lib/query/query-keys"
import { commentsApi, ResourceType } from "../api/comments.api"

export function useComments(resourceId: string, resourceType: ResourceType) {
  return useQuery({
    queryKey: [...queryKeys.collaboration.comments(resourceId), resourceType],
    queryFn: () => commentsApi.getComments(resourceId, resourceType),
    enabled: Boolean(resourceId),
    staleTime: 10_000,
  })
}

export function useCreateComment(resourceId: string, resourceType: ResourceType) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (body: string) => commentsApi.createComment(resourceId, resourceType, body),
    onSuccess: () => {
      queryClient.invalidateQueries({
        queryKey: [...queryKeys.collaboration.comments(resourceId), resourceType],
      })
    },
  })
}

export function useUpdateComment(resourceId: string, resourceType: ResourceType) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ commentId, body }: { commentId: string; body: string }) =>
      commentsApi.updateComment(commentId, body),
    onSuccess: () => {
      queryClient.invalidateQueries({
        queryKey: [...queryKeys.collaboration.comments(resourceId), resourceType],
      })
    },
  })
}

export function useDeleteComment(resourceId: string, resourceType: ResourceType) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (commentId: string) => commentsApi.deleteComment(commentId),
    onSuccess: () => {
      queryClient.invalidateQueries({
        queryKey: [...queryKeys.collaboration.comments(resourceId), resourceType],
      })
    },
  })
}
