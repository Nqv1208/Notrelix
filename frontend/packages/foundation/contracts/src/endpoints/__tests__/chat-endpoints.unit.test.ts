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
      chat.threadReplies("w", hostile),
      chat.threadFollow("w", hostile),
      chat.threadRead("w", hostile),
      chat.messagePin("w", hostile),
      chat.spacePins("w", hostile),
      chat.channelPermissions("w", hostile),
      chat.messageModeration("w", hostile),
      chat.messageReaction("w", "m", hostile),
      chat.messageReactors("w", hostile),
      chat.conversationParticipants("w", hostile),
      chat.conversationLeave("w", hostile),
      chat.conversationVisibility("w", hostile),
    ];
    for (const path of paths) {
      expect(path).toContain(encoded);
      expect(path).not.toContain("../");
      expect(path).not.toContain("?");
      expect(path).not.toContain("#");
    }
  });

  it("builds the thread, pin and reaction paths of the Chat REST contract", () => {
    const chat = endpoints.chat;
    expect(chat.threadReplies("w", "r1")).toBe(
      "/workspaces/w/chat/messages/r1/replies",
    );
    expect(chat.threadFollow("w", "t1")).toBe(
      "/workspaces/w/chat/threads/t1/follow",
    );
    expect(chat.threadRead("w", "t1")).toBe(
      "/workspaces/w/chat/threads/t1/read",
    );
    expect(chat.followedThreads("w")).toBe("/workspaces/w/chat/me/threads");
    expect(chat.messagePin("w", "m1")).toBe(
      "/workspaces/w/chat/messages/m1/pin",
    );
    expect(chat.spacePins("w", "s1")).toBe("/workspaces/w/chat/spaces/s1/pins");
    expect(chat.channelPermissions("w", "c1")).toBe(
      "/workspaces/w/chat/channels/c1/permissions",
    );
    expect(chat.messageModeration("w", "m1")).toBe(
      "/workspaces/w/chat/messages/m1/moderation",
    );
    expect(chat.messageReaction("w", "m1", "👍")).toBe(
      `/workspaces/w/chat/messages/m1/reactions/${encodeURIComponent("👍")}`,
    );
    expect(chat.messageReactors("w", "m1")).toBe(
      "/workspaces/w/chat/messages/m1/reactions",
    );
  });

  it("builds the conversation and block paths of the Chat REST contract", () => {
    const chat = endpoints.chat;
    expect(chat.conversations("w")).toBe("/workspaces/w/chat/conversations");
    expect(chat.conversationParticipants("w", "c1")).toBe(
      "/workspaces/w/chat/conversations/c1/participants",
    );
    expect(chat.conversationLeave("w", "c1")).toBe(
      "/workspaces/w/chat/conversations/c1/participants/me",
    );
    expect(chat.conversationVisibility("w", "c1")).toBe(
      "/workspaces/w/chat/conversations/c1/visibility",
    );
    expect(chat.blocks("w")).toBe("/workspaces/w/chat/me/blocks");
    expect(chat.conversations("a/b")).toContain("a%2Fb");
    expect(chat.blocks("a/b")).toContain("a%2Fb");
  });

  it("builds the mentionables path and encodes its ids", () => {
    const chat = endpoints.chat;
    expect(chat.spaceMentionables("w", "s1")).toBe(
      "/workspaces/w/chat/spaces/s1/mentionables",
    );
    expect(chat.spaceMentionables("a/b", "c d")).toBe(
      "/workspaces/a%2Fb/chat/spaces/c%20d/mentionables",
    );
  });

  it("builds the message attachment paths and encodes their ids", () => {
    const chat = endpoints.chat;
    expect(chat.messageAttachments("w", "m1")).toBe(
      "/workspaces/w/chat/messages/m1/attachments",
    );
    expect(chat.messageAttachment("w", "m1", "a1")).toBe(
      "/workspaces/w/chat/messages/m1/attachments/a1",
    );
    expect(chat.messageAttachmentContent("w", "m1", "a1")).toBe(
      "/workspaces/w/chat/messages/m1/attachments/a1/content",
    );
    expect(chat.messageAttachmentContent("a/b", "c d", "e?f")).toBe(
      "/workspaces/a%2Fb/chat/messages/c%20d/attachments/e%3Ff/content",
    );
    expect(chat.messageAttachments("a/b", "c d")).toContain("c%20d");
  });

  it("keeps a plain id as it is", () => {
    expect(endpoints.chat.channel("w-1", "c_2")).toBe(
      "/workspaces/w-1/chat/channels/c_2",
    );
  });
});
