import { useAccountId } from "@notrelix/query";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import {
  createAccountService,
  type AccountApiClient,
  type AccountEndpoints,
} from "../../../core/api/account.service";
import type { UserProfile } from "../../../core/types/account";
import { accountQueryKeys } from "../../../query/keys";

interface UseProfilePhotoDeps {
  api: AccountApiClient;
  endpoints: AccountEndpoints;
}

/**
 * Sends or removes the profile photo. The producer answers the new address
 * (`null` when removed); it is written into the profile cache at once, and
 * `onChanged` lets the host refresh the other places that show the person
 * (the signed-in session).
 */
export function createUseProfilePhoto({ api, endpoints }: UseProfilePhotoDeps) {
  const service = createAccountService(api, endpoints);

  return function useProfilePhoto(options?: {
    onChanged?: (avatarUrl: string | null) => void;
  }) {
    const accountId = useAccountId();
    const queryClient = useQueryClient();
    const key = accountQueryKeys.profile(accountId);

    const apply = async (avatarUrl: string | null) => {
      queryClient.setQueryData<UserProfile>(key, (profile) =>
        profile ? { ...profile, avatarUrl } : profile,
      );
      options?.onChanged?.(avatarUrl);
      await queryClient.invalidateQueries({ queryKey: key });
    };

    const upload = useMutation({
      mutationFn: (file: File) => service.uploadAvatar(file),
      onSuccess: apply,
    });
    const remove = useMutation({
      mutationFn: () => service.removeAvatar(),
      onSuccess: () => apply(null),
    });

    return {
      upload: upload.mutateAsync,
      remove: remove.mutateAsync,
      isBusy: upload.isPending || remove.isPending,
    };
  };
}
