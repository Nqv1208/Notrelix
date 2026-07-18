import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { createAccountService, type AccountApiClient, type AccountEndpoints } from '~/core/api/account.service';
import { accountQueryKeys } from '../keys';
import type { UserPreferences } from '~/core/types/account';

interface UsePreferencesDeps {
  api: AccountApiClient;
  endpoints: AccountEndpoints;
  options?: {
    mockMode?: boolean;
  };

}

export function createUsePreferences({ api, endpoints, options }: UsePreferencesDeps) {
  const service = createAccountService(api, endpoints, options);

  return function usePreferences() {
    const queryClient = useQueryClient();

    const query = useQuery({
      queryKey: accountQueryKeys.preferences,
      queryFn: () => service.getPreferences(),
    });

    const mutation = useMutation({
      mutationFn: (prefs: Partial<UserPreferences>) => service.updatePreferences(prefs),
      onSuccess: () => {
        queryClient.invalidateQueries({ queryKey: accountQueryKeys.preferences });
      },
    });

    return {
      preferences: query.data,
      isLoading: query.isLoading,
      isError: query.isError,
      updatePreferences: mutation.mutate,
      isUpdating: mutation.isPending,
    };
  };
}
