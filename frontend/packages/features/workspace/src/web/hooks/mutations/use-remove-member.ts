import { useMutation, useQueryClient } from "@tanstack/react-query";
import { createMembersService } from "../../../core/api/members.service";
import type { WorkspaceApiClient } from "../../../core/api/workspace.service";
import { workspaceQueryKeys } from "../../../core/query/keys";

interface UseRemoveMemberDeps {
  api: WorkspaceApiClient;
}

export function createUseRemoveMember({ api }: UseRemoveMemberDeps) {
  const service = createMembersService(api);

  return function useRemoveMember(workspaceId: string) {
    const queryClient = useQueryClient();

    return useMutation({
      mutationFn: (userId: string) => service.removeMember(workspaceId, userId),
      onSuccess: () => {
        queryClient.invalidateQueries({
          queryKey: workspaceQueryKeys.members(workspaceId),
        });
      },
    });
  };
}
