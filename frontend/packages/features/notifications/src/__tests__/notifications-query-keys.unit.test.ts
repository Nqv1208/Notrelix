import { describe, expect, it } from "vitest";
import { assertNotrelixQueryKey } from "@notrelix/query";
import { notificationsQueryKeys } from "../query/keys";

describe("notificationsQueryKeys Q7 purity tests", () => {
  it("every notificationsQueryKey satisfies assertNotrelixQueryKey", () => {
    const keys = [
      notificationsQueryKeys.all("acc-1"),
      notificationsQueryKeys.unreadCount("acc-1"),
      notificationsQueryKeys.preferences("acc-1"),
    ];

    for (const key of keys) {
      expect(() => assertNotrelixQueryKey(key)).not.toThrow();
      expect(key[0]).toBe("account");
      expect(key[1]).toBe("acc-1");
      expect(key[2]).toBe("notifications");
    }
  });
});
