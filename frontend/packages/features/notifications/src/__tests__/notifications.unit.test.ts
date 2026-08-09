import { describe, it, expect } from "vitest";
import { notificationsQueryKeys } from "../core/query/keys";

describe("notificationsQueryKeys", () => {
  it("should generate notifications root query key", () => {
    expect(notificationsQueryKeys.all).toEqual(["notifications"]);
  });

  it("should generate unread count query key", () => {
    expect(notificationsQueryKeys.unreadCount).toEqual([
      "notifications",
      "unread-count",
    ]);
  });

  it("should generate preferences query key", () => {
    expect(notificationsQueryKeys.preferences).toEqual([
      "notifications",
      "preferences",
    ]);
  });
});
