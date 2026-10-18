import { useQuery } from "@tanstack/react-query";
import {
  createSpacesService,
  type SpacesApiClient,
  type SpacesEndpoints,
} from "../../../core/api/spaces.service";
import { workspaceQueryKeys } from "../../../query/keys";

interface UseSpacesDeps {
  api: SpacesApiClient;
  endpoints: SpacesEndpoints;
}

/** The caller's visible Space org tree (flat, parent-linked; GOV-029). */
export function createUseSpaceTree({ api, endpoints }: UseSpacesDeps) {
  const service = createSpacesService(api, endpoints);

  return function useSpaceTree(workspaceId: string) {
    return useQuery({
      queryKey: workspaceQueryKeys.spaceTree(workspaceId),
      queryFn: () => service.getTree(workspaceId),
      enabled: !!workspaceId,
      staleTime: 30_000,
    });
  };
}

/** The direct User and Team entries of one Space (WSP-024). */
export function createUseSpaceMembers({ api, endpoints }: UseSpacesDeps) {
  const service = createSpacesService(api, endpoints);

  return function useSpaceMembers(workspaceId: string, spaceId: string | null) {
    return useQuery({
      queryKey: workspaceQueryKeys.spaceMembers(workspaceId, spaceId ?? ""),
      queryFn: () => service.getMembers(workspaceId, spaceId!),
      enabled: !!workspaceId && !!spaceId,
      staleTime: 30_000,
    });
  };
}
