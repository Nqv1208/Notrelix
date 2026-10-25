import { useAccountId } from "@notrelix/query";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import {
  createAccountService,
  type AccountApiClient,
  type AccountEndpoints,
} from "../../core/api/account.service";
import { accountQueryKeys } from "../../query/keys";

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
    const accountId = useAccountId();
    const queryClient = useQueryClient();

    return useMutation({
      mutationFn: (profile: { name?: string }) =>
        service.updateProfile(profile),
      onSuccess: () => {
        queryClient.invalidateQueries({
          queryKey: accountQueryKeys.profile(accountId),
        });
      },
    });
  };
}
