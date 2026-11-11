import { describe, expect, it } from "vitest";
import { endpoints } from "../endpoints";

describe("chat endpoint paths", () => {
  it("encodes every id so an address-bar value cannot leave its path segment", () => {
    const hostile = "../../x?y=1#z";
    const encoded = encodeURIComponent(hostile);
    const chat = endpoints.chat;
    const paths = [
      chat.channel("w", hostile),
      chat.channelArchive("w", hostile),
      chat.channelMembers("w", hostile),
      chat.channelJoin("w", hostile),
      chat.channelMember("w", "c", hostile),
      chat.channelGroup("w", hostile),
      chat.messages("w", hostile),
      chat.message("w", hostile),
      chat.markRead("w", hostile),
      chat.notificationLevel("w", hostile),
    ];
    for (const path of paths) {
      expect(path).toContain(encoded);
      expect(path).not.toContain("../");
      expect(path).not.toContain("?");
      expect(path).not.toContain("#");
    }
  });

  it("keeps a plain id as it is", () => {
    expect(endpoints.chat.channel("w-1", "c_2")).toBe(
      "/workspaces/w-1/chat/channels/c_2",
    );
  });
});
