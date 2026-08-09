import { useQuery } from "@tanstack/react-query";
import {
  createInvitationsService,
  type InvitationsEndpoints,
} from "../../../core/api/invitations.service";
import type { WorkspaceApiClient } from "../../../core/api/workspace.service";
import { workspaceQueryKeys } from "../../../core/query/keys";

interface UseInvitationByTokenDeps {
  api: WorkspaceApiClient;
  endpoints: InvitationsEndpoints;
  options?: {
    mockMode?: boolean;
  };
}

export function createUseInvitationByToken({
  api,
  endpoints,
  options,
}: UseInvitationByTokenDeps) {
  const service = createInvitationsService(api, endpoints, options);

  return function useInvitationByToken(token: string) {
    return useQuery({
      queryKey: workspaceQueryKeys.invitationByToken(token),
      queryFn: () => service.getByToken(token),
      enabled: !!token,
    });
  };
}
