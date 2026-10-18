import { useMutation, useQueryClient } from "@tanstack/react-query";
import {
  createWorkspaceSettingsService,
  type WorkspaceApiClient,
  type WorkspaceSettingsEndpoints,
} from "../../../core/api/workspace.service";
import type {
  UpdateWorkspaceProfileInput,
  WorkspaceSettings,
} from "../../../core/types/workspace";
import { workspaceQueryKeys } from "../../../query/keys";

interface SettingsMutationDeps {
  api: WorkspaceApiClient;
  endpoints: WorkspaceSettingsEndpoints;
}

/**
 * Workspace profile, defaults and archive writes (WX-SET-001). Each carries
 * the version the member last read; a stale version is rejected by the
 * producer and surfaced as a conflict, never retried automatically.
 */
export function createUseWorkspaceSettingsMutations({
  api,
  endpoints,
}: SettingsMutationDeps) {
  const service = createWorkspaceSettingsService(api, endpoints);

  return function useWorkspaceSettingsMutations(workspaceId: string) {
    const queryClient = useQueryClient();
    const refreshWorkspace = async () => {
      await queryClient.invalidateQueries({
        queryKey: workspaceQueryKeys.detail(workspaceId),
      });
      await queryClient.invalidateQueries({
        queryKey: workspaceQueryKeys.all,
      });
    };

    const updateProfile = useMutation({
      mutationFn: (input: UpdateWorkspaceProfileInput) =>
        service.updateProfile(workspaceId, input),
      retry: false,
      onSettled: refreshWorkspace,
    });

    const updateSettings = useMutation({
      mutationFn: ({
        settings,
        expectedVersion,
      }: {
        settings: WorkspaceSettings;
        expectedVersion: number;
      }) => service.updateSettings(workspaceId, settings, expectedVersion),
      retry: false,
      onSettled: async () => {
        await queryClient.invalidateQueries({
          queryKey: workspaceQueryKeys.settings(workspaceId),
        });
        await refreshWorkspace();
      },
    });

    const archive = useMutation({
      mutationFn: (expectedVersion: number) =>
        service.archive(workspaceId, expectedVersion),
      retry: false,
      onSettled: () =>
        queryClient.invalidateQueries({ queryKey: workspaceQueryKeys.all }),
    });

    return { updateProfile, updateSettings, archive };
  };
}
