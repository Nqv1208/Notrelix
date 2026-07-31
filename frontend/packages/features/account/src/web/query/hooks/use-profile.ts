import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { createAccountService, type AccountApiClient, type AccountEndpoints } from '../../../core/api/account.service';
import { accountQueryKeys } from '../../../core/query/keys';
import type { UserProfile } from '../../../core/types/account';

interface UseProfileDeps {
  api: AccountApiClient;
  endpoints: AccountEndpoints;
  options?: {
    mockMode?: boolean;
  };

}

export function createUseProfile({ api, endpoints, options }: UseProfileDeps) {
  const service = createAccountService(api, endpoints, options);

  return function useProfile() {
    const queryClient = useQueryClient();

    const query = useQuery({
      queryKey: accountQueryKeys.profile,
      queryFn: () => service.getProfile(),
    });

    const mutation = useMutation({
      mutationFn: (profile: Partial<UserProfile>) => service.updateProfile(profile),
      onSuccess: () => {
        queryClient.invalidateQueries({ queryKey: accountQueryKeys.profile });
      },
    });

    return {
      profile: query.data,
      isLoading: query.isLoading,
      isError: query.isError,
      updateProfile: mutation.mutate,
      isUpdating: mutation.isPending,
    };
  };
}
