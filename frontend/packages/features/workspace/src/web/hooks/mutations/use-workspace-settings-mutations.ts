import { useAccountId } from "@notrelix/query";
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
 * Workspace profile, defaults, archive and delete writes (WX-SET-001). Each carries
 * the version the member last read; a stale version is rejected by the
 * producer and surfaced as a conflict, never retried automatically.
 */
export function createUseWorkspaceSettingsMutations({
  api,
  endpoints,
}: SettingsMutationDeps) {
  const service = createWorkspaceSettingsService(api, endpoints);

  return function useWorkspaceSettingsMutations(workspaceId: string) {
    const accountId = useAccountId();
    const queryClient = useQueryClient();
    const refreshWorkspace = async () => {
      await queryClient.invalidateQueries({
        queryKey: workspaceQueryKeys.detail(workspaceId),
      });
      await queryClient.invalidateQueries({
        queryKey: workspaceQueryKeys.all(accountId),
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
        queryClient.invalidateQueries({
          queryKey: workspaceQueryKeys.all(accountId),
        }),
    });

    // Deleting cuts the workspace off at once: the lists the switcher and the rail draw lose it before
    // anything refetches. Dropping the workspace's own cached answers is the host's step, after it has left
    // the workspace (a scope transition: removing them first would only refetch a NotFound under the page).
    const remove = useMutation({
      mutationFn: (expectedVersion: number) =>
        service.remove(workspaceId, expectedVersion),
      retry: false,
      onSuccess: async () => {
        queryClient.setQueriesData<readonly { id: string }[]>(
          { queryKey: workspaceQueryKeys.all(accountId) },
          (rows) =>
            Array.isArray(rows)
              ? rows.filter((row) => row.id !== workspaceId)
              : rows,
        );
        await queryClient.invalidateQueries({
          queryKey: workspaceQueryKeys.all(accountId),
        });
      },
    });

    return { updateProfile, updateSettings, archive, remove };
  };
}
