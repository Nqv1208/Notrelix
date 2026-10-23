import {
  createContext,
  createElement,
  useContext,
  type ReactNode,
} from "react";
import type { QueryClient } from "@tanstack/react-query";

/**
 * Active Account scope for the client cache.
 *
 * TEMPORARY MODEL: Account equals the authenticated principal (`User.id`)
 * until the backend exposes an Account ID and an account-switch signal.
 * The host feeds this provider from its auth state; account-scoped hooks read
 * the ID with `useAccountId()`, which fails loudly when no Account is active.
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
}
