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

/** One Space by id (K03): NotFound is an error of kind not-found, not an empty value. */
export function createUseSpace({ api, endpoints }: UseSpacesDeps) {
  const service = createSpacesService(api, endpoints);

  return function useSpace(workspaceId: string, spaceId: string) {
    return useQuery({
      queryKey: workspaceQueryKeys.spaceDetail(workspaceId, spaceId),
      queryFn: () => service.get(workspaceId, spaceId),
      enabled: !!workspaceId && !!spaceId,
      staleTime: 30_000,
      retry: false,
    });
  };
}

/** The deleted Spaces that can still be restored (workspace Owner or Admin only). */
export function createUseDeletedSpaces({ api, endpoints }: UseSpacesDeps) {
  const service = createSpacesService(api, endpoints);

  return function useDeletedSpaces(workspaceId: string) {
    return useQuery({
      queryKey: workspaceQueryKeys.deletedSpaces(workspaceId),
      queryFn: () => service.listDeleted(workspaceId),
      enabled: !!workspaceId,
      staleTime: 0,
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
