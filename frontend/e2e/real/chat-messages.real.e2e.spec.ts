import AxeBuilder from "@axe-core/playwright";
import { expect, test, type Page, type Route } from "@playwright/test";
import {
  apiRead,
  apiWrite,
  findWorkspace,
  realUsers,
  signIn,
} from "./support/real-session";

test.use({ viewport: { width: 1440, height: 900 } });
test.setTimeout(150_000);

interface Message {
  id: string;
  sequence: number;
  kind: string;
  content: string | null;
  edited: boolean;
  deleted: boolean;
  version: number;
}
interface MessagePage {
  items: Message[];
  hasOlder: boolean;
}
interface Channel {
  id: string;
  name: string;
}

const log = (page: Page) => page.getByRole("log");
const composer = (page: Page, channel: string) =>
  page.getByRole("combobox", { name: `Message #${channel}` });

async function messages(page: Page, ws: string, channelId: string) {
  return apiRead<MessagePage>(
    page,
    `/workspaces/${ws}/chat/spaces/${channelId}/messages?limit=100`,
  );
}

async function openMenuOf(page: Page, text: string) {
  const row = page.locator("[data-message-id]", { hasText: text }).first();
  await row.hover();
  await row.getByRole("button", { name: "More actions" }).click();
}

test("a channel is created and its messages are sent, edited, deleted, paged and resent through the real stack, and an outsider is told NotFound", async ({
  page,
  browser,
}) => {
  const unique = Date.now().toString(36);
  const channelName = `e2e-${unique}`;
  await page.addInitScript(() => {
    try {
      window.localStorage.setItem("notrelix-locale", "en");
    } catch {
      // Private mode: the default language applies.
    }
  });
  await signIn(page, realUsers.workspaceOwner);
  const workspace = await findWorkspace(page, "Workspace 3");
  const base = `/workspaces/${workspace.id}`;
  await page.goto(`${base}/chat`);

  // Create a channel through the dialog: it opens, and a duplicate name is refused with nothing stored.
  const nav = page.getByRole("navigation", { name: "Channels" });
  await nav.getByRole("button", { name: "New channel" }).click();
  await page
    .getByRole("dialog")
    .getByRole("textbox", { name: "Name", exact: true })
    .fill(channelName);
  await page
    .getByRole("dialog")
    .getByRole("button", { name: "Create channel" })
    .click();
  await expect(page).toHaveURL(new RegExp(`${base}/chat/[0-9a-f-]{36}`), {
    timeout: 20_000,
  });
  await expect(composer(page, channelName)).toBeVisible();
  const channels = await apiRead<Channel[]>(
    page,
    `/workspaces/${workspace.id}/chat/channels`,
  );
  expect(channels.filter((c) => c.name === channelName)).toHaveLength(1);
  // The route first carries a temporary id; the stored id is the one the list returns, and the page ends on it.
  const channelId = channels.find((c) => c.name === channelName)!.id;
  await expect(page).toHaveURL(new RegExp(`/chat/${channelId}`));
  // Browse lists only the public channels the caller is not in.
  expect(
    (
      await apiRead<Channel[]>(
        page,
        `/workspaces/${workspace.id}/chat/channels?browse=true`,
      )
    ).map((c) => c.id),
  ).not.toContain(channelId);

  const duplicate = await apiWrite(
    page,
    "POST",
    `/workspaces/${workspace.id}/chat/channels`,
    { name: channelName, isPrivate: false },
  );
  expect(duplicate.status).toBe(409);
  expect(
    (
      await apiRead<Channel[]>(
        page,
        `/workspaces/${workspace.id}/chat/channels`,
      )
    ).filter((c) => c.name === channelName),
  ).toHaveLength(1);

  // Send: one bubble, no pending copy, one stored message.
  const first = `First ${unique}`;
  await composer(page, channelName).fill(first);
  await composer(page, channelName).press("Enter");
  await expect(log(page).getByText(first)).toHaveCount(1);
  await expect(page.getByTestId("chat-pending-message")).toHaveCount(0);
  let stored: Message[] = [];
  await expect
    .poll(async () => {
      const pageOfMessages = await messages(page, workspace.id, channelId);
      stored = pageOfMessages.items;
      return stored.filter((m) => m.content === first).length;
    })
    .toBe(1);

  // A retry under the same client message id replays the stored message.
  const key = crypto.randomUUID();
  const sent = await apiWrite<Message>(
    page,
    "POST",
    `/workspaces/${workspace.id}/chat/spaces/${channelId}/messages`,
    { content: `Retry ${unique}`, clientMessageId: key },
    key,
  );
  const replay = await apiWrite<Message>(
    page,
    "POST",
    `/workspaces/${workspace.id}/chat/spaces/${channelId}/messages`,
    { content: `Retry ${unique}`, clientMessageId: key },
    key,
  );
  expect(sent.status).toBeLessThan(300);
  expect(replay.data!.id).toBe(sent.data!.id);
  stored = (await messages(page, workspace.id, channelId)).items;
  expect(stored.filter((m) => m.content === `Retry ${unique}`)).toHaveLength(1);

  // Edit: the text changes, the bubble says edited, the stored version moved.
  const edited = `First ${unique} edited`;
  await openMenuOf(page, first);
  await page.getByRole("menuitem", { name: "Edit message" }).click();
  const editor = page.getByRole("textbox", { name: "Edit message" });
  await editor.fill(edited);
  await page.getByRole("button", { name: "Save", exact: true }).click();
  await expect(log(page).getByText(edited)).toHaveCount(1);
  await expect(
    log(page).getByText("edited", { exact: true }).first(),
  ).toBeVisible();
  const afterEdit = (await messages(page, workspace.id, channelId)).items.find(
    (m) => m.content === edited,
  )!;
  expect(afterEdit.edited).toBe(true);
  // A stale write changes nothing.
  const stale = await apiWrite(
    page,
    "PATCH",
    `/workspaces/${workspace.id}/chat/messages/${afterEdit.id}`,
    { content: "stale", expectedVersion: afterEdit.version - 1 },
  );
  expect(stale.status).toBe(412);
  expect(
    (await messages(page, workspace.id, channelId)).items.find(
      (m) => m.id === afterEdit.id,
    )!.content,
  ).toBe(edited);

  // Delete: confirmed, the bubble becomes a tombstone and keeps its place.
  await openMenuOf(page, edited);
  await page.getByRole("menuitem", { name: "Delete message" }).click();
  await page
    .getByRole("alertdialog")
    .getByRole("button", { name: "Delete", exact: true })
    .click();
  await expect(log(page).getByText("This message was deleted")).toBeVisible();
  const tomb = (await messages(page, workspace.id, channelId)).items.find(
    (m) => m.id === afterEdit.id,
  )!;
  expect(tomb.deleted).toBe(true);
  expect(tomb.content).toBeNull();

  // Browse and join: another member finds the public channel, joins it, and the channel announces it.
  const memberContext = await browser.newContext({
    viewport: { width: 1440, height: 900 },
  });
  const memberPage = await memberContext.newPage();
  await memberPage.addInitScript(() => {
    try {
      window.localStorage.setItem("notrelix-locale", "en");
    } catch {
      // Private mode: the default language applies.
    }
  });
  await signIn(memberPage, realUsers.workspaceAdmin);
  await memberPage.goto(`${base}/chat`);
  await memberPage
    .getByRole("button", { name: `Join #${channelName}` })
    .click();
  await expect(composer(memberPage, channelName)).toBeVisible({
    timeout: 20_000,
  });
  await expect(log(page).getByText("joined the channel")).toBeVisible({
    timeout: 20_000,
  });
  const announced = (
    await messages(page, workspace.id, channelId)
  ).items.filter((m) => m.kind === "system");
  expect(announced.map((m) => m.content)).toEqual(["joined the channel"]);

  // History: more than one page is paged with a cursor, and an older page loads on scroll.
  // The send limit is 30 a minute per person, so two people write 48 messages between them, so nobody nears it.
  for (let i = 1; i <= 48; i += 1) {
    const r = await apiWrite(
      i <= 28 ? memberPage : page,
      "POST",
      `/workspaces/${workspace.id}/chat/spaces/${channelId}/messages`,
      { content: `Bulk ${i} ${unique}`, clientMessageId: crypto.randomUUID() },
    );
    expect(r.status).toBeLessThan(300);
  }
  await memberContext.close();
  await page.reload();
  await expect(log(page).getByText(`Bulk 48 ${unique}`)).toBeVisible({
    timeout: 20_000,
  });
  await expect(log(page).getByText(`Bulk 1 ${unique}`)).toHaveCount(0);
  const newestPage = await apiRead<MessagePage>(
    page,
    `/workspaces/${workspace.id}/chat/spaces/${channelId}/messages`,
  );
  expect(newestPage.items).toHaveLength(50);
  expect(newestPage.hasOlder).toBe(true);
  const newest = newestPage.items;
  const older = await apiRead<MessagePage>(
    page,
    `/workspaces/${workspace.id}/chat/spaces/${channelId}/messages?limit=5&before=${newest[0]!.sequence}`,
  );
  expect(older.items.length).toBeGreaterThan(0);
  expect(older.items.every((m) => m.sequence < newest[0]!.sequence)).toBe(true);
  for (let i = 0; i < 12; i += 1) {
    if ((await log(page).getByText(`Bulk 1 ${unique}`).count()) > 0) break;
    await log(page).evaluate((el) => {
      const scroller = [
        el,
        ...Array.from(el.querySelectorAll<HTMLElement>("*")),
      ]
        .filter((e) => e.scrollHeight > e.clientHeight + 50)
        .pop();
      if (scroller) scroller.scrollTop = 0;
    });
    await page.waitForTimeout(500);
  }
  await expect(log(page).getByText(`Bulk 1 ${unique}`)).toHaveCount(1);

  // Jump to a message: a search hit reopens the history around it, far from the newest window.
  await page.goto(`${base}/chat/${channelId}`);
  await expect(log(page).getByText(`Bulk 48 ${unique}`)).toBeVisible({
    timeout: 20_000,
  });
  await expect(log(page).getByText(`Bulk 2 ${unique}`)).toHaveCount(0);
  await page.getByRole("button", { name: "Search messages" }).first().click();
  await page.locator('input[type="search"]').fill(`Bulk 2 ${unique}`);
  await page.getByRole("listbox").getByRole("option").first().click();
  await expect(log(page).getByText(`Bulk 2 ${unique}`)).toBeVisible({
    timeout: 20_000,
  });

  // axe finds nothing serious on the channel.
  const axe = await new AxeBuilder({ page })
    .disableRules(["color-contrast", "scrollable-region-focusable"])
    .analyze();
  expect(
    axe.violations.filter((v) =>
      ["serious", "critical"].includes(v.impact ?? ""),
    ),
  ).toEqual([]);

  // A failed send stays in the list as Not sent; Resend stores it once.
  await page.goto(`${base}/chat/${channelId}`);
  await expect(composer(page, channelName)).toBeVisible();
  const lost = `Lost ${unique}`;
  await composer(page, channelName).fill(lost);
  const refuse = (route: Route) =>
    route.request().method() === "POST"
      ? route.fulfill({
          status: 503,
          contentType: "application/json",
          body: "{}",
        })
      : route.fallback();
  await page.route("**/chat/spaces/*/messages", refuse);
  await composer(page, channelName).press("Enter");
  await expect(page.getByText("Not sent")).toBeVisible({ timeout: 30_000 });
  await page.unroute("**/chat/spaces/*/messages", refuse);
  await page.getByRole("button", { name: "Resend" }).click();
  await expect(page.getByText("Not sent")).toHaveCount(0, { timeout: 30_000 });
  await expect(log(page).getByText(lost)).toHaveCount(1);
  await expect
    .poll(
      async () =>
        (await messages(page, workspace.id, channelId)).items.filter(
          (m) => m.content === lost,
        ).length,
    )
    .toBe(1);

  // An outsider (a Guest not added to the channel) gets the same NotFound as for a channel that does not exist.
  const guestContext = await browser.newContext();
  const guestPage = await guestContext.newPage();
  await signIn(guestPage, realUsers.workspaceGuest);
  const api = process.env.VITE_API_URL ?? "http://127.0.0.1:58000/api/v1";
  const read = async (id: string) => {
    const r = await guestPage.request.get(
      `${api}/workspaces/${workspace.id}/chat/spaces/${id}/messages`,
    );
    return { status: r.status(), body: await r.text() };
  };
  const hidden = await read(channelId);
  const missing = await read(crypto.randomUUID());
  expect(hidden.status).toBe(404);
  expect(hidden.status).toBe(missing.status);
  const shape = (body: string) => {
    const { instance, traceId, correlationId, ...rest } = JSON.parse(body);
    (void instance, traceId, correlationId);
    return {
      ...rest,
      detail: String(rest.detail).replace(/\([0-9a-f-]{36}\)/, "(id)"),
    };
  };
  expect(shape(hidden.body)).toEqual(shape(missing.body));
  await guestContext.close();
});
