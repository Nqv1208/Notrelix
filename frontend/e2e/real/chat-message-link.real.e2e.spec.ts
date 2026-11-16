import AxeBuilder from "@axe-core/playwright";
import { expect, test } from "@playwright/test";
import {
  apiRead,
  apiWrite,
  findWorkspace,
  realUsers,
  signIn,
} from "./support/real-session";

test.use({ viewport: { width: 1440, height: 900 } });
test.setTimeout(150_000);

interface Channel {
  id: string;
  name: string;
}
interface Message {
  id: string;
  sequence: number;
  content: string | null;
}

const english = () => {
  try {
    window.localStorage.setItem("notrelix-locale", "en");
  } catch {
    // Private mode: the default language applies.
  }
};

test("message text is drawn with safe markup, Copy link opens the message for a member, and a stranger learns nothing", async ({
  page,
  browser,
}) => {
  const unique = Date.now().toString(36);
  const name = `link-${unique}`;
  await page.addInitScript(english);
  await signIn(page, realUsers.workspaceOwner);
  const workspace = await findWorkspace(page, "Workspace 3");
  const base = `/workspaces/${workspace.id}`;
  const channelsPath = `/workspaces/${workspace.id}/chat/channels`;

  // A private channel with the owner only, then many messages so the linked one is far from the newest window.
  const created = await apiWrite<Channel>(page, "POST", channelsPath, {
    name,
    isPrivate: true,
  });
  expect(created.status).toBe(201);
  const channelId = created.data!.id;
  const send = (content: string) =>
    apiWrite<Message>(
      page,
      "POST",
      `/workspaces/${workspace.id}/chat/spaces/${channelId}/messages`,
      { content, clientMessageId: crypto.randomUUID() },
    );
  const hostile = `<img src=x onerror="window.__pwned=1"> javascript:window.__pwned=1 ${unique}`;
  const marked = `**bold ${unique}** _soft_ \`code\` https://example.com/path.`;
  await send(`Target ${unique}`);
  let target!: Message;
  await send(hostile);
  await send(marked);
  for (let i = 0; i < 60; i += 1) {
    // The rate limit (UC-CHT-H08) paces a burst; a refused send is stored nowhere, so it is sent again after a pause.
    let sent = await send(`Filler ${i} ${unique}`);
    for (let attempt = 0; sent.status === 429 && attempt < 30; attempt += 1) {
      await page.waitForTimeout(2000);
      sent = await send(`Filler ${i} ${unique}`);
    }
    expect(sent.status).toBe(201);
    // The linked message sits in the middle, far from both the first and the newest window.
    if (i === 30) target = sent.data!;
  }

  await page.goto(`${base}/chat/${channelId}`);
  const log = page.getByRole("log");
  // The history opens at the first unread message, which is the first one here.
  await expect(log.getByText(`Target ${unique}`)).toBeVisible({
    timeout: 20_000,
  });

  // Safe rendering: markup becomes elements, the hostile text stays text, nothing runs.
  await expect(
    log.locator("strong", { hasText: `bold ${unique}` }),
  ).toHaveCount(1);
  await expect(log.locator("em", { hasText: "soft" })).toHaveCount(1);
  await expect(log.locator("code", { hasText: "code" })).toHaveCount(1);
  const link = log.getByRole("link", { name: "https://example.com/path" });
  await expect(link).toHaveAttribute("rel", "noopener noreferrer nofollow ugc");
  await expect(link).toHaveAttribute("target", "_blank");
  await expect(log.getByText(hostile)).toHaveCount(1);
  await expect(log.locator("img")).toHaveCount(0);
  expect(
    await page.evaluate(() => (window as { __pwned?: number }).__pwned),
  ).toBeUndefined();

  // Copy link from the message menu: the address carries the sequence.
  await page.context().grantPermissions(["clipboard-read", "clipboard-write"]);
  await page.goto(`${base}/chat/${channelId}?m=${target.sequence}`);
  const row = page.locator(`[data-message-id="${target.id}"]`);
  await expect(row).toHaveAttribute("data-highlighted", "true", {
    timeout: 20_000,
  });
  await row.hover();
  await row.getByRole("button", { name: "More actions" }).click();
  await page.getByRole("menuitem", { name: "Copy link" }).click();
  const copied = await page.evaluate(() => navigator.clipboard.readText());
  expect(copied).toBe(
    new URL(
      `${base}/chat/${channelId}?m=${target.sequence}`,
      page.url(),
    ).toString(),
  );

  // A member of the channel opens the copied link in a fresh session and lands on the message.
  const memberContext = await browser.newContext({
    viewport: { width: 1440, height: 900 },
  });
  const memberPage = await memberContext.newPage();
  await memberPage.addInitScript(english);
  await signIn(memberPage, realUsers.workspaceAdmin);
  const memberId = (
    await apiRead<{ userId: string }>(
      memberPage,
      `/workspaces/${workspace.id}/members/me`,
    )
  ).userId;
  const added = await apiWrite(
    page,
    "POST",
    `${channelsPath}/${channelId}/members`,
    { userIds: [memberId] },
  );
  expect(added.status).toBeLessThan(300);
  await memberPage.goto(copied);
  await expect(
    memberPage.locator(`[data-message-id="${target.id}"]`),
  ).toHaveAttribute("data-highlighted", "true", { timeout: 30_000 });
  await memberContext.close();

  // A person outside the private channel gets the same answer as for a missing one and sees none of the text.
  const strangerContext = await browser.newContext({
    viewport: { width: 1440, height: 900 },
  });
  const stranger = await strangerContext.newPage();
  await stranger.addInitScript(english);
  await signIn(stranger, realUsers.workspaceGuest);
  await stranger.goto(copied);
  await stranger.waitForTimeout(3000);
  await expect(stranger.getByText(`Target ${unique}`)).toHaveCount(0);
  await expect(stranger.locator("[data-message-id]")).toHaveCount(0);
  await strangerContext.close();

  const axe = await new AxeBuilder({ page })
    .disableRules(["color-contrast", "scrollable-region-focusable"])
    .analyze();
  expect(
    axe.violations.filter((v) =>
      ["serious", "critical"].includes(v.impact ?? ""),
    ),
  ).toEqual([]);
});
