import { useAccountId } from "@notrelix/query";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useRef } from "react";
import {
  createWorkspaceService,
  type WorkspaceApiClient,
  type WorkspaceEndpoints,
} from "../../../core/api/workspace.service";
import type { CreateWorkspaceInput } from "../../../core/types/workspace";
import { workspaceQueryKeys } from "../../../query/keys";

interface UseCreateWorkspaceDeps {
  api: WorkspaceApiClient;
  endpoints: WorkspaceEndpoints;
}

export function createUseCreateWorkspace({
  api,
  endpoints,
}: UseCreateWorkspaceDeps) {
  const service = createWorkspaceService(api, endpoints);

  return function useCreateWorkspace() {
    const accountId = useAccountId();
    const queryClient = useQueryClient();
    // One key per create the person asked for: a retry of the same input reuses it, so the producer
    // answers with the workspace the first attempt made; a changed input is a new request.
    const attempt = useRef<{ signature: string; key: string } | null>(null);

    return useMutation({
      mutationFn: (input: CreateWorkspaceInput) => {
        const signature = JSON.stringify([accountId, input]);
        if (attempt.current?.signature !== signature)
          attempt.current = { signature, key: crypto.randomUUID() };
        return service.create(accountId, input, attempt.current.key);
      },
      onSuccess: () => {
        attempt.current = null;
        queryClient.invalidateQueries({
          queryKey: workspaceQueryKeys.all(accountId),
        });
      },
    });
  };
}
