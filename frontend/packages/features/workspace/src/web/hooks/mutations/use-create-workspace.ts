import { useAccountId } from "@notrelix/query";
import { useMutation, useQueryClient } from "@tanstack/react-query";
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

    return useMutation({
      mutationFn: (input: CreateWorkspaceInput) =>
        service.create(accountId, input),
      onSuccess: () => {
        queryClient.invalidateQueries({
          queryKey: workspaceQueryKeys.all(accountId),
        });
      },
    });
  };
}
