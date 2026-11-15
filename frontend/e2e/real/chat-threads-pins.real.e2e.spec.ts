import AxeBuilder from "@axe-core/playwright";
import { expect, test, type Browser, type Page } from "@playwright/test";
import {
  apiRead,
  apiWrite,
  findWorkspace,
  realUsers,
  signIn,
} from "./support/real-session";

test.use({ viewport: { width: 1440, height: 900 } });
test.setTimeout(300_000);

interface Message {
  id: string;
  content: string | null;
  deleted: boolean;
  threadRootId: string | null;
  threadReplyCount: number;
  replyToId: string | null;
}
interface Summary {
  replyCount: number;
  following: boolean;
  muted: boolean;
  unreadReplies: number;
}
interface Channel {
  id: string;
  name: string;
}

const english = () => {
  try {
    window.localStorage.setItem("notrelix-locale", "en");
  } catch {
    // Private mode: the default language applies.
  }
};
const log = (page: Page) => page.getByRole("log").first();
const row = (page: Page, text: string) =>
  page.locator("[data-message-id]", { hasText: text }).first();

async function person(browser: Browser, user: Parameters<typeof signIn>[1]) {
  const context = await browser.newContext({
    viewport: { width: 1440, height: 900 },
  });
  const page = await context.newPage();
  await page.addInitScript(english);
  await signIn(page, user);
  return { context, page };
}

test("threads, quotes, follow and mute, the followed list and pins work for members and are closed to everyone else, through the real stack", async ({
  page,
  browser,
}) => {
  const unique = Date.now().toString(36);
  const name = `thr-${unique}`;
  await page.addInitScript(english);
  await signIn(page, realUsers.workspaceOwner);
  const ws = (await findWorkspace(page, "Workspace 3")).id;
  const chat = `/workspaces/${ws}/chat`;

  const member = await person(browser, realUsers.admin);
  const outsider = await person(browser, realUsers.workspaceAdmin);
  const guest = await person(browser, realUsers.workspaceGuest);
  const memberId = (
    await apiRead<{ userId: string }>(
      member.page,
      `/workspaces/${ws}/members/me`,
    )
  ).userId;

  const made = await apiWrite<Channel>(page, "POST", `${chat}/channels`, {
    name,
    isPrivate: true,
  });
  const channelId = made.data!.id;
  expect(
    (
      await apiWrite(page, "POST", `${chat}/channels/${channelId}/members`, {
        userIds: [memberId],
      })
    ).status,
  ).toBeLessThan(300);
  const send = async (p: Page, content: string, extra: object = {}) => {
    const r = await apiWrite<Message>(
      p,
      "POST",
      `${chat}/spaces/${channelId}/messages`,
      { content, clientMessageId: crypto.randomUUID(), ...extra },
    );
    expect(r.status).toBeLessThan(300);
    return r.data!;
  };
  const rootText = `root ${unique}`;
  const root = await send(page, rootText);

  // F01 + F03: the member replies from the thread panel; the root shows the chip and the panel the reply.
  await member.page.goto(`/workspaces/${ws}${"/chat/"}${channelId}`);
  const target = row(member.page, rootText);
  await expect(target).toBeVisible({ timeout: 20_000 });
  await target.hover();
  await target.getByRole("button", { name: "Reply in thread" }).click();
  const panel = member.page.getByTestId("chat-thread-panel");
  await expect(panel).toBeVisible();
  const replyText = `reply ${unique}`;
  const box = panel.getByRole("combobox", { name: "Reply in thread" });
  await box.fill(replyText);
  await box.press("Enter");
  await expect(panel.getByText(replyText)).toBeVisible();
  await expect(target.getByRole("button", { name: /1 reply/ })).toBeVisible();
  const page1 = await apiRead<{
    items: Message[];
    thread: Summary;
    hasNewer: boolean;
  }>(page, `${chat}/messages/${root.id}/replies?limit=50`);
  expect(page1.items.map((m) => m.content)).toEqual([replyText]);
  expect(page1.thread.replyCount).toBe(1);

  // A retry of the same client id stores nothing; a reply to a reply is refused.
  const key = crypto.randomUUID();
  const body = {
    content: "retry",
    clientMessageId: key,
    alsoSendToChannel: false,
  };
  const first = await apiWrite<Message>(
    page,
    "POST",
    `${chat}/messages/${root.id}/replies`,
    body,
    key,
  );
  const again = await apiWrite<Message>(
    page,
    "POST",
    `${chat}/messages/${root.id}/replies`,
    body,
    key,
  );
  expect([first.status, again.status]).toEqual([201, 200]);
  expect(again.data!.id).toBe(first.data!.id);
  const nested = await apiWrite(
    page,
    "POST",
    `${chat}/messages/${first.data!.id}/replies`,
    {
      content: "nested",
      clientMessageId: crypto.randomUUID(),
      alsoSendToChannel: false,
    },
  );
  expect(nested.status).toBe(409);

  // F02: a quote of a message that is deleted later keeps no content.
  const quoted = await send(page, `quoted secret ${unique}`);
  const quoting = await send(member.page, `quoting ${unique}`, {
    replyToId: quoted.id,
  });
  expect(quoting.replyToId).toBe(quoted.id);
  expect(
    (await apiWrite(page, "DELETE", `${chat}/messages/${quoted.id}`)).status,
  ).toBeLessThan(300);
  const afterDelete = await apiRead<{ items: Message[] }>(
    member.page,
    `${chat}/spaces/${channelId}/messages?limit=100`,
  );
  const quoteRow = afterDelete.items.find((m) => m.id === quoting.id)!;
  expect(JSON.stringify(quoteRow)).not.toContain(`quoted secret ${unique}`);
  await member.page.reload();
  await expect(row(member.page, `quoting ${unique}`)).toContainText(
    "This message was deleted",
  );

  // F04 + F08: the owner follows through the reply panel; a mute stops the unread notice list from growing
  // in the owner's list only when read; the followed list shows the unread count and mark-as-read clears it.
  await page.goto(`/workspaces/${ws}/chat/${channelId}`);
  const ownerRoot = row(page, rootText);
  await ownerRoot.hover();
  await ownerRoot
    .getByRole("button", { name: /Open thread|1 reply|2 replies/ })
    .first()
    .click();
  const ownerPanel = page.getByTestId("chat-thread-panel");
  await expect(ownerPanel).toBeVisible();
  const followBtn = ownerPanel.getByRole("button", {
    name: /^(Follow|Following)$/,
  });
  if ((await followBtn.textContent()) === "Following") await followBtn.click();
  await expect(
    ownerPanel.getByRole("button", { name: "Follow", exact: true }),
  ).toBeVisible();
  await ownerPanel.getByRole("button", { name: "Follow", exact: true }).click();
  await expect(
    ownerPanel.getByRole("button", { name: "Following" }),
  ).toBeVisible();
  await ownerPanel.getByRole("button", { name: "Mute" }).click();
  await expect(ownerPanel.getByText("Muted", { exact: true })).toBeVisible();
  let mine = await apiRead<{ items: { root: Message; thread: Summary }[] }>(
    page,
    `/workspaces/${ws}/chat/me/threads?limit=50`,
  );
  const entry = mine.items.find((i) => i.root.id === root.id)!;
  expect(entry.thread.muted).toBe(true);
  await ownerPanel.getByRole("button", { name: "Unmute" }).click();

  const more = await apiWrite<Message>(
    member.page,
    "POST",
    `${chat}/messages/${root.id}/replies`,
    {
      content: `more ${unique}`,
      clientMessageId: crypto.randomUUID(),
      alsoSendToChannel: false,
    },
  );
  expect(more.status).toBe(201);
  mine = await apiRead(page, `/workspaces/${ws}/chat/me/threads?limit=50`);
  expect(
    mine.items.find((i) => i.root.id === root.id)!.thread.unreadReplies,
  ).toBeGreaterThan(0);
  await page.getByRole("button", { name: "Following" }).first().click();
  const followed = page.getByTestId("chat-followed-panel");
  const item = followed
    .getByTestId("chat-followed-item")
    .filter({ hasText: rootText });
  await expect(item.getByTestId("chat-followed-unread")).toBeVisible();
  const axe = await new AxeBuilder({ page })
    .include('[data-testid="chat-followed-panel"]')
    .analyze();
  expect(
    axe.violations.filter(
      (v) => v.impact === "serious" || v.impact === "critical",
    ),
  ).toEqual([]);
  await item.getByRole("button", { name: "Mark as read" }).click();
  await expect(item.getByTestId("chat-followed-unread")).toHaveCount(0);
  // The follow list is the caller's own: the member's list does not hold the owner's follow state.
  const memberMine = await apiRead<{
    items: { thread: Summary; root: Message }[];
  }>(member.page, `/workspaces/${ws}/chat/me/threads?limit=50`);
  expect(
    memberMine.items.find((i) => i.root.id === root.id)?.thread.unreadReplies ??
      0,
  ).toBe(0);

  // F06 + F07: the member pins from the toolbar; the owner reads it in the panel; the owner unpins; a pinned
  // message that is deleted leaves the list.
  const pinText = `pin me ${unique}`;
  const pinned = await send(page, pinText);
  await member.page.reload();
  const pinRow = row(member.page, pinText);
  await pinRow.hover();
  await pinRow.getByRole("button", { name: "Pin message" }).click();
  await expect
    .poll(async () =>
      (
        await apiRead<{ items: { message: Message }[] }>(
          page,
          `${chat}/spaces/${channelId}/pins?limit=50`,
        )
      ).items.map((p) => p.message.id),
    )
    .toContain(pinned.id);
  await page.reload();
  await page.getByRole("button", { name: "Pins", exact: true }).click();
  const pinsPanel = page.getByTestId("chat-pins-panel");
  await expect(pinsPanel.getByText(pinText)).toBeVisible();
  const pinsAxe = await new AxeBuilder({ page })
    .include('[data-testid="chat-pins-panel"]')
    .analyze();
  expect(
    pinsAxe.violations.filter(
      (v) => v.impact === "serious" || v.impact === "critical",
    ),
  ).toEqual([]);
  await pinsPanel
    .getByRole("button", { name: new RegExp(`Unpin message: ${pinText}`) })
    .click();
  await expect(pinsPanel.getByText(pinText)).toHaveCount(0);
  const again2 = await apiWrite(
    page,
    "PUT",
    `${chat}/messages/${pinned.id}/pin`,
  );
  expect(again2.status).toBeLessThan(300);
  expect(
    (await apiWrite(page, "DELETE", `${chat}/messages/${pinned.id}`)).status,
  ).toBeLessThan(300);
  const left = await apiRead<{ items: { message: Message }[] }>(
    page,
    `${chat}/spaces/${channelId}/pins?limit=50`,
  );
  expect(left.items.map((p) => p.message.id)).not.toContain(pinned.id);

  // Everyone outside the private channel (a workspace admin who is not in it, a guest who was not added) gets
  // the identical NotFound on each read and write that takes a message, thread or space id.
  for (const who of [outsider.page, guest.page]) {
    const reads = [
      `${chat}/messages/${root.id}/replies`,
      `${chat}/spaces/${channelId}/pins`,
    ];
    for (const path of reads) {
      const r = await who.request.get(
        `${process.env.VITE_API_URL ?? "http://127.0.0.1:58000/api/v1"}${path}`,
      );
      expect(r.status(), path).toBe(404);
    }
    const writes: [string, string, object?][] = [
      [
        "POST",
        `${chat}/messages/${root.id}/replies`,
        {
          content: "x",
          clientMessageId: crypto.randomUUID(),
          alsoSendToChannel: false,
        },
      ],
      [
        "PUT",
        `${chat}/threads/${root.id}/follow`,
        { following: true, muted: false },
      ],
      ["PUT", `${chat}/threads/${root.id}/read`, { upToReplySequence: 1 }],
      ["PUT", `${chat}/messages/${root.id}/pin`],
      ["DELETE", `${chat}/messages/${root.id}/pin`],
    ];
    for (const [method, path, payload] of writes) {
      const r = await apiWrite(who, method as "POST", path, payload);
      expect(r.status, `${method} ${path}`).toBe(404);
    }
  }

  await Promise.all([member, outsider, guest].map((p) => p.context.close()));
});
