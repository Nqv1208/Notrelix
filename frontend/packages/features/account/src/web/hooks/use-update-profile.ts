import { useMutation, useQueryClient } from '@tanstack/react-query';
import { createAccountService, type AccountApiClient, type AccountEndpoints } from '../../core/api/account.service';
import { accountQueryKeys } from '../../core/query/keys';
import type { UserProfile } from '../../core/types/account';

interface UseUpdateProfileDeps {
  api: AccountApiClient;
  endpoints: AccountEndpoints;
  options?: { mockMode?: boolean };
}

export function createUseUpdateProfile({ api, endpoints, options }: UseUpdateProfileDeps) {
  const service = createAccountService(api, endpoints, options);

  return function useUpdateProfile() {
    const queryClient = useQueryClient();

    return useMutation({
      mutationFn: (profile: Partial<UserProfile>) => service.updateProfile(profile),
      onSuccess: () => {
        queryClient.invalidateQueries({ queryKey: accountQueryKeys.profile });
      },
    });
  };
}
