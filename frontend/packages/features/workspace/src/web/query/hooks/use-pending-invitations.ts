import { useQuery } from "@tanstack/react-query";
import {
  createInvitationsService,
  type InvitationsEndpoints,
} from "../../../core/api/invitations.service";
import type { WorkspaceApiClient } from "../../../core/api/workspace.service";
import { workspaceQueryKeys } from "../../../query/keys";

interface UsePendingInvitationsDeps {
  api: WorkspaceApiClient;
  endpoints: InvitationsEndpoints;
}

export function createUsePendingInvitations({
  api,
  endpoints,
}: UsePendingInvitationsDeps) {
  const service = createInvitationsService(api, endpoints);

  return function usePendingInvitations() {
    return useQuery({
      queryKey: workspaceQueryKeys.pendingInvitations,
      queryFn: () => service.getPending(),
    });
  };
}
