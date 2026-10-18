import { useQuery } from "@tanstack/react-query";
import {
  createWorkspaceSettingsService,
  type WorkspaceApiClient,
  type WorkspaceSettingsEndpoints,
} from "../../../core/api/workspace.service";
import { workspaceQueryKeys } from "../../../query/keys";

interface UseWorkspaceSettingsDeps {
  api: WorkspaceApiClient;
  endpoints: WorkspaceSettingsEndpoints;
}

/** Workspace defaults (`Workspaces.Settings.GetWorkspaceSettings`). */
export function createUseWorkspaceSettings({
  api,
  endpoints,
}: UseWorkspaceSettingsDeps) {
  const service = createWorkspaceSettingsService(api, endpoints);

  return function useWorkspaceSettings(workspaceId: string) {
    return useQuery({
      queryKey: workspaceQueryKeys.settings(workspaceId),
      queryFn: () => service.getSettings(workspaceId),
      enabled: !!workspaceId,
    });
  };
}
