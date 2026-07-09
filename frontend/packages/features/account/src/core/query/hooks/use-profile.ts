import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { createAccountService, type AccountApiClient, type AccountEndpoints } from '../../api/account.service';
import { accountQueryKeys } from '../keys';
import type { UserProfile } from '../../types/account';

interface UseProfileDeps {
  api: AccountApiClient;
  endpoints: AccountEndpoints;
}

export function createUseProfile({ api, endpoints }: UseProfileDeps) {
  const service = createAccountService(api, endpoints);

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
