import { describe, it, expect } from "vitest";
import { runDigest } from "../contexts/chat";
import {
  SPACES,
  USERS,
  WORKSPACE_ID,
  actAs,
  chat,
  key,
  storeAs,
} from "./chat-test-kit";

const DAY = 86_400;
type Store = ReturnType<typeof storeAs>;

/** The owner turns the digest e-mail on for the given persona's own matrix. */
function digestOn(store: Store, userId: string, on = true) {
  store.accountPages.notificationsOf(userId, WORKSPACE_ID).rows.digest.email =
    on;
}

async function say(store: Store, label: string, content: string) {
  const res = await chat(store, "POST", `/spaces/${SPACES.general}/messages`, {
    idempotencyKey: key(label),
    body: { content, clientMessageId: `cm-${label}` },
  });
  expect(res.status).toBe(201);
  return res.body;
}

/** Owner mentions the member `n` times in #general (each a notification for the member). */
async function mentionMember(store: Store, count: number) {
  actAs(store, "owner");
  for (let i = 0; i < count; i += 1) {
    const res = await chat(
      store,
      "POST",
      `/spaces/${SPACES.general}/messages`,
      {
        idempotencyKey: key(`dg-${i}-${store.chat.virtualSeconds()}`),
        body: {
          content: `@Member look at item ${i}`,
          clientMessageId: `cm-dg-${i}-${store.chat.virtualSeconds()}`,
          mentions: [
            { kind: "user", start: 0, length: 7, userId: USERS.member },
          ],
        },
      },
    );
    expect(res.status).toBe(201);
  }
}

function completeWindow(store: Store) {
  store.chat.advanceVirtualSeconds(DAY);
}

describe("MFB-CHAT-S3.5: Chat mock digest (UC-CHT-H09)", () => {
  it("queues ONE digest per person and window for the missed mentions, and a second pass adds nothing", async () => {
    const store = storeAs("owner");
    digestOn(store, USERS.member);
    await mentionMember(store, 3);
    completeWindow(store);

    const first = runDigest(store.chat, WORKSPACE_ID);
    const second = runDigest(store.chat, WORKSPACE_ID);

    expect(first.queued).toBe(1);
    expect(second.queued).toBe(0);
    expect(store.chat.digestOutbox).toHaveLength(1);
    const mail = store.chat.digestOutbox[0]!;
    expect(mail.userId).toBe(USERS.member);
    expect(mail.subject).toBe("You missed 3 chat messages");
    expect(mail.windowKey).toMatch(/^\d{12}$/);
    expect(mail.lines).toHaveLength(3);
    expect(mail.lines[0]!.path).toBe(
      `/workspaces/${WORKSPACE_ID}/chat/${SPACES.general}`,
    );
  });

  it("sends nothing while the digest is off (the default), and nothing before the window completes", async () => {
    const store = storeAs("owner");
    await mentionMember(store, 2);
    expect(runDigest(store.chat, WORKSPACE_ID).queued).toBe(0);
    digestOn(store, USERS.member);
    // The items are still in the running window.
    expect(runDigest(store.chat, WORKSPACE_ID).queued).toBe(0);
    completeWindow(store);
    expect(runDigest(store.chat, WORKSPACE_ID).queued).toBe(1);
  });

  it("leaves out what was read, a muted space and a space the person left", async () => {
    const store = storeAs("owner");
    digestOn(store, USERS.member);
    await mentionMember(store, 2);
    completeWindow(store);

    const membership = store.chat.activeMembershipOf(
      SPACES.general,
      USERS.member,
    )!;
    membership.readSequence = store.chat.spaces.get(
      SPACES.general,
    )!.lastSequence;
    expect(runDigest(store.chat, WORKSPACE_ID).queued).toBe(0);

    membership.readSequence = 0;
    membership.notificationLevel = "none";
    expect(runDigest(store.chat, WORKSPACE_ID).queued).toBe(0);

    membership.notificationLevel = "all";
    membership.leftAt = store.getClock().isoNow();
    expect(runDigest(store.chat, WORKSPACE_ID).queued).toBe(0);
    expect(store.chat.digestOutbox).toHaveLength(0);
  });

  it("caps the lines and says how many more, and never mails the author", async () => {
    const store = storeAs("owner");
    digestOn(store, USERS.member);
    digestOn(store, USERS.owner);
    await mentionMember(store, 4);
    completeWindow(store);

    runDigest(store.chat, WORKSPACE_ID, { maxItems: 2 });

    expect(store.chat.digestOutbox).toHaveLength(1);
    const mail = store.chat.digestOutbox[0]!;
    expect(mail.userId).toBe(USERS.member);
    expect(mail.lines).toHaveLength(2);
    expect(mail.more).toBe(2);
  });

  it("gives a Guest none", async () => {
    const store = storeAs("owner");
    digestOn(store, USERS.member);
    await mentionMember(store, 1);
    completeWindow(store);
    const row = store
      .getWorkspaceMembers(WORKSPACE_ID)
      .find((m) => m.userId === USERS.member)!;
    row.role = "guest";
    expect(runDigest(store.chat, WORKSPACE_ID).queued).toBe(0);
  });

  it("is the same after a say that does not mention anyone: no digest without a notification", async () => {
    const store = storeAs("owner");
    digestOn(store, USERS.member);
    await say(store, "plain", "no mention here");
    completeWindow(store);
    expect(runDigest(store.chat, WORKSPACE_ID).queued).toBe(0);
  });
});
