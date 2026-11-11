import { describe, expect, it } from "vitest";
import { getActiveChannelIdFromPathname } from "../active-channel";

describe("getActiveChannelIdFromPathname", () => {
  it("reads the channel of the Chat route", () => {
    expect(
      getActiveChannelIdFromPathname("/workspaces/ws-a/chat/space-1"),
    ).toBe("space-1");
    expect(
      getActiveChannelIdFromPathname("/workspaces/ws-a/chat/space-1?m=4"),
    ).toBe("space-1");
  });

  it("reads the conversation of the direct message route", () => {
    expect(
      getActiveChannelIdFromPathname("/workspaces/ws-a/chat/dm/conv-1"),
    ).toBe("conv-1");
    expect(
      getActiveChannelIdFromPathname("/workspaces/ws-a/chat/dm/conv%201?m=2"),
    ).toBe("conv 1");
    expect(
      getActiveChannelIdFromPathname("/workspaces/ws-a/chat/dm"),
    ).toBeNull();
  });

  it("is null outside a channel (the bare Chat page, other pages)", () => {
    expect(getActiveChannelIdFromPathname("/workspaces/ws-a/chat")).toBeNull();
    expect(
      getActiveChannelIdFromPathname("/workspaces/ws-a/boards/b-1"),
    ).toBeNull();
  });
});
