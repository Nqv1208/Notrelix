import { useMutation, useQueryClient } from "@tanstack/react-query";
import {
  createAccountService,
  type AccountApiClient,
  type AccountEndpoints,
} from "../../core/api/account.service";
import { accountQueryKeys } from "../../core/query/keys";
import type { UserPreferences } from "../../core/types/account";

interface UseUpdatePreferencesDeps {
  api: AccountApiClient;
  endpoints: AccountEndpoints;
  options?: { mockMode?: boolean };
}

export function createUseUpdatePreferences({
  api,
  endpoints,
  options,
}: UseUpdatePreferencesDeps) {
  const service = createAccountService(api, endpoints, options);

  return function useUpdatePreferences() {
    const queryClient = useQueryClient();

    return useMutation({
      mutationFn: (prefs: Partial<UserPreferences>) =>
        service.updatePreferences(prefs),
      onSuccess: () => {
        queryClient.invalidateQueries({
          queryKey: accountQueryKeys.preferences,
        });
      },
    });
  };
}
