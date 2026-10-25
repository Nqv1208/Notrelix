import { useAccountId } from "@notrelix/query";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  createAccountService,
  type AccountApiClient,
  type AccountEndpoints,
} from "../../../core/api/account.service";
import type { AccountWorkspaceEntry } from "../../../core/types/account";
import { accountQueryKeys } from "../../../query/keys";

interface UseAccountWorkspacesDeps {
  api: AccountApiClient;
  endpoints: AccountEndpoints;
}

/**
 * The person's own workspaces with the way to choose the default one and to
 * leave one. The producer owns every fact: after a change the list is read
 * again. Leaving answers the list that remains, so the host can decide where
 * to go next; clearing the left workspace's scoped data and navigating are the
 * host's (workspace transition).
 */
export function createUseAccountWorkspaces({
  api,
  endpoints,
}: UseAccountWorkspacesDeps) {
  const service = createAccountService(api, endpoints);

  return function useAccountWorkspaces() {
    const accountId = useAccountId();
    const queryClient = useQueryClient();
    const key = accountQueryKeys.workspaces(accountId);

    const query = useQuery({
      queryKey: key,
      queryFn: () => service.listMyWorkspaces(),
      retry: false,
    });

    const setDefault = useMutation({
      mutationFn: (workspaceId: string) =>
        service.setDefaultWorkspace(workspaceId),
      onSuccess: async () => {
        // The default also bumps the profile version the preferences quote.
        await Promise.all([
          queryClient.invalidateQueries({ queryKey: key }),
          queryClient.invalidateQueries({
            queryKey: accountQueryKeys.profile(accountId),
          }),
          queryClient.invalidateQueries({
            queryKey: accountQueryKeys.preferences(accountId),
          }),
        ]);
      },
    });

    const leave = useMutation({
      mutationFn: async (
        workspaceId: string,
      ): Promise<AccountWorkspaceEntry[]> => {
        await service.leaveWorkspace(workspaceId);
        return queryClient.fetchQuery({
          queryKey: key,
          queryFn: () => service.listMyWorkspaces(),
          staleTime: 0,
        });
      },
    });

    return {
      workspaces: query.data,
      isError: query.isError,
      refetch: query.refetch,
      setDefault: setDefault.mutateAsync,
      leave: leave.mutateAsync,
      isBusy: setDefault.isPending || leave.isPending,
    };
  };
}
