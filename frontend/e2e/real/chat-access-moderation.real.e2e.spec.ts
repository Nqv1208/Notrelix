import { readFile } from "node:fs/promises";
import AxeBuilder from "@axe-core/playwright";
import { expect, test, type Page } from "@playwright/test";
import {
  apiRead,
  apiWrite,
  findWorkspace,
  realUsers,
  signIn,
} from "./support/real-session";

// Chat access, moderation and hardening on the real stack: a Guest sees only the spaces they were added to, a channel
// manager turns posting off for a role, a moderator removes another person's message, search follows the live
// membership, and a burst of sends is answered 429 while the draft stays.
test.use({ viewport: { width: 1440, height: 900 } });
test.setTimeout(300_000);

interface Channel {
  id: string;
  name: string;
}
interface Message {
  id: string;
  content: string | null;
  deleted: boolean;
  version: number;
}
interface MessagePage {
  items: Message[];
}
interface SearchPage {
  items: { messageId: string; spaceId: string }[];
}

const composer = (page: Page, channel: string) =>
  page.getByRole("combobox", { name: `Message #${channel}` });

async function english(page: Page) {
  await page.addInitScript(() => {
    try {
      window.localStorage.setItem("notrelix-locale", "en");
    } catch {
      // Private mode: the default language applies.
    }
  });
}

async function serious(page: Page) {
  const axe = await new AxeBuilder({ page })
    .disableRules(["color-contrast", "scrollable-region-focusable"])
    .analyze();
  return axe.violations.filter((v) =>
    ["serious", "critical"].includes(v.impact ?? ""),
  );
}

test("a guest sees only the channels they were added to, a role is denied posting, a moderator removes a message, search follows membership and a burst is limited", async ({
  page: owner,
  browser,
}) => {
  const unique = Date.now().toString(36);
  const privateName = `acc-p-${unique}`;
  const publicName = `acc-q-${unique}`;
  const word = `zebra${unique}`;

  await english(owner);
  await signIn(owner, realUsers.workspaceOwner);
  const workspace = await findWorkspace(owner, "Workspace 3");
  const ws = workspace.id;
  const base = `/workspaces/${ws}`;
  const channelsPath = `/workspaces/${ws}/chat/channels`;

  const guestContext = await browser.newContext({
    viewport: { width: 1440, height: 900 },
  });
  const guest = await guestContext.newPage();
  await english(guest);
  await signIn(guest, realUsers.workspaceGuest);
  const guestMe = await apiRead<{ userId: string }>(
    guest,
    `/workspaces/${ws}/members/me`,
  );
  const adminContext = await browser.newContext({
    viewport: { width: 1440, height: 900 },
  });
  const admin = await adminContext.newPage();
  await english(admin);
  await signIn(admin, realUsers.workspaceAdmin);
  const adminMe = await apiRead<{ userId: string }>(
    admin,
    `/workspaces/${ws}/members/me`,
  );

  // The owner makes a private channel with the admin and the guest in it, and a public channel the guest is not in.
  const created = await apiWrite<Channel>(owner, "POST", channelsPath, {
    name: privateName,
    isPrivate: true,
  });
  expect(created.status).toBeLessThan(300);
  const privateId = created.data!.id;
  const publicCreated = await apiWrite<Channel>(owner, "POST", channelsPath, {
    name: publicName,
    isPrivate: false,
  });
  const publicId = publicCreated.data!.id;
  const added = await apiWrite(
    owner,
    "POST",
    `${channelsPath}/${privateId}/members`,
    { userIds: [adminMe.userId, guestMe.userId] },
  );
  expect(added.status).toBeLessThan(300);

  // Guest access: only the added space is listed, nothing to browse, and the other channel is the same NotFound as a missing one.
  const guestChannels = await apiRead<Channel[]>(guest, channelsPath);
  expect(guestChannels.map((c) => c.id)).toEqual([privateId]);
  expect(
    await apiRead<Channel[]>(guest, `${channelsPath}?browse=true`),
  ).toEqual([]);
  const api = process.env.VITE_API_URL ?? "http://127.0.0.1:58000/api/v1";
  const asGuest = async (id: string) => {
    const r = await guest.request.get(`${api}${channelsPath}/${id}`);
    const body = JSON.parse(await r.text());
    return {
      status: r.status(),
      body: {
        ...body,
        instance: undefined,
        traceId: undefined,
        correlationId: undefined,
        detail: String(body.detail).replace(/\([0-9a-f-]{36}\)/, "(id)"),
      },
    };
  };
  const hidden = await asGuest(publicId);
  expect(hidden.status).toBe(404);
  expect(hidden).toEqual(await asGuest(crypto.randomUUID()));
  const joinHidden = await apiWrite(
    guest,
    "POST",
    `${channelsPath}/${publicId}/members/me`,
  );
  expect(joinHidden.status).toBe(403);
  const guestCreate = await apiWrite(guest, "POST", channelsPath, {
    name: `nope-${unique}`,
    isPrivate: false,
  });
  expect(guestCreate.status).toBe(403);

  await guest.goto(`${base}/chat/${privateId}`);
  const guestNav = guest.getByRole("navigation", { name: "Channels" });
  await expect(
    guestNav.getByRole("button", { name: new RegExp(`: ${privateName}(,|$)`) }),
  ).toBeVisible({ timeout: 20_000 });
  await expect(
    guestNav.getByRole("button", { name: "New channel" }),
  ).toHaveCount(0);
  await expect(composer(guest, privateName)).toBeVisible();
  expect(await serious(guest)).toEqual([]);
  await guest.goto(`${base}/chat/${publicId}`);
  await expect(guest.getByText("You cannot see this channel")).toBeVisible({
    timeout: 20_000,
  });

  // The owner turns posting off for guests in the private channel, in the permissions dialog.
  await owner.goto(`${base}/chat/${privateId}`);
  await expect(composer(owner, privateName)).toBeVisible({ timeout: 20_000 });
  await owner.getByRole("button", { name: "Channel settings" }).click();
  await owner.getByRole("menuitem", { name: "Permissions" }).click();
  const dialog = owner.getByRole("dialog", { name: "Channel permissions" });
  await expect(dialog.getByRole("table")).toBeVisible();
  expect(await serious(owner)).toEqual([]);
  await dialog
    .getByRole("combobox", { name: "Post messages, Guests" })
    .selectOption("deny");
  await dialog.getByRole("button", { name: "Save", exact: true }).click();
  await expect(owner.getByText("Permissions saved.")).toBeVisible();
  await owner.keyboard.press("Escape");
  await expect(dialog).toHaveCount(0);

  await guest.goto(`${base}/chat/${privateId}`);
  await expect(
    guest
      .getByText("Posting is turned off for your role in this channel.")
      .first(),
  ).toBeVisible({ timeout: 20_000 });
  const guestPost = await apiWrite(
    guest,
    "POST",
    `/workspaces/${ws}/chat/spaces/${privateId}/messages`,
    { content: "blocked", clientMessageId: crypto.randomUUID() },
  );
  expect(guestPost.status).toBe(403);
  // An Admin keeps every action, whatever the override says.
  const removeMe = `removeme ${word}`;
  const keep = `keep ${word}`;
  for (const content of [removeMe, keep]) {
    const posted = await apiWrite(
      admin,
      "POST",
      `/workspaces/${ws}/chat/spaces/${privateId}/messages`,
      { content, clientMessageId: crypto.randomUUID() },
    );
    expect(posted.status).toBeLessThan(300);
  }

  // The owner removes the admin's message as a moderator, with a reason; the tombstone shows to the guest as well.
  await owner.goto(`${base}/chat/${privateId}`);
  const row = owner.locator("[data-message-id]", { hasText: removeMe }).first();
  await expect(row).toBeVisible({ timeout: 20_000 });
  await row.hover();
  await row.getByRole("button", { name: "More actions" }).click();
  await owner.getByRole("menuitem", { name: "Delete as moderator" }).click();
  const moderate = owner.getByRole("dialog", {
    name: "Delete this message as a moderator?",
  });
  await expect(moderate.getByRole("textbox", { name: "Reason" })).toBeVisible();
  await moderate
    .getByRole("textbox", { name: "Reason" })
    .fill("spam in a private channel");
  expect(await serious(owner)).toEqual([]);
  await moderate.getByRole("button", { name: "Delete message" }).click();
  await expect(
    owner.getByText("This message was removed by a moderator"),
  ).toBeVisible();
  await guest.reload();
  await expect(
    guest.getByText("This message was removed by a moderator"),
  ).toBeVisible({
    timeout: 20_000,
  });
  const stored = (
    await apiRead<MessagePage>(
      owner,
      `/workspaces/${ws}/chat/spaces/${privateId}/messages?limit=100`,
    )
  ).items;
  const gone = stored.find((m) => m.deleted)!;
  expect(gone.content).toBeNull();
  // A guest is not a moderator; the Admin's other message stays.
  const kept = stored.find((m) => m.content === keep)!;
  const forbidden = await apiWrite(
    guest,
    "POST",
    `/workspaces/${ws}/chat/messages/${kept.id}/moderation`,
    { reason: "no right", expectedVersion: kept.version },
  );
  expect(forbidden.status).toBe(403);

  // Search: the admin finds the kept message in the private channel through the panel; the guest finds it as a member.
  await admin.goto(`${base}/chat/${privateId}`);
  await admin.getByRole("button", { name: "Search messages" }).first().click();
  const search = admin.getByRole("combobox", { name: "Search text" });
  await search.fill(word);
  await expect(
    admin.getByRole("listbox", { name: "Search results" }).getByRole("option"),
  ).toHaveCount(1, { timeout: 20_000 });
  expect(await serious(admin)).toEqual([]);
  const hits = (q: string, page: Page) =>
    apiRead<SearchPage>(page, `/workspaces/${ws}/chat/search?q=${q}`);
  expect((await hits(word, guest)).items).toHaveLength(1);
  // The guest does not find a message of the public channel they are not in.
  const secret = `quokka${unique}`;
  const secretPost = await apiWrite(
    owner,
    "POST",
    `/workspaces/${ws}/chat/spaces/${publicId}/messages`,
    { content: secret, clientMessageId: crypto.randomUUID() },
  );
  expect(secretPost.status).toBeLessThan(300);
  expect((await hits(secret, owner)).items).toHaveLength(1);
  expect((await hits(secret, guest)).items).toHaveLength(0);
  // A too-short query is refused.
  const tooShort = await admin.request.get(
    `${api}/workspaces/${ws}/chat/search?q=a`,
  );
  expect(tooShort.status()).toBe(400);
  // Removed from the private channel, the admin no longer finds its messages, at once.
  const removed = await apiWrite(
    owner,
    "DELETE",
    `${channelsPath}/${privateId}/members/${adminMe.userId}`,
  );
  expect(removed.status).toBeLessThan(300);
  expect((await hits(word, admin)).items).toHaveLength(0);
  await search.fill("");
  await search.fill(word);
  await expect(admin.getByText("No messages found").first()).toBeVisible({
    timeout: 20_000,
  });

  // Rate limit: a burst of sends in the public channel is answered 429, and the draft in the composer stays.
  let limited = 0;
  for (let i = 0; i < 40 && limited === 0; i += 1) {
    const sent = await apiWrite<{ errorCode?: string }>(
      owner,
      "POST",
      `/workspaces/${ws}/chat/spaces/${publicId}/messages`,
      { content: `burst ${i} ${unique}`, clientMessageId: crypto.randomUUID() },
    );
    if (sent.status === 429) {
      limited = i;
      expect(sent.data?.errorCode).toBe("rate_limit.exceeded");
    }
  }
  expect(limited).toBeGreaterThan(0);
  await owner.goto(`${base}/chat/${publicId}`);
  const draft = `draft ${unique}`;
  await expect(composer(owner, publicName)).toBeVisible({ timeout: 20_000 });
  await composer(owner, publicName).fill(draft);
  await composer(owner, publicName).press("Enter");
  await expect(owner.getByTestId("chat-rate-limit-countdown")).toBeVisible();
  // The text is held in the log as an unsent message, not lost; axe finds nothing serious on the notice.
  await expect(
    owner.getByTestId("chat-pending-message").getByText(draft),
  ).toBeVisible();
  expect(await serious(owner)).toEqual([]);
  // When the wait is over the notice offers the retry, and the held message is sent once and stored.
  await owner
    .getByRole("button", { name: "Send again" })
    .click({ timeout: 90_000 });
  await expect(owner.getByRole("log").getByText(draft)).toHaveCount(1, {
    timeout: 30_000,
  });
  await expect(owner.getByTestId("chat-pending-message")).toHaveCount(0);

  await guestContext.close();
  await adminContext.close();
});

test("an admin exports a person's chat data from the settings dialog: only what the requester can see, a guest is refused, and an Admin who did not ask cannot read the file", async ({
  page: owner,
  browser,
}) => {
  const unique = Date.now().toString(36);
  const sharedName = `exp-t-${unique}`;
  const hiddenName = `exp-r-${unique}`;
  const shown = `shown ${unique}`;
  const hidden = `hidden ${unique}`;

  await english(owner);
  await signIn(owner, realUsers.workspaceOwner);
  const workspace = await findWorkspace(owner, "Workspace 3");
  const ws = workspace.id;
  const channelsPath = `/workspaces/${ws}/chat/channels`;
  const api = process.env.VITE_API_URL ?? "http://127.0.0.1:58000/api/v1";

  const adminContext = await browser.newContext();
  const admin = await adminContext.newPage();
  await english(admin);
  await signIn(admin, realUsers.workspaceAdmin);
  const adminMe = await apiRead<{ userId: string }>(
    admin,
    `/workspaces/${ws}/members/me`,
  );
  const guestContext = await browser.newContext();
  const guest = await guestContext.newPage();
  await signIn(guest, realUsers.workspaceGuest);

  // The admin writes in a public channel and in a private channel the owner is not in.
  const shared = await apiWrite<Channel>(owner, "POST", channelsPath, {
    name: sharedName,
    isPrivate: false,
  });
  const sharedId = shared.data!.id;
  expect(
    (await apiWrite(admin, "POST", `${channelsPath}/${sharedId}/members/me`))
      .status,
  ).toBeLessThan(300);
  const secret = await apiWrite<Channel>(admin, "POST", channelsPath, {
    name: hiddenName,
    isPrivate: true,
  });
  for (const [spaceId, content] of [
    [sharedId, shown],
    [secret.data!.id, hidden],
  ]) {
    const posted = await apiWrite(
      admin,
      "POST",
      `/workspaces/${ws}/chat/spaces/${spaceId}/messages`,
      { content, clientMessageId: crypto.randomUUID() },
    );
    expect(posted.status).toBeLessThan(300);
  }

  // The owner asks for the admin's data in the dialog and downloads the file.
  await owner.goto(`/workspaces/${ws}/chat`);
  await owner.getByRole("button", { name: "Chat settings" }).click();
  const dialog = owner.getByRole("dialog", { name: "Chat settings" });
  expect(await serious(owner)).toEqual([]);
  await dialog
    .getByLabel("Person", { exact: true })
    .selectOption(adminMe.userId);
  await dialog.getByRole("button", { name: "Request export" }).click();
  await expect(dialog.getByText(/^The export is ready:/)).toBeVisible({
    timeout: 60_000,
  });
  const download = owner.waitForEvent("download");
  await dialog.getByRole("button", { name: "Download" }).click();
  const file = JSON.parse(
    await readFile((await (await download).path())!, "utf8"),
  ) as {
    format: string;
    subjectUserId: string;
    messages: { content: string; spaceName: string }[];
    summary: { spacesNotVisibleToRequester: number };
  };
  expect(file.format).toBe("notrelix.chat-export.v1");
  expect(file.subjectUserId).toBe(adminMe.userId);
  const texts = file.messages.map((m) => m.content);
  expect(texts).toContain(shown);
  expect(texts).not.toContain(hidden);
  expect(JSON.stringify(file)).not.toContain(hiddenName);
  expect(file.summary.spacesNotVisibleToRequester).toBeGreaterThan(0);

  // A guest cannot start an export, and the other Admin cannot read the owner's job.
  const refused = await apiWrite(
    guest,
    "POST",
    `/workspaces/${ws}/chat/exports`,
    { subjectUserId: adminMe.userId },
  );
  expect(refused.status).toBe(403);
  const job = await apiWrite<{ id: string }>(
    owner,
    "POST",
    `/workspaces/${ws}/chat/exports`,
    { subjectUserId: adminMe.userId },
  );
  expect(job.status).toBeLessThan(300);
  const foreign = await admin.request.get(
    `${api}/workspaces/${ws}/chat/exports/${job.data!.id}`,
  );
  expect(foreign.status()).toBe(404);

  await adminContext.close();
  await guestContext.close();
});
