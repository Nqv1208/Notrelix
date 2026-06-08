"use client"

import { useMutation, useQueryClient } from "@tanstack/react-query"
import { queryKeys } from "@/lib/query/query-keys"
import { workspaceApi } from "../../api/workspace.api"
import type { CreateWorkspaceInput } from "../../types"

export function useCreateWorkspace() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (input: CreateWorkspaceInput) => workspaceApi.createWorkspace(input),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.workspaces.all })
    },
  })
}
