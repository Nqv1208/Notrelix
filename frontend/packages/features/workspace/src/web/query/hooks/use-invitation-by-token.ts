import { useQuery } from "@tanstack/react-query";
import {
  createInvitationsService,
  type InvitationsEndpoints,
} from "../../../core/api/invitations.service";
import type { WorkspaceApiClient } from "../../../core/api/workspace.service";
import { workspaceQueryKeys } from "../../../query/keys";

interface UseInvitationByTokenDeps {
  api: WorkspaceApiClient;
  endpoints: InvitationsEndpoints;
}

export function createUseInvitationByToken({
  api,
  endpoints,
}: UseInvitationByTokenDeps) {
  const service = createInvitationsService(api, endpoints);

  return function useInvitationByToken(token: string) {
    return useQuery({
      queryKey: workspaceQueryKeys.invitationPreview(token),
      queryFn: () => service.getPreview(token),
      enabled: !!token,
    });
  };
}
