import { accountQueryKey } from "@notrelix/query";

export const accountQueryKeys = {
  all: (accountId: string) => accountQueryKey(accountId, "account"),
  profile: (accountId: string) =>
    accountQueryKey(accountId, "account", "profile"),
  preferences: (accountId: string) =>
    accountQueryKey(accountId, "account", "preferences"),
  security: (accountId: string) =>
    accountQueryKey(accountId, "account", "security"),
} as const;
