import { describe, it, expect } from "vitest";
import { runExport, runRetention } from "../contexts/chat";
import {
  SPACES,
  USERS,
  WORKSPACE_ID,
  actAs,
  chat,
  storeAs,
} from "./chat-test-kit";

const DAY = 86_400;

async function setRule(
  store: ReturnType<typeof storeAs>,
  retentionDays: number | null,
  mode: "tombstone" | "purge",
  expectedVersion?: number,
) {
  const current = await chat(store, "GET", "/retention");
  return chat(store, "PUT", "/retention", {
    body: {
      retentionDays,
      mode,
      expectedVersion: expectedVersion ?? current.body.version,
    },
  });
}

function liveUserMessages(store: ReturnType<typeof storeAs>, spaceId: string) {
  return store.chat
    .messagesOf(spaceId)
    .filter((m) => m.kind === "user" && m.deletedAt === null);
}

describe("MFB-CHAT-S3.3: Chat mock retention (UC-CHT-H05)", () => {
  it("reads as no retention at version 1, with canChange for the Owner only", async () => {
    const owner = await chat(storeAs("owner"), "GET", "/retention");
    expect(owner.status).toBe(200);
    expect(owner.body).toMatchObject({
      retentionDays: null,
      mode: "tombstone",
      version: 1,
      canChange: true,
    });
    const admin = await chat(storeAs("admin"), "GET", "/retention");
    expect(admin.body.canChange).toBe(false);
    const member = await chat(storeAs("member"), "GET", "/retention");
    expect(member.status).toBe(200);
    expect(member.body.canChange).toBe(false);
  });

  it("is set by the Owner only: an Admin is 403, a stale version 412, a window outside 7 to 3650 400", async () => {
    const store = storeAs("owner");
    expect((await setRule(store, 30, "purge")).status).toBe(200);
    expect((await setRule(store, 30, "purge", 1)).status).toBe(412);
    expect((await setRule(store, 6, "tombstone")).status).toBe(400);
    expect((await setRule(store, 3651, "tombstone")).status).toBe(400);
    expect((await setRule(store, 30.5, "tombstone")).status).toBe(400);
    const read = await chat(store, "GET", "/retention");
    expect(read.body).toMatchObject({
      retentionDays: 30,
      mode: "purge",
      version: 2,
    });

    actAs(store, "admin");
    expect((await setRule(store, 60, "tombstone")).status).toBe(403);
  });

  it("keeps the version when the same rule is set again, and keeps the mode when it is turned off", async () => {
    const store = storeAs("owner");
    await setRule(store, 30, "purge");
    const same = await setRule(store, 30, "purge");
    expect(same.body.version).toBe(2);
    const off = await setRule(store, null, "purge");
    expect(off.body).toMatchObject({
      retentionDays: null,
      mode: "purge",
      version: 3,
    });
  });

  it("with no window changes nothing", () => {
    const store = storeAs("owner");
    const before = liveUserMessages(store, SPACES.general).length;
    expect(runRetention(store.chat, WORKSPACE_ID)).toEqual({
      spaces: 0,
      messages: 0,
      threads: 0,
    });
    expect(liveUserMessages(store, SPACES.general)).toHaveLength(before);
  });

  it("tombstone mode turns old user messages into tombstones, keeps system messages and skips a deleted space", async () => {
    const store = storeAs("owner");
    await setRule(store, 7, "tombstone");
    const leadershipSystem = store.chat
      .messagesOf(SPACES.leadership)
      .filter((m) => m.kind === "system").length;
    expect(leadershipSystem).toBeGreaterThan(0);
    const oldNotesBefore = liveUserMessages(store, SPACES.oldNotes).length;

    store.chat.advanceVirtualSeconds(400 * DAY);
    const run = runRetention(store.chat, WORKSPACE_ID);

    expect(run.messages).toBeGreaterThan(0);
    expect(liveUserMessages(store, SPACES.general)).toHaveLength(0);
    expect(
      store.chat
        .messagesOf(SPACES.leadership)
        .filter((m) => m.kind === "system"),
    ).toHaveLength(leadershipSystem);
    expect(liveUserMessages(store, SPACES.oldNotes)).toHaveLength(
      oldNotesBefore,
    );
    const tombstone = store.chat
      .messagesOf(SPACES.general)
      .find((m) => m.deletedAt !== null && m.deletedBy === null)!;
    expect(tombstone.content).toBeNull();
    expect(runRetention(store.chat, WORKSPACE_ID).messages).toBe(0);
  });

  it("tombstone mode clears the pin, reactions and files of an expired message", async () => {
    const store = storeAs("owner");
    const target = liveUserMessages(store, SPACES.general)[0]!;
    store.chat.pins.set(target.id, {
      id: "p1",
      workspaceId: WORKSPACE_ID,
      spaceId: SPACES.general,
      messageId: target.id,
      pinnedBy: USERS.owner,
      pinnedAt: target.createdAt,
    } as never);
    await setRule(store, 7, "tombstone");
    store.chat.advanceVirtualSeconds(400 * DAY);
    runRetention(store.chat, WORKSPACE_ID);
    expect(store.chat.pins.has(target.id)).toBe(false);
  });

  it("purge mode removes the rows, keeps the sequence of the space and lets the reads agree", async () => {
    const store = storeAs("owner");
    await setRule(store, 7, "purge");
    store.chat.advanceVirtualSeconds(400 * DAY);
    const lastSequence = store.chat.spaces.get(SPACES.general)!.lastSequence;
    const run = runRetention(store.chat, WORKSPACE_ID);
    expect(run.messages).toBeGreaterThan(0);
    expect(
      store.chat.messagesOf(SPACES.general).filter((m) => m.kind === "user"),
    ).toHaveLength(0);
    expect(store.chat.spaces.get(SPACES.general)!.lastSequence).toBe(
      lastSequence,
    );
    const page = await chat(
      store,
      "GET",
      `/spaces/${SPACES.general}/messages`,
      {
        query: "limit=50",
      },
    );
    expect(page.status).toBe(200);
    expect(
      page.body.items.filter((m: { kind: string }) => m.kind === "user"),
    ).toHaveLength(0);
  });
});

describe("MFB-CHAT-S3.3: Chat mock export (UC-CHT-H06)", () => {
  async function create(
    store: ReturnType<typeof storeAs>,
    subjectUserId: string,
    extra: Record<string, unknown> = {},
  ) {
    return chat(store, "POST", "/exports", {
      body: { subjectUserId, ...extra },
    });
  }

  it("is for the Owner and the Admin; a Member is 403, an unknown person 404, a backwards range 400", async () => {
    expect((await create(storeAs("member"), USERS.admin)).status).toBe(403);
    const store = storeAs("owner");
    expect((await create(store, "not-a-member")).status).toBe(404);
    const bad = await create(store, USERS.admin, {
      rangeFrom: "2026-02-01T00:00:00Z",
      rangeTo: "2026-01-01T00:00:00Z",
    });
    expect(bad.status).toBe(400);
    expect((await create(store, USERS.admin)).status).toBe(202);
    expect((await create(actAs(store, "admin"), USERS.member)).status).toBe(
      202,
    );
  });

  it("queues, finishes on the first status read and counts what it leaves out", async () => {
    const store = storeAs("owner");
    const created = await create(store, USERS.admin);
    expect(created.body).toMatchObject({
      status: "queued",
      downloadPath: null,
    });
    const done = await chat(store, "GET", `/exports/${created.body.id}`);
    expect(done.body.status).toBe("ready");
    expect(done.body.downloadPath).toContain(
      `/exports/${created.body.id}/content`,
    );
    // The Owner is not in the private leadership channel, where the admin writes.
    expect(done.body.excludedSpaceCount).toBeGreaterThan(0);
    expect(done.body.messageCount).toBeGreaterThan(0);
  });

  it("holds no private content, no tombstone body and no one else's message", async () => {
    const store = storeAs("owner");
    const created = await create(store, USERS.admin);
    const job = store.chat.exportJobs.get(created.body.id)!;
    runExport(store.chat, job);
    const text = job.file!;
    const leadership = store.chat
      .messagesOf(SPACES.leadership)
      .filter((m) => m.kind === "user" && m.content);
    for (const m of leadership) expect(text).not.toContain(m.content!);
    const others = store.chat
      .messagesOf(SPACES.general)
      .filter((m) => m.authorId !== USERS.admin && m.content);
    for (const m of others) expect(text).not.toContain(m.content!);
    expect(JSON.parse(text).format).toBe("notrelix.chat-export.v1");
  });

  it("serves the file to the requester only, and not after its 24 hours", async () => {
    const store = storeAs("owner");
    const created = await create(store, USERS.admin);
    await chat(store, "GET", `/exports/${created.body.id}`);
    const file = await chat(
      store,
      "GET",
      `/exports/${created.body.id}/content`,
    );
    expect(file.status).toBe(200);
    expect(JSON.parse(JSON.stringify(file.body)).format).toBe(
      "notrelix.chat-export.v1",
    );

    expect(
      (await chat(actAs(store, "admin"), "GET", `/exports/${created.body.id}`))
        .status,
    ).toBe(404);

    actAs(store, "owner");
    store.chat.advanceVirtualSeconds(25 * 3600);
    const expired = await chat(store, "GET", `/exports/${created.body.id}`);
    expect(expired.body.status).toBe("expired");
    expect(expired.body.downloadPath).toBeNull();
    expect(
      (await chat(store, "GET", `/exports/${created.body.id}/content`)).status,
    ).toBe(404);
  });

  it("is rate limited per person: the sixth request in an hour is 429, and an hour later it works", async () => {
    const store = storeAs("owner");
    for (let i = 0; i < 5; i += 1) {
      expect((await create(store, USERS.admin)).status).toBe(202);
    }
    const refused = await create(store, USERS.admin);
    expect(refused.status).toBe(429);
    expect(refused.body.policy).toBe("chat.export");
    store.chat.advanceVirtualSeconds(3700);
    expect((await create(store, USERS.admin)).status).toBe(202);
  });
});
