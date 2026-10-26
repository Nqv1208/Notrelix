import { describe, it, expect } from "vitest";
import { notificationsQueryKeys } from "../query/keys";

describe("notificationsQueryKeys", () => {
  it("should generate notifications root query key", () => {
    expect(notificationsQueryKeys.all("acc-1")).toEqual([
      "account",
      "acc-1",
      "notifications",
    ]);
  });

  it("should generate unread count query key", () => {
    expect(notificationsQueryKeys.unreadCount("acc-1")).toEqual([
      "account",
      "acc-1",
      "notifications",
      "unread-count",
    ]);
  });

  it("should generate preferences query key", () => {
    expect(notificationsQueryKeys.preferences("ws-1")).toEqual([
      "workspace",
      "ws-1",
      "notification-preferences",
    ]);
  });
});
