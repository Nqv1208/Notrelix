import { accountQueryKey } from "@notrelix/query";

export const accountQueryKeys = {
  all: accountQueryKey("account"),
  profile: accountQueryKey("account", "profile"),
  preferences: accountQueryKey("account", "preferences"),
  security: accountQueryKey("account", "security"),
} as const;
