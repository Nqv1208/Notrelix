import { useAccountId } from "@notrelix/query";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import {
  createAccountService,
  type AccountApiClient,
  type AccountEndpoints,
} from "../../../core/api/account.service";
import { accountQueryKeys } from "../../../query/keys";
import type { UserPreferences } from "../../../core/types/account";

interface UsePreferencesDeps {
  api: AccountApiClient;
  endpoints: AccountEndpoints;
}

export function createUsePreferences({ api, endpoints }: UsePreferencesDeps) {
  const service = createAccountService(api, endpoints);

  return function usePreferences() {
    const accountId = useAccountId();
    const queryClient = useQueryClient();

    const query = useQuery({
      queryKey: accountQueryKeys.preferences(accountId),
      queryFn: () => service.getPreferences(),
    });

    const mutation = useMutation({
      mutationFn: (prefs: Partial<UserPreferences>) =>
        service.updatePreferences(prefs),
      onSuccess: () => {
        queryClient.invalidateQueries({
          queryKey: accountQueryKeys.preferences(accountId),
        });
      },
    });

    return {
      preferences: query.data,
      isLoading: query.isLoading,
      isError: query.isError,
      refetch: query.refetch,
      updatePreferences: mutation.mutate,
      isUpdating: mutation.isPending,
    };
  };
}
