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
