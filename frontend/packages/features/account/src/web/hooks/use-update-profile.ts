import { useMutation, useQueryClient } from "@tanstack/react-query";
import {
  createAccountService,
  type AccountApiClient,
  type AccountEndpoints,
} from "../../core/api/account.service";
import { accountQueryKeys } from "../../query/keys";
import type { UserProfile } from "../../core/types/account";

interface UseUpdateProfileDeps {
  api: AccountApiClient;
  endpoints: AccountEndpoints;
}

export function createUseUpdateProfile({
  api,
  endpoints,
}: UseUpdateProfileDeps) {
  const service = createAccountService(api, endpoints);

  return function useUpdateProfile() {
    const queryClient = useQueryClient();

    return useMutation({
      mutationFn: (profile: Partial<UserProfile>) =>
        service.updateProfile(profile),
      onSuccess: () => {
        queryClient.invalidateQueries({ queryKey: accountQueryKeys.profile });
      },
    });
  };
}
