import { useQuery } from "@tanstack/react-query";
import {
  createInvitationsService,
  type InvitationsEndpoints,
} from "../../../core/api/invitations.service";
import type { WorkspaceApiClient } from "../../../core/api/workspace.service";
import { workspaceQueryKeys } from "../../../query/keys";

interface UseWorkspaceInvitationsDeps {
  api: WorkspaceApiClient;
  endpoints: InvitationsEndpoints;
}

export function createUseWorkspaceInvitations({
  api,
  endpoints,
}: UseWorkspaceInvitationsDeps) {
  const service = createInvitationsService(api, endpoints);

  return function useWorkspaceInvitations(workspaceId: string) {
    return useQuery({
      queryKey: workspaceQueryKeys.invitations(workspaceId),
      queryFn: () => service.listForWorkspace(workspaceId),
      enabled: !!workspaceId,
    });
  };
}
