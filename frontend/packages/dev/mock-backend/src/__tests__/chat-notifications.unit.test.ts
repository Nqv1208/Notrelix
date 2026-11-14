import { describe, expect, it } from "vitest";
import { reasonFor } from "../contexts/chat/chat-notifications";
import { createMockFetch } from "../transport/create-mock-fetch";
import {
  SPACES,
  USERS,
  WORKSPACE_ID,
  actAs,
  chat,
  key,
  storeAs,
} from "./chat-test-kit";

type Store = ReturnType<typeof storeAs>;

async function inbox(store: Store) {
  const response = await createMockFetch(store)(
    `http://localhost:8000/api/v1/workspaces/${WORKSPACE_ID}/inbox`,
  );
  const body = JSON.parse(await response.text());
  return (
    body.items as { type: string; target: any; excerpt: string | null }[]
  ).filter((item) => item.type.startsWith("chat."));
}

let counter = 0;
async function send(
  store: Store,
  spaceId: string,
  content: string,
  mentions: unknown[] = [],
) {
  counter += 1;
  return chat(store, "POST", `/spaces/${spaceId}/messages`, {
    idempotencyKey: key(`notify-${counter}`),
    body: { content, clientMessageId: `cid-notify-${counter}`, mentions },
  });
}

describe("reasonFor (ChatNotificationHandoff.ReasonFor)", () => {
  it("gives one reason by priority and respects the level", () => {
    expect(reasonFor("none", true, true, true, true)).toBeNull();
    expect(reasonFor("all", true, true, true, true)).toBe("mention");
    expect(reasonFor("all", false, true, true, true)).toBe("thread-reply");
    expect(reasonFor("all", false, false, true, true)).toBe("direct-message");
    expect(reasonFor("mentions", false, false, true, false)).toBeNull();
    expect(reasonFor("mentions", false, false, true, true)).toBe("broadcast");
    expect(reasonFor("all", false, false, false, false)).toBeNull();
  });
});

describe("MFB-CHAT-S2.8: chat notifications in the mock Inbox (UC-CHT-G09)", () => {
  it("notifies a mentioned member with the channel and a preview, and never the author", async () => {
    const store = storeAs("owner");
    await send(store, SPACES.general, "hi @Member", [
      { kind: "user", start: 3, length: 7, userId: USERS.member },
    ]);

    expect(await inbox(store)).toEqual([]);
    actAs(store, "member");
    const items = await inbox(store);
    expect(items).toHaveLength(1);
    expect(items[0]).toMatchObject({
      type: "chat.mention",
      excerpt: "hi @Member",
      target: { kind: "chat", id: SPACES.general },
    });
  });

  it("gives a plain channel message nothing and a direct message a notification without a channel name", async () => {
    const store = storeAs("owner");
    await send(store, SPACES.general, "just talking");
    actAs(store, "member");
    expect(await inbox(store)).toEqual([]);

    actAs(store, "owner");
    const started = await chat(store, "POST", "/conversations", {
      body: { kind: "direct", userId: USERS.member },
      idempotencyKey: key("notify-direct"),
    });
    await send(store, started.body.conversation.id, "ping");
    actAs(store, "member");
    const items = await inbox(store);
    expect(items).toHaveLength(1);
    expect(items[0]).toMatchObject({
      type: "chat.direct-message",
      target: { kind: "chat", title: "" },
    });
  });

  it("gives nothing for a muted space and stops showing it after the member leaves", async () => {
    const store = storeAs("owner");
    const membership = store.chat.activeMembershipOf(
      SPACES.general,
      USERS.member,
    )!;
    membership.notificationLevel = "none";
    await send(store, SPACES.general, "hi @Member", [
      { kind: "user", start: 3, length: 7, userId: USERS.member },
    ]);
    actAs(store, "member");
    expect(await inbox(store)).toEqual([]);

    membership.notificationLevel = "all";
    actAs(store, "owner");
    await send(store, SPACES.general, "again @Member", [
      { kind: "user", start: 6, length: 7, userId: USERS.member },
    ]);
    actAs(store, "member");
    expect(await inbox(store)).toHaveLength(1);
    membership.leftAt = store.getClock().isoNow();
    expect(await inbox(store)).toEqual([]);
  });
});
