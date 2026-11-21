import { useAccountId } from "@notrelix/query";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { useCallback, useState } from "react";
import {
  createAccountService,
  type AccountApiClient,
  type AccountEndpoints,
} from "../../../core/api/account.service";
import type { AccountApiToken } from "../../../core/types/account";
import { accountQueryKeys } from "../../../query/keys";

interface UseApiTokensDeps {
  api: AccountApiClient;
  endpoints: AccountEndpoints;
}

/**
 * The API tokens of one workspace: the first page is a query, the later pages
 * are fetched on demand and kept beside it. Never holds a secret.
 */
export function createUseApiTokens({ api, endpoints }: UseApiTokensDeps) {
  const service = createAccountService(api, endpoints);

  return function useApiTokens(workspaceId: string) {
    const accountId = useAccountId();
    const queryClient = useQueryClient();
    const [more, setMore] = useState<{
      items: readonly AccountApiToken[];
      next: string | null;
    } | null>(null);
    const [loadingMore, setLoadingMore] = useState(false);
    const first = useQuery({
      queryKey: accountQueryKeys.apiTokens(accountId, workspaceId),
      queryFn: () => service.listApiTokens(workspaceId),
      enabled: workspaceId !== "",
      retry: false,
    });
    const next = more ? more.next : (first.data?.nextCursor ?? null);
    const loadMore = useCallback(async () => {
      if (next === null) return;
      setLoadingMore(true);
      try {
        const page = await service.listApiTokens(workspaceId, next);
        setMore((previous) => ({
          items: [...(previous?.items ?? []), ...page.items],
          next: page.nextCursor,
        }));
      } finally {
        setLoadingMore(false);
      }
    }, [next, workspaceId]);
    return {
      service,
      first,
      tokens: [...(first.data?.items ?? []), ...(more?.items ?? [])],
      hasMore: next !== null,
      loadingMore,
      loadMore,
      /** Refetches the first page after a change and drops the later ones. */
      invalidate: async () => {
        setMore(null);
        await queryClient.invalidateQueries({
          queryKey: accountQueryKeys.apiTokens(accountId, workspaceId),
        });
      },
    };
  };
}
