import { useAccountId } from "@notrelix/query";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import {
  createAccountService,
  type AccountApiClient,
  type AccountEndpoints,
} from "../../../core/api/account.service";
import { accountQueryKeys } from "../../../query/keys";
import type {
  AccountPreferences,
  AccountPreferencesPatch,
} from "../../../core/types/account";

interface UsePreferencesDeps {
  api: AccountApiClient;
  endpoints: AccountEndpoints;
}

/** A save that lost the race to another device: the version it quoted is old. */
export function isPreferencesConflict(error: unknown): boolean {
  return (
    typeof error === "object" &&
    error !== null &&
    (error as { status?: unknown }).status === 412
  );
}

/**
 * The person's own preferences. A save quotes the version it was read at
 * (`expectedVersion`, 0 before the first save); a 412 refetches the
 * authoritative values and rethrows so the page can say it did not save.
 */
export function createUsePreferences({ api, endpoints }: UsePreferencesDeps) {
  const service = createAccountService(api, endpoints);

  return function usePreferences() {
    const accountId = useAccountId();
    const queryClient = useQueryClient();
    const key = accountQueryKeys.preferences(accountId);

    const query = useQuery({
      queryKey: key,
      queryFn: () => service.getPreferences(),
      retry: false,
    });

    const mutation = useMutation({
      mutationFn: (patch: AccountPreferencesPatch) => {
        const current = queryClient.getQueryData<AccountPreferences>(key);
        return service.updatePreferences(patch, current?.version ?? 0);
      },
      onSuccess: (saved) => queryClient.setQueryData(key, saved),
      onError: (error) => {
        if (isPreferencesConflict(error))
          void queryClient.invalidateQueries({ queryKey: key });
      },
    });

    return {
      preferences: query.data,
      isLoading: query.isLoading,
      isError: query.isError,
      refetch: query.refetch,
      save: mutation.mutateAsync,
      isSaving: mutation.isPending,
    };
  };
}
