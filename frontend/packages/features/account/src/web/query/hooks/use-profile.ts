import { useAccountId } from "@notrelix/query";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import {
  createAccountService,
  type AccountApiClient,
  type AccountEndpoints,
} from "../../../core/api/account.service";
import { accountQueryKeys } from "../../../query/keys";

interface UseProfileDeps {
  api: AccountApiClient;
  endpoints: AccountEndpoints;
}

export function createUseProfile({ api, endpoints }: UseProfileDeps) {
  const service = createAccountService(api, endpoints);

  return function useProfile() {
    const accountId = useAccountId();
    const queryClient = useQueryClient();

    const query = useQuery({
      queryKey: accountQueryKeys.profile(accountId),
      queryFn: () => service.getProfile(),
    });

    const mutation = useMutation({
      mutationFn: (profile: { name?: string }) =>
        service.updateProfile(profile),
      onSuccess: () => {
        queryClient.invalidateQueries({
          queryKey: accountQueryKeys.profile(accountId),
        });
      },
    });

    return {
      profile: query.data,
      isLoading: query.isLoading,
      isError: query.isError,
      refetch: query.refetch,
      updateProfile: mutation.mutate,
      isUpdating: mutation.isPending,
    };
  };
}
