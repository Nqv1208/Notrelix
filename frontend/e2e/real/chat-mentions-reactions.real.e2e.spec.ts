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
test.setTimeout(240_000);

interface Mention {
  kind: string;
  start: number;
  length: number;
  userId: string | null;
  spaceId: string | null;
}
interface Notice {
  code: string;
  userId: string | null;
}
interface Message {
  id: string;
  content: string | null;
  mentions: Mention[];
  mentionNotices: Notice[];
  reactions: { emoji: string; count: number; reactedByMe: boolean }[];
  attachments: { id: string; fileName: string; downloadPath: string }[];
}
interface MessagePage {
  items: Message[];
}
interface Channel {
  id: string;
  name: string;
}
interface MemberRow {
  userId: string;
  name: string;
}
interface InboxEntry {
  type: string;
  excerpt: string | null;
}

const english = () => {
  try {
    window.localStorage.setItem("notrelix-locale", "en");
  } catch {
    // Private mode: the default language applies.
  }
};

const log = (page: Page) => page.getByRole("log");
const composer = (page: Page, channel: string) =>
  page.getByRole("combobox", { name: `Message #${channel}` });
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

test("people are mentioned and broadcast to, channels are linked, reactions and files are added and removed, and each outsider is held back, through the real stack", async ({
  page,
  browser,
}) => {
  const unique = Date.now().toString(36);
  const publicName = `men-${unique}`;
  const secretName = `sec-${unique}`;
  await page.addInitScript(english);
  await signIn(page, realUsers.workspaceOwner);
  const workspace = await findWorkspace(page, "Workspace 3");
  const ws = workspace.id;
  const base = `/workspaces/${ws}`;
  const chat = `/workspaces/${ws}/chat`;

  // The people: the owner writes, the member reads, the workspace admin stays out of the channel, the guest is added.
  const reader = await person(browser, realUsers.admin);
  const outside = await person(browser, realUsers.workspaceAdmin);
  const guest = await person(browser, realUsers.workspaceGuest);
  const idOf = async (p: Page) =>
    (await apiRead<{ userId: string }>(p, `/workspaces/${ws}/members/me`))
      .userId;
  const readerId = await idOf(reader.page);
  const outsideId = await idOf(outside.page);
  const guestId = await idOf(guest.page);
  const roster = await apiRead<{ items: MemberRow[] }>(
    page,
    `/workspaces/${ws}/members?limit=200`,
  );
  const nameOf = (id: string) =>
    roster.items.find((m) => m.userId === id)!.name;

  const made = await apiWrite<Channel>(page, "POST", `${chat}/channels`, {
    name: publicName,
    isPrivate: false,
  });
  const channelId = made.data!.id;
  const secret = await apiWrite<Channel>(page, "POST", `${chat}/channels`, {
    name: secretName,
    isPrivate: true,
  });
  const secretId = secret.data!.id;
  const added = await apiWrite(
    page,
    "POST",
    `${chat}/channels/${channelId}/members`,
    { userIds: [readerId, guestId] },
  );
  expect(added.status).toBeLessThan(300);
  const messagesOf = async (p: Page) =>
    (
      await apiRead<MessagePage>(
        p,
        `${chat}/spaces/${channelId}/messages?limit=100`,
      )
    ).items;
  // The bubble shows before the stored copy is readable: wait for the stored message.
  const storedWith = async (p: Page, text: string, exact = false) => {
    let found: Message | undefined;
    await expect
      .poll(
        async () => {
          found = (await messagesOf(p)).find((m) =>
            exact ? m.content === text : m.content?.includes(text),
          );
          return found !== undefined;
        },
        { timeout: 30_000 },
      )
      .toBe(true);
    return found!;
  };
  const inboxOf = async (p: Page) =>
    (
      await apiRead<{ items: InboxEntry[] }>(
        p,
        `/workspaces/${ws}/inbox?limit=50`,
      )
    ).items;

  await page.goto(`${base}/chat/${channelId}`);
  await expect(composer(page, publicName)).toBeVisible({ timeout: 20_000 });

  // Mention a person: the picker lists members, the pick sends a stored mention, and the person is notified.
  const mentionText = `ping ${unique}`;
  const box = composer(page, publicName);
  await box.fill(`@${nameOf(readerId)}`.slice(0, 4));
  const picker = page.getByRole("listbox", { name: "Mention suggestions" });
  await expect(picker).toBeVisible();
  await picker
    .getByRole("option", { name: new RegExp(nameOf(readerId)) })
    .click();
  await box.pressSequentially(` ${mentionText}`);
  await box.press("Enter");
  await expect(log(page).getByText(mentionText)).toBeVisible();
  let sent = await storedWith(page, mentionText);
  expect(sent.mentions.map((m) => [m.kind, m.userId])).toEqual([
    ["user", readerId],
  ]);
  await expect
    .poll(
      async () =>
        (await inboxOf(reader.page)).filter((e) =>
          e.excerpt?.includes(mentionText),
        ).length,
      { timeout: 30_000 },
    )
    .toBe(1);

  // A person outside the channel is marked in the picker, the composer warns before the send, and nothing is
  // granted or delivered after it: no notification, and the channel stays NotFound to them.
  const warnText = `outside ${unique}`;
  await box.fill(`@${nameOf(outsideId)}`.slice(0, 4));
  await expect(picker).toBeVisible();
  const outsideOption = picker.getByRole("option", {
    name: new RegExp(nameOf(outsideId)),
  });
  await expect(outsideOption).toContainText("Not in this channel");
  await outsideOption.click();
  await box.pressSequentially(` ${warnText}`);
  const warning = page.getByTestId("chat-mention-warnings");
  await expect(warning).toContainText("will not be notified");
  await expect(warning.getByRole("button", { name: /^Add / })).toBeVisible();
  // The notice is for the sender and rides on the send answer only.
  const answer = page.waitForResponse(
    (r) =>
      r.request().method() === "POST" &&
      r.url().endsWith(`/chat/spaces/${channelId}/messages`),
  );
  await box.press("Enter");
  await expect(log(page).getByText(warnText)).toBeVisible();
  const sentNotices = ((await (await answer).json()) as Message).mentionNotices;
  expect(sentNotices.map((n) => [n.code, n.userId])).toEqual([
    ["cannot-view", outsideId],
  ]);
  // Nothing is granted: the mentioned person is still no member (the channel is not in their list and they cannot post).
  expect(
    (await apiRead<Channel[]>(outside.page, `${chat}/channels`)).map(
      (c) => c.id,
    ),
  ).not.toContain(channelId);
  const refused = await apiWrite(
    outside.page,
    "POST",
    `${chat}/spaces/${channelId}/messages`,
    { content: "not a member", clientMessageId: crypto.randomUUID() },
  );
  expect(refused.status).toBe(404);
  await page.waitForTimeout(3000);
  expect(
    (await inboxOf(outside.page)).filter((e) => e.excerpt?.includes(warnText)),
  ).toHaveLength(0);

  // Broadcast: @channel reaches the member; the guest may not broadcast, so the same token stays plain text.
  const castText = `everyone ${unique}`;
  await box.fill("@chan");
  await picker
    .getByRole("option", { name: /channel/ })
    .first()
    .click();
  await box.pressSequentially(` ${castText}`);
  await box.press("Enter");
  await expect(log(page).getByText(castText)).toBeVisible();
  sent = await storedWith(page, castText);
  expect(sent.mentions.map((m) => m.kind)).toEqual(["broadcast-channel"]);
  await expect
    .poll(
      async () =>
        (await inboxOf(reader.page)).filter((e) =>
          e.excerpt?.includes(castText),
        ).length,
      { timeout: 30_000 },
    )
    .toBe(1);
  const guestText = `@channel guest ${unique}`;
  const guestSend = await apiWrite<Message>(
    guest.page,
    "POST",
    `${chat}/spaces/${channelId}/messages`,
    {
      content: guestText,
      clientMessageId: crypto.randomUUID(),
      mentions: [
        {
          kind: "broadcast-channel",
          start: 0,
          length: 8,
          userId: null,
          spaceId: null,
        },
      ],
    },
  );
  expect(guestSend.status).toBeLessThan(300);
  expect(guestSend.data!.mentions).toEqual([]);
  expect(guestSend.data!.mentionNotices.map((n) => n.code)).toEqual([
    "broadcast-not-allowed",
  ]);
  const guestPeople = await apiRead<{ canBroadcast: boolean }>(
    guest.page,
    `${chat}/spaces/${channelId}/mentionables`,
  );
  expect(guestPeople.canBroadcast).toBe(false);
  await page.waitForTimeout(3000);
  expect(
    (await inboxOf(reader.page)).filter((e) =>
      e.excerpt?.includes(`guest ${unique}`),
    ),
  ).toHaveLength(0);

  // Link a channel with #: the owner sees the private channel as a link; the member who cannot see it reads plain text.
  const linkText = `see #${secretName} ${unique}`;
  const linked = await apiWrite(
    page,
    "POST",
    `${chat}/spaces/${channelId}/messages`,
    {
      content: linkText,
      clientMessageId: crypto.randomUUID(),
      mentions: [
        {
          kind: "channel-link",
          start: 4,
          length: secretName.length + 1,
          userId: null,
          spaceId: secretId,
        },
      ],
    },
  );
  expect(linked.status).toBeLessThan(300);
  expect((await storedWith(page, linkText, true)).mentions).toHaveLength(1);
  expect((await storedWith(reader.page, linkText, true)).mentions).toHaveLength(
    0,
  );
  await box.fill("#sec");
  await expect(
    picker.getByRole("option", { name: new RegExp(secretName) }),
  ).toBeVisible();
  await box.fill("");

  // React: a double add keeps one reaction, the count shows for the other reader, the list names who, and a removal
  // drops the count on the other page live.
  await reader.page.goto(`${base}/chat/${channelId}`);
  await expect(composer(reader.page, publicName)).toBeVisible({
    timeout: 20_000,
  });
  const target = row(reader.page, mentionText);
  await target.hover();
  await target.getByRole("button", { name: "React", exact: true }).click();
  await reader.page.getByRole("button", { name: "React with 👍" }).click();
  const readerChip = target.getByRole("button", { name: /👍 1/ });
  await expect(readerChip).toBeVisible();
  const messageId = (await storedWith(page, mentionText)).id;
  const again = await apiWrite(
    reader.page,
    "PUT",
    `${chat}/messages/${messageId}/reactions/${encodeURIComponent("👍")}`,
  );
  expect(again.status).toBe(200);
  // The reader's reaction reaches the owner's open page live; the owner then joins in with the chip.
  await expect(
    row(page, mentionText).getByRole("button", { name: /^👍 1$/ }),
  ).toBeVisible({ timeout: 20_000 });
  await row(page, mentionText)
    .getByRole("button", { name: /^👍 1$/ })
    .click();
  await expect(
    row(page, mentionText).getByRole("button", { name: /👍 2, you reacted/ }),
  ).toBeVisible({ timeout: 20_000 });
  const who = await apiRead<{ items: { userId: string }[] }>(
    page,
    `${chat}/messages/${messageId}/reactions?kind=${encodeURIComponent("👍")}`,
  );
  expect(who.items.map((i) => i.userId).sort()).toEqual(
    [readerId, await idOf(page)].sort(),
  );
  await row(page, mentionText)
    .getByRole("button", { name: "Who reacted" })
    .click();
  await expect(page.getByRole("list", { name: "Who reacted" })).toBeVisible();
  await page.keyboard.press("Escape");
  const removed = await apiWrite(
    reader.page,
    "DELETE",
    `${chat}/messages/${messageId}/reactions/${encodeURIComponent("👍")}`,
  );
  expect(removed.status).toBe(200);
  await expect(
    row(page, mentionText).getByRole("button", { name: /👍 1, you reacted/ }),
  ).toBeVisible({ timeout: 20_000 });
  const missing = await apiWrite(
    reader.page,
    "DELETE",
    `${chat}/messages/${messageId}/reactions/${encodeURIComponent("👍")}`,
  );
  expect(missing.status).toBe(200);
  const outsider = await apiWrite(
    outside.page,
    "PUT",
    `${chat}/messages/${messageId}/reactions/${encodeURIComponent("👍")}`,
  );
  expect(outsider.status).toBe(404);

  // Attach: a blocked type is refused before anything is stored; an allowed file is stored on the message, downloads
  // for a member and is NotFound to a person outside the channel.
  const fileText = `with a file ${unique}`;
  const input = page.getByTestId("chat-file-input");
  await input.setInputFiles({
    name: "run.exe",
    mimeType: "application/octet-stream",
    buffer: Buffer.from("MZ-not-really"),
  });
  await box.fill(`blocked ${unique}`);
  await box.press("Enter");
  await expect(page.getByText(/cannot be attached/)).toBeVisible({
    timeout: 20_000,
  });
  await page
    .getByRole("button", { name: /^Dismiss/ })
    .first()
    .click()
    .catch(() => {});
  await input.setInputFiles({
    name: "notes.txt",
    mimeType: "text/plain",
    buffer: Buffer.from(`hello ${unique}`),
  });
  await box.fill(fileText);
  await box.press("Enter");
  await expect(row(page, fileText)).toBeVisible({ timeout: 20_000 });
  await expect(
    // The file name and the icon both download: either one is enough.
    row(page, fileText)
      .getByRole("button", { name: /Download notes.txt/ })
      .first(),
  ).toBeVisible({ timeout: 30_000 });
  const filed = await storedWith(page, fileText);
  const files = await apiRead<{ id: string; fileName: string }[]>(
    page,
    `${chat}/messages/${filed.id}/attachments`,
  );
  expect(files.map((f) => f.fileName)).toEqual(["notes.txt"]);
  expect(
    (await messagesOf(page)).flatMap((m) => m.attachments ?? []),
  ).toHaveLength(1);
  const api = process.env.VITE_API_URL ?? "http://127.0.0.1:58000/api/v1";
  const content = `${api}${chat}/messages/${filed.id}/attachments/${files[0]!.id}/content`;
  const read = await reader.page.request.get(content);
  expect(read.status()).toBe(200);
  expect(await read.text()).toBe(`hello ${unique}`);
  // A file of a private channel is NotFound to a person who is not in it (a public channel is readable by the Workspace).
  const secretMessage = await apiWrite<Message>(
    page,
    "POST",
    `${chat}/spaces/${secretId}/messages`,
    { content: `private file ${unique}`, clientMessageId: crypto.randomUUID() },
  );
  const csrf = (await (await page.request.get(`${api}/auth/csrf`)).json()) as {
    token: string;
  };
  const uploaded = await page.request.post(
    `${api}${chat}/messages/${secretMessage.data!.id}/attachments`,
    {
      headers: {
        "X-CSRF-Token": csrf.token,
        "Idempotency-Key": crypto.randomUUID(),
      },
      multipart: {
        file: {
          name: "secret.txt",
          mimeType: "text/plain",
          buffer: Buffer.from("secret"),
        },
      },
    },
  );
  expect(uploaded.status()).toBe(201);
  const secretFile = (await uploaded.json()) as { id: string };
  const secretContent = `${api}${chat}/messages/${secretMessage.data!.id}/attachments/${secretFile.id}/content`;
  expect((await page.request.get(secretContent)).status()).toBe(200);
  expect((await outside.page.request.get(secretContent)).status()).toBe(404);

  // Link a Board item: the owner who can view it reads its title, a person without access reads no title, and the
  // answer for an item that does not exist has the same shape.
  const boards = await apiRead<{ id: string; title: string }[]>(
    page,
    `/workspaces/${ws}/boards`,
  );
  const board = boards.find((b) => b.title.startsWith("Engineering Tasks"))!;
  const groupId = (
    await apiRead<{ lists: { id: string }[] }>(page, `/boards/${board.id}/full`)
  ).lists[0]!.id;
  const itemTitle = `Linked item ${unique}`;
  const item = await apiWrite<{ id: string }>(
    page,
    "POST",
    `/boards/${board.id}/items`,
    { groupId, title: itemTitle },
  );
  expect(item.status).toBeLessThan(300);
  const resolve = (p: Page, itemId: string) =>
    apiRead<{ accessible: boolean; title: string | null }>(
      p,
      `${chat}/links/resolve?url=${encodeURIComponent(
        `${base}/boards/${board.id}?item=${itemId}`,
      )}`,
    );
  expect(await resolve(page, item.data!.id)).toMatchObject({
    accessible: true,
    title: itemTitle,
  });
  const unseen = await resolve(guest.page, item.data!.id);
  const absent = await resolve(page, crypto.randomUUID());
  expect(unseen.accessible).toBe(false);
  expect(unseen.title ?? null).toBeNull();
  expect(absent).toMatchObject({ accessible: false });
  expect(absent.title ?? null).toBeNull();

  // A mention of a person who left before the message is sent notifies nobody (the check runs at delivery).
  const left = await apiWrite(
    reader.page,
    "DELETE",
    `${chat}/channels/${channelId}/members/${readerId}`,
  );
  expect(left.status).toBeLessThan(300);
  const lateText = `late ${unique}`;
  const late = await apiWrite<Message>(
    page,
    "POST",
    `${chat}/spaces/${channelId}/messages`,
    {
      content: `@${nameOf(readerId)} ${lateText}`,
      clientMessageId: crypto.randomUUID(),
      mentions: [
        {
          kind: "user",
          start: 0,
          length: nameOf(readerId).length + 1,
          userId: readerId,
          spaceId: null,
        },
      ],
    },
  );
  expect(late.status).toBeLessThan(300);
  expect(late.data!.mentionNotices.map((n) => n.code)).toEqual(["cannot-view"]);
  await page.waitForTimeout(3000);
  expect(
    (await inboxOf(reader.page)).filter((e) => e.excerpt?.includes(lateText)),
  ).toHaveLength(0);

  // axe finds nothing serious on the channel with mentions, reactions and a file.
  const axe = await new AxeBuilder({ page })
    .disableRules(["color-contrast", "scrollable-region-focusable"])
    .analyze();
  expect(
    axe.violations.filter((v) =>
      ["serious", "critical"].includes(v.impact ?? ""),
    ),
  ).toEqual([]);

  for (const p of [reader, outside, guest]) await p.context.close();
});
