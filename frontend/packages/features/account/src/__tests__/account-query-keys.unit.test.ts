import { describe, expect, it } from "vitest";
import { assertNotrelixQueryKey } from "@notrelix/query";
import { accountQueryKeys } from "../query/keys";

describe("accountQueryKeys Q7 purity tests", () => {
  it("every accountQueryKey satisfies assertNotrelixQueryKey", () => {
    const keys = [
      accountQueryKeys.all,
      accountQueryKeys.profile,
      accountQueryKeys.preferences,
      accountQueryKeys.security,
    ];

    for (const key of keys) {
      expect(() => assertNotrelixQueryKey(key)).not.toThrow();
      expect(key[0]).toBe("account");
      expect(key[1]).toBe("account");
    }
  });
});
