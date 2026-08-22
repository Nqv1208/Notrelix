import { useMutation, useQueryClient } from "@tanstack/react-query";
import {
  createAccountService,
  type AccountApiClient,
  type AccountEndpoints,
} from "../../core/api/account.service";
import { accountQueryKeys } from "../../query/keys";
import type { UserPreferences } from "../../core/types/account";

interface UseUpdatePreferencesDeps {
  api: AccountApiClient;
  endpoints: AccountEndpoints;
}

export function createUseUpdatePreferences({
  api,
  endpoints,
}: UseUpdatePreferencesDeps) {
  const service = createAccountService(api, endpoints);

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
