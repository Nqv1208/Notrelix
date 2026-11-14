import { describe, it, expect } from "vitest";
import { buildSnippet, foldSearchText } from "../contexts/chat/search.handlers";
import {
  SPACES,
  USERS,
  actAs,
  chat,
  key,
  storeAs,
  WORKSPACE_ID,
} from "./chat-test-kit";

type Store = ReturnType<typeof storeAs>;

let counter = 0;
async function say(store: Store, spaceId: string, content: string) {
  counter += 1;
  const res = await chat(store, "POST", `/spaces/${spaceId}/messages`, {
    idempotencyKey: key(`search-${counter}`),
    body: { content, clientMessageId: `cid-search-${counter}` },
  });
  expect(res.status).toBe(201);
  return res.body as { id: string };
}

const search = (store: Store, query: string) =>
  chat(store, "GET", "/search", { query });

const ids = (res: { body: any }) =>
  (res.body.items as { messageId: string }[]).map((item) => item.messageId);

describe("MFB-CHAT-S3.4: Chat mock search (UC-CHT-H07)", () => {
  it("refuses an empty, too short or too long text and a bad limit, cursor or range with 400", async () => {
    const store = storeAs("member");
    for (const query of [
      "q=",
      "q=%20%20",
      "q=a",
      `q=${"a".repeat(101)}`,
      "q=ab&limit=0",
      "q=ab&limit=51",
      "q=ab&cursor=garbage",
      "q=ab&from=2026-02-01T00:00:00Z&to=2026-01-01T00:00:00Z",
    ]) {
      expect((await search(store, query)).status, query).toBe(400);
    }
    expect((await search(store, "q=ab")).status).toBe(200);
  });

  it("ignores case and Vietnamese marks in both directions, and treats the text as a phrase, never a pattern", async () => {
    const store = storeAs("member");
    const a = await say(store, SPACES.general, "Hóa đơn tháng 11 đã duyệt");
    const b = await say(store, SPACES.general, "hoa don thang 12 chua duyet");
    const c = await say(store, SPACES.general, "cost is (a+b)* here");

    expect(ids(await search(store, "q=hoa%20don"))).toEqual(
      expect.arrayContaining([a.id, b.id]),
    );
    expect(
      ids(await search(store, `q=${encodeURIComponent("HÓA ĐƠN")}`)),
    ).toEqual(expect.arrayContaining([a.id, b.id]));
    expect(
      ids(await search(store, `q=${encodeURIComponent("(a+b)*")}`)),
    ).toEqual([c.id]);
    expect((await search(store, "q=.*")).body.items).toEqual([]);
  });

  it("finds a private channel for its members only: an Owner who is not in it finds nothing", async () => {
    const store = storeAs("member");
    const secret = await say(
      store,
      SPACES.leadership,
      "kế hoạch bí mật zzquarter",
    );

    expect(ids(await search(store, "q=zzquarter"))).toEqual([secret.id]);
    expect(ids(await search(actAs(store, "owner"), "q=zzquarter"))).toEqual([]);
    expect(
      ids(
        await search(
          actAs(store, "owner"),
          `q=zzquarter&spaceId=${SPACES.leadership}`,
        ),
      ),
    ).toEqual([]);
    expect(ids(await search(actAs(store, "admin"), "q=zzquarter"))).toEqual([
      secret.id,
    ]);
  });

  it("stops finding a private channel after the member is removed from it", async () => {
    const store = storeAs("member");
    const secret = await say(store, SPACES.leadership, "zzremoved text");
    expect(ids(await search(store, "q=zzremoved"))).toEqual([secret.id]);

    const row = store.chat.membershipOf(SPACES.leadership, USERS.member)!;
    row.leftAt = "2026-01-01T00:00:00.000Z";
    expect(ids(await search(store, "q=zzremoved"))).toEqual([]);
  });

  it("shows a Guest only the spaces they were added to, public channels included", async () => {
    const store = storeAs("member");
    const inGeneral = await say(store, SPACES.general, "zzguest in general");
    const inEngineering = await say(
      store,
      SPACES.engineering,
      "zzguest in engineering",
    );

    const guest = actAs(store, "viewer");
    const found = ids(await search(guest, "q=zzguest"));
    expect(found).toContain(inGeneral.id);
    expect(found).not.toContain(inEngineering.id);
  });

  it("finds a direct conversation for its two people only", async () => {
    const store = storeAs("member");
    const started = await chat(store, "POST", "/conversations", {
      body: { kind: "direct", userId: USERS.admin },
      idempotencyKey: key("search-dm"),
    });
    const dm = await say(
      store,
      started.body.conversation.id,
      "zzprivate between two",
    );

    expect(ids(await search(store, "q=zzprivate"))).toEqual([dm.id]);
    expect(ids(await search(actAs(store, "admin"), "q=zzprivate"))).toEqual([
      dm.id,
    ]);
    expect(ids(await search(actAs(store, "owner"), "q=zzprivate"))).toEqual([]);
  });

  it("keeps an archived channel searchable, and never returns a tombstone, a system message or a deleted space", async () => {
    const store = storeAs("owner");
    const kept = await say(store, SPACES.random, "zzarchive keep");
    const gone = await say(store, SPACES.random, "zzarchive gone");
    expect(
      (
        await chat(store, "DELETE", `/messages/${gone.id}`, {
          query: "expectedVersion=1",
        })
      ).status,
    ).toBe(204);
    expect(
      (
        await chat(store, "POST", `/channels/${SPACES.random}/archive`, {
          body: {
            archived: true,
            expectedVersion: store.chat.spaces.get(SPACES.random)!.version,
          },
        })
      ).status,
    ).toBe(200);

    const res = await search(store, "q=zzarchive");
    expect(ids(res)).toEqual([kept.id]);
    expect(res.body.items[0].spaceArchived).toBe(true);

    const systemText = store.chat
      .messagesOf(SPACES.leadership)
      .find((m) => m.kind === "system")!.content!;
    expect(
      ids(
        await search(
          actAs(store, "admin"),
          `q=${encodeURIComponent(systemText)}`,
        ),
      ),
    ).toEqual([]);

    const oldNotes = store.chat
      .messagesOf(SPACES.oldNotes)
      .find((m) => m.content)!;
    expect(
      ids(
        await search(
          store,
          `q=${encodeURIComponent(oldNotes.content!.slice(0, 12))}&spaceId=${SPACES.oldNotes}`,
        ),
      ),
    ).toEqual([]);
  });

  it("hides a message past the retention window before the job runs", async () => {
    const store = storeAs("owner");
    const old = await say(store, SPACES.general, "zzretained old");
    store.chat.advanceVirtualSeconds(40 * 86_400);
    const fresh = await say(store, SPACES.general, "zzretained fresh");
    expect(ids(await search(store, "q=zzretained"))).toEqual([
      fresh.id,
      old.id,
    ]);

    await chat(store, "PUT", "/retention", {
      body: { retentionDays: 30, mode: "purge", expectedVersion: 1 },
    });
    expect(ids(await search(store, "q=zzretained"))).toEqual([fresh.id]);
  });

  it("filters by space, author, date range, file and mention, and pages newest first without gaps", async () => {
    const store = storeAs("owner");
    const one = await say(store, SPACES.general, "zzfilter one");
    store.chat.advanceVirtualSeconds(3600);
    const two = await say(store, SPACES.engineering, "zzfilter two");
    store.chat.advanceVirtualSeconds(3600);
    actAs(store, "member");
    const three = await say(store, SPACES.general, "zzfilter three");

    const all = await search(store, "q=zzfilter");
    expect(ids(all)).toEqual([three.id, two.id, one.id]);
    expect(
      ids(await search(store, `q=zzfilter&spaceId=${SPACES.engineering}`)),
    ).toEqual([two.id]);
    expect(
      ids(await search(store, `q=zzfilter&authorId=${USERS.member}`)),
    ).toEqual([three.id]);
    const twoItem = all.body.items[1];
    expect(
      ids(
        await search(
          store,
          `q=zzfilter&from=${encodeURIComponent(twoItem.createdAt)}&to=${encodeURIComponent(all.body.items[0].createdAt)}`,
        ),
      ),
    ).toEqual([two.id]);

    const first = await search(store, "q=zzfilter&limit=2");
    expect(first.body.nextCursor).not.toBeNull();
    const second = await search(
      store,
      `q=zzfilter&limit=2&cursor=${encodeURIComponent(first.body.nextCursor)}`,
    );
    expect([...ids(first), ...ids(second)]).toEqual([three.id, two.id, one.id]);
    expect(second.body.nextCursor).toBeNull();
  });

  it("answers a hit with the jump target and a plain-text snippet with ranges, never markup", async () => {
    const store = storeAs("member");
    const m = await say(
      store,
      SPACES.general,
      "<b>Hóa đơn</b> số 1 và hoa don số 2",
    );
    const res = await search(store, "q=hoa%20don");
    const hit = res.body.items.find((item: any) => item.messageId === m.id);
    expect(hit).toMatchObject({
      spaceId: SPACES.general,
      spaceName: "general",
      spaceKind: "channel_public",
      threadRootId: null,
    });
    const text = hit.snippet.text as string;
    expect(
      hit.snippet.ranges.map((r: any) =>
        text.slice(r.start, r.start + r.length),
      ),
    ).toEqual(["Hóa đơn", "hoa don"]);
  });

  it("is counted per person and per Workspace: over the limit is 429 with retryAfter", async () => {
    const store = storeAs("member");
    let last = 200;
    for (let i = 0; i < 31; i += 1) last = (await search(store, "q=ab")).status;
    expect(last).toBe(429);
    const limited = await search(store, "q=ab");
    expect(limited.body).toMatchObject({
      ruleCode: "rate-limited",
      policy: "chat.search",
    });
    store.chat.advanceVirtualSeconds(limited.body.retryAfter + 60);
    expect((await search(store, "q=ab")).status).toBe(200);
  });

  it("reads another Workspace as NotFound", async () => {
    const res = await chat(storeAs("member"), "GET", "/search", {
      query: "q=ab",
      workspaceId: "mock-workspace-unknown",
    });
    expect(res.status).toBe(404);
    expect(WORKSPACE_ID).toBeTruthy();
  });
});

describe("MFB-CHAT-S3.4: the mock snippet and fold", () => {
  it("folds case, marks and đ", () => {
    expect(foldSearchText("Đường HÓA Đơn")).toBe("duong hoa don");
  });

  it("cuts a long message around the first match and says so", () => {
    const text = `${"x".repeat(300)} needle ${"y".repeat(300)}`;
    const snippet = buildSnippet(text, "needle");
    expect(snippet.cutBefore).toBe(true);
    expect(snippet.cutAfter).toBe(true);
    expect(snippet.ranges).toHaveLength(1);
    const { start, length } = snippet.ranges[0]!;
    expect(snippet.text.slice(start, start + length)).toBe("needle");
  });
});
