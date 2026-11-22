import {
  createContext,
  createElement,
  useContext,
  type ReactNode,
} from "react";
import type { QueryClient } from "@tanstack/react-query";
import { runSessionReset } from "./session-reset";

/**
 * Active Account scope for the client cache.
 *
 * The Account is the signed-in member's own Account: the `accountId` the
 * backend returns in `GET /auth/bootstrap`, which is not the `User.id`. The
 * host resolves it per signed-in person and feeds this provider; account-scoped
 * hooks read the ID with `useAccountId()`, which fails loudly when no Account is
 * active, so the host renders them only once it is known.
 */
const AccountScopeContext = createContext<string | null>(null);

export function AccountScopeProvider({
  accountId,
  children,
}: {
  readonly accountId: string | null;
  readonly children?: ReactNode;
}) {
  return createElement(
    AccountScopeContext.Provider,
    { value: accountId },
    children,
  );
}

export function useAccountId(): string {
  const accountId = useContext(AccountScopeContext);
  if (accountId === null || accountId.length === 0) {
    throw new Error(
      "useAccountId requires an active Account; render under AccountScopeProvider with an authenticated principal",
    );
  }
  return accountId;
}

/**
 * Principal/Account transition boundary (FE-STATE-038/039).
 *
 * Cancels in-flight requests and drops every cached query so the previous
 * principal's data is unreachable. Removed queries discard late responses, and
 * keys are Account-scoped, so a late response cannot populate the new scope.
 */
export function resetCacheForAccountTransition(queryClient: QueryClient): void {
  void queryClient.cancelQueries();
  queryClient.clear();
  runSessionReset();
}
