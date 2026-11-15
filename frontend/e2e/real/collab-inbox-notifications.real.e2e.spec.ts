import { expect, test, type Page } from "@playwright/test";
import {
  apiRead,
  apiWrite,
  findWorkspace,
  openBoard,
  realUsers,
  signIn,
} from "./support/real-session";

test.use({ viewport: { width: 1440, height: 900 } });
test.setTimeout(180_000);

interface InboxEntry {
  readonly id: string;
  readonly type: string;
  readonly excerpt: string | null;
  readonly unread: boolean;
  readonly needsReply: boolean;
  readonly snoozedUntil: string | null;
  readonly target: { readonly id: string } | null;
}
interface Preferences {
  readonly version: number;
}

/** A manager of Workspace 3 and a reader (the admin, a member of it) with one shared item. */
async function scene(browser: import("@playwright/test").Browser, page: Page) {
  const context = await browser.newContext();
  const reader = await context.newPage();
  await signIn(reader, realUsers.admin);
  const workspace = await findWorkspace(reader, "Workspace 3");
  const me = await apiRead<{ userId: string }>(
    reader,
    `/workspaces/${workspace.id}/members/me`,
  );
  await signIn(page, realUsers.workspaceAdmin);
  const { board } = await openBoard(page, "Workspace 3", "Engineering Tasks");
  const groupId = (
    await apiRead<{ lists: { id: string }[] }>(page, `/boards/${board.id}/full`)
  ).lists[0]!.id;
  const unique = Date.now().toString(36);
  const item = await apiWrite<{ id: string }>(
    page,
    "POST",
    `/boards/${board.id}/items`,
    { groupId, title: `Collab inbox ${unique}` },
  );
  const itemId = item.data!.id;
  const inbox = async () =>
    (
      await apiRead<{ items: InboxEntry[] }>(
        reader,
        `/workspaces/${workspace.id}/inbox?limit=50`,
      )
    ).items;
  const comment = async (text: string, mention = false) =>
    expect(
      (
        await apiWrite(page, "POST", `/board-items/${itemId}/comments`, {
          contentMd: text,
          mentionedUserIds: mention ? [me.userId] : undefined,
        })
      ).status,
    ).toBeLessThan(300);
  const entryWith = async (text: string) =>
    (await inbox()).find((entry) => entry.excerpt?.includes(text));
  return {
    context,
    reader,
    workspace,
    me,
    itemId,
    unique,
    inbox,
    comment,
    entryWith,
  };
}

test("a mention reaches the Inbox only while the in-app mention preference is on", async ({
  browser,
  page,
}) => {
  const s = await scene(browser, page);
  const setMention = async (inApp: boolean) => {
    const read = await apiRead<Preferences>(
      s.reader,
      `/workspaces/${s.workspace.id}/notification-preferences`,
    );
    const saved = await apiWrite(
      s.reader,
      "PATCH",
      `/workspaces/${s.workspace.id}/notification-preferences`,
      {
        expectedVersion: read.version,
        rows: [{ type: "mention", inApp }],
      },
    );
    expect(saved.status).toBeLessThan(300);
  };

  try {
    await setMention(false);
    await s.comment(`Muted-pref mention ${s.unique}`, true);
    // The event is delivered asynchronously; the preference is read at delivery, so let it be delivered first.
    await s.reader.waitForTimeout(10_000);
    await setMention(true);
    await s.comment(`Open-pref mention ${s.unique}`, true);

    await expect
      .poll(
        async () => Boolean(await s.entryWith(`Open-pref mention ${s.unique}`)),
        {
          timeout: 60_000,
        },
      )
      .toBe(true);
    expect(
      await s.entryWith(`Muted-pref mention ${s.unique}`),
      "the mention made while the preference was off is not delivered",
    ).toBeUndefined();

    await s.reader.goto(`/workspaces/${s.workspace.id}/inbox`);
    await expect(
      s.reader.getByText(`Open-pref mention ${s.unique}`).first(),
    ).toBeVisible();
    await expect(s.reader.getByText("Needs a reply").first()).toBeVisible();
    await expect(
      s.reader.getByText(`Muted-pref mention ${s.unique}`),
    ).toHaveCount(0);
  } finally {
    await setMention(true);
    await s.context.close();
  }
});

test("a follower is told once per change, and not while the thread is muted", async ({
  browser,
  page,
}) => {
  const s = await scene(browser, page);
  await expect
    .poll(
      async () =>
        (
          await apiRead<{ following: boolean }>(
            page,
            `/board-items/${s.itemId}/following`,
          )
        ).following,
      { message: "the creator follows the item they create", timeout: 30_000 },
    )
    .toBe(true);
  const assigned = await apiWrite(
    page,
    "POST",
    `/board-items/${s.itemId}/assignees`,
    {
      userId: s.me.userId,
      expectedVersion: (
        await apiRead<{ version: number }>(page, `/board-items/${s.itemId}`)
      ).version,
    },
  );
  expect(assigned.status, JSON.stringify(assigned.data)).toBeLessThan(300);
  await expect
    .poll(
      async () =>
        (
          await apiRead<{ following: boolean }>(
            s.reader,
            `/board-items/${s.itemId}/following`,
          )
        ).following,
      { message: "the assignee follows", timeout: 30_000 },
    )
    .toBe(true);

  await s.comment(`First change ${s.unique}`);
  await expect
    .poll(async () => Boolean(await s.entryWith(`First change ${s.unique}`)), {
      timeout: 60_000,
    })
    .toBe(true);
  const first = (await s.entryWith(`First change ${s.unique}`))!;
  expect(
    (await s.inbox()).filter((entry) => entry.excerpt === first.excerpt),
    "one entry for one comment",
  ).toHaveLength(1);

  const muted = await apiWrite(
    s.reader,
    "POST",
    `/workspaces/${s.workspace.id}/inbox/${first.id}/mute`,
  );
  expect(muted.status).toBeLessThan(300);
  await s.comment(`While muted ${s.unique}`);

  const unmuted = await apiWrite(
    s.reader,
    "POST",
    `/workspaces/${s.workspace.id}/inbox/${first.id}/unmute`,
  );
  expect(unmuted.status).toBeLessThan(300);
  await s.comment(`After unmute ${s.unique}`);

  await expect
    .poll(async () => Boolean(await s.entryWith(`After unmute ${s.unique}`)), {
      timeout: 60_000,
    })
    .toBe(true);
  expect(
    await s.entryWith(`While muted ${s.unique}`),
    "nothing arrives while the thread is muted",
  ).toBeUndefined();

  await s.reader.goto(`/workspaces/${s.workspace.id}/inbox`);
  await s.reader.getByRole("tab", { name: "Following" }).click();
  await expect(
    s.reader.getByText(`After unmute ${s.unique}`).first(),
  ).toBeVisible();
  await s.context.close();
});

test("the recipient reads, finishes, snoozes and replies to mentions from the Inbox, and each change survives a reload", async ({
  browser,
  page,
}) => {
  const s = await scene(browser, page);
  const { reader } = s;
  const phrase = (step: string) => `${step} ${s.unique}`;
  const posted = (suffix: RegExp) =>
    reader.waitForResponse(
      (response) =>
        response.request().method() === "POST" && suffix.test(response.url()),
    );
  const listed = async (state: "inbox" | "done" | "snoozed", text: string) =>
    (
      await apiRead<{ items: InboxEntry[] }>(
        reader,
        `/workspaces/${s.workspace.id}/inbox?limit=50&state=${state}`,
      )
    ).items.find((entry) => entry.excerpt?.includes(text));
  const reload = async () => {
    await reader.goto(`/workspaces/${s.workspace.id}/inbox`);
    await expect(
      reader.getByRole("heading", { level: 1, name: "Inbox" }),
    ).toBeVisible();
  };
  const open = async (text: string) => {
    await reload();
    await reader.getByText(text).first().click();
  };

  try {
    for (const step of ["Read", "Done", "Snooze", "Reply"]) {
      await s.comment(phrase(step), true);
      await expect
        .poll(async () => Boolean(await s.entryWith(phrase(step))), {
          message: `${step} mention delivered`,
          timeout: 60_000,
        })
        .toBe(true);
    }

    // Mark read: unread before, read after, and still read after a reload.
    const read = phrase("Read");
    expect((await s.entryWith(read))!.unread).toBe(true);
    await open(read);
    const marked = posted(/\/inbox\/[^/]+\/read$/);
    await reader.getByRole("button", { name: /^Mark as read/ }).click();
    expect((await marked).status()).toBeLessThan(300);
    await expect
      .poll(async () => (await s.entryWith(read))!.unread)
      .toBe(false);
    await reload();
    expect((await s.entryWith(read))!.unread).toBe(false);
    // Needs action lists only what is still open; the All tab keeps the read entry.
    await reader.getByRole("tab", { name: "All" }).click();
    await expect(reader.getByText(read).first()).toBeVisible();

    // Mark done leaves the default list and sits under Done; moving it back returns it.
    const done = phrase("Done");
    await open(done);
    const finished = posted(/\/inbox\/[^/]+\/done$/);
    await reader.getByRole("button", { name: "Mark as done" }).click();
    expect((await finished).status()).toBeLessThan(300);
    await expect(reader.getByText(done)).toHaveCount(0);
    await reload();
    await expect(reader.getByText(done)).toHaveCount(0);
    expect(await listed("inbox", done)).toBeUndefined();
    expect(await listed("done", done)).toBeDefined();
    await reader.getByRole("button", { name: "Done", exact: true }).click();
    await reader.getByText(done).first().click();
    const reopened = posted(/\/inbox\/[^/]+\/reopen$/);
    await reader.getByRole("button", { name: "Move back to inbox" }).click();
    expect((await reopened).status()).toBeLessThan(300);
    await reload();
    await expect(reader.getByText(done).first()).toBeVisible();
    expect(await listed("inbox", done)).toBeDefined();
    expect(await listed("done", done)).toBeUndefined();

    // Snooze: the entry leaves the default list and appears under Snoozed.
    const snooze = phrase("Snooze");
    await open(snooze);
    const snoozed = posted(/\/inbox\/[^/]+\/snooze$/);
    await reader.getByRole("button", { name: /Remind me later/ }).click();
    await reader.getByRole("menuitem", { name: "In 1 hour" }).click();
    expect((await snoozed).status()).toBeLessThan(300);
    await expect(reader.getByText(snooze)).toHaveCount(0);
    await reload();
    await expect(reader.getByText(snooze)).toHaveCount(0);
    expect(await listed("inbox", snooze)).toBeUndefined();
    const held = await listed("snoozed", snooze);
    expect(held?.snoozedUntil).toBeTruthy();
    await reader.getByRole("button", { name: "Snoozed", exact: true }).click();
    await expect(reader.getByText(snooze).first()).toBeVisible();

    // Reply: a comment lands on the item and the entry stops needing a reply.
    const reply = phrase("Reply");
    expect((await s.entryWith(reply))!.needsReply).toBe(true);
    await open(reply);
    await reader
      .getByRole("textbox", { name: /^Reply/ })
      .fill(`Answered ${s.unique}`);
    const sent = posted(/\/board-items\/[^/]+\/comments$/);
    await reader.getByRole("button", { name: /^Send/ }).click();
    expect((await sent).status()).toBeLessThan(300);
    const comments = await apiRead<{ contentMd?: string; content?: string }[]>(
      page,
      `/board-items/${s.itemId}/comments`,
    );
    expect(
      comments.some((c) =>
        (c.contentMd ?? c.content ?? "").includes(`Answered ${s.unique}`),
      ),
    ).toBe(true);
    await expect
      .poll(async () => (await s.entryWith(reply))?.needsReply)
      .toBe(false);
    await reload();
    expect((await s.entryWith(reply))!.needsReply).toBe(false);
  } finally {
    await s.context.close();
  }
});
