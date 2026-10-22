import { describe, expect, it } from "vitest";
import { assertNotrelixQueryKey } from "@notrelix/query";
import { accountQueryKeys } from "../query/keys";

describe("accountQueryKeys Q7 purity tests", () => {
  it("every accountQueryKey satisfies assertNotrelixQueryKey", () => {
    const keys = [
      accountQueryKeys.all("acc-1"),
      accountQueryKeys.profile("acc-1"),
      accountQueryKeys.preferences("acc-1"),
      accountQueryKeys.security("acc-1"),
    ];

    for (const key of keys) {
      expect(() => assertNotrelixQueryKey(key)).not.toThrow();
      expect(key[0]).toBe("account");
      expect(key[1]).toBe("acc-1");
      expect(key[2]).toBe("account");
    }
  });
});
