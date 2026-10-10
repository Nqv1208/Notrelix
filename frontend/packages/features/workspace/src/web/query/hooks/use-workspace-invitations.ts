import { useQuery } from "@tanstack/react-query";
import {
  createWorkspaceInvitationsService,
  type WorkspaceInvitationsEndpoints,
} from "../../../core/api/invitations.service";
import type { WorkspaceApiClient } from "../../../core/api/workspace.service";
import { workspaceQueryKeys } from "../../../query/keys";

interface UseWorkspaceInvitationsDeps {
  api: WorkspaceApiClient;
  endpoints: WorkspaceInvitationsEndpoints;
}

/** The workspace's invitations (`Workspaces.Invitations.ListWorkspaceInvitations`). */
export function createUseWorkspaceInvitations({
  api,
  endpoints,
}: UseWorkspaceInvitationsDeps) {
  const service = createWorkspaceInvitationsService(api, endpoints);

  return function useWorkspaceInvitations(
    workspaceId: string,
    options: { readonly enabled?: boolean } = {},
  ) {
    return useQuery({
      queryKey: workspaceQueryKeys.invitations(workspaceId),
      queryFn: () => service.getList(workspaceId),
      enabled: !!workspaceId && (options.enabled ?? true),
    });
  };
}
