import { useAccountId } from "@notrelix/query";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import {
  createAccountService,
  type AccountApiClient,
  type AccountEndpoints,
} from "../../../core/api/account.service";
import { accountQueryKeys } from "../../../query/keys";

interface UseSecurityDeps {
  api: AccountApiClient;
  endpoints: AccountEndpoints;
}

/**
 * The person's sign-in security: the summary, the sessions (every status; the
 * page filters) and the first page of the activity. Account-scoped keys, so a
 * change of account never shows another person's devices.
 */
export function createUseSecurity({ api, endpoints }: UseSecurityDeps) {
  const service = createAccountService(api, endpoints);

  return function useSecurity() {
    const accountId = useAccountId();
    const queryClient = useQueryClient();
    const summary = useQuery({
      queryKey: accountQueryKeys.security(accountId),
      queryFn: () => service.getSecuritySummary(),
      retry: false,
    });
    const sessions = useQuery({
      queryKey: accountQueryKeys.sessions(accountId),
      queryFn: () => service.listSessions(),
      retry: false,
    });
    const activity = useQuery({
      queryKey: accountQueryKeys.activity(accountId),
      queryFn: () => service.listActivity(),
      retry: false,
    });
    return {
      service,
      summary,
      sessions,
      activity,
      /** Refetches everything the security page shows after a change. */
      invalidate: () =>
        Promise.all([
          queryClient.invalidateQueries({
            queryKey: accountQueryKeys.security(accountId),
          }),
          queryClient.invalidateQueries({
            queryKey: accountQueryKeys.sessions(accountId),
          }),
          queryClient.invalidateQueries({
            queryKey: accountQueryKeys.activity(accountId),
          }),
        ]),
    };
  };
}
