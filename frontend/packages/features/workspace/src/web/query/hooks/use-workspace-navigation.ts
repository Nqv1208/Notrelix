import { useQuery } from "@tanstack/react-query";
import {
  createNavigationService,
  type NavigationApiClient,
  type NavigationEndpoints,
} from "../../../core/api/navigation.service";
import { workspaceQueryKeys } from "../../../query/keys";

interface UseWorkspaceNavigationDeps {
  api: NavigationApiClient;
  endpoints: NavigationEndpoints;
}

/** The member's recently opened Boards/Docs (references, newest first). */
export function createUseWorkspaceRecents({
  api,
  endpoints,
}: UseWorkspaceNavigationDeps) {
  const service = createNavigationService(api, endpoints);

  return function useWorkspaceRecents(workspaceId: string) {
    return useQuery({
      queryKey: workspaceQueryKeys.recents(workspaceId),
      queryFn: () => service.getRecents(workspaceId),
      enabled: !!workspaceId,
      staleTime: 30_000,
    });
  };
}

/** The member's pinned Boards/Docs (references, newest pin first). */
export function createUseWorkspacePins({
  api,
  endpoints,
}: UseWorkspaceNavigationDeps) {
  const service = createNavigationService(api, endpoints);

  return function useWorkspacePins(workspaceId: string) {
    return useQuery({
      queryKey: workspaceQueryKeys.pins(workspaceId),
      queryFn: () => service.getPins(workspaceId),
      enabled: !!workspaceId,
      staleTime: 30_000,
    });
  };
}
