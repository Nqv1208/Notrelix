import AxeBuilder from "@axe-core/playwright";
import { expect, test, type Page, type WebSocketRoute } from "@playwright/test";
import {
  apiRead,
  apiWrite,
  findWorkspace,
  realUsers,
  signIn,
} from "./support/real-session";

// Chat realtime on the real stack (UC-CHT-C01 to C09): two people hold the same channel open in two
// browsers, and what one does reaches the other through the real WebSocket gateway with no reload.
test.use({ viewport: { width: 1440, height: 900 } });
test.setTimeout(180_000);

interface Channel {
  id: string;
  name: string;
}
interface Message {
  id: string;
  content: string | null;
}
interface MessagePage {
  items: Message[];
}

const log = (page: Page) => page.getByRole("log");
const composer = (page: Page, channel: string) =>
  page.getByRole("combobox", { name: `Message #${channel}` });
const channelButton = (page: Page, channel: string) =>
  page
    .getByRole("navigation", { name: "Channels" })
    .getByRole("button", { name: new RegExp(`: ${channel}(,|$)`) });

async function english(page: Page) {
  await page.addInitScript(() => {
    try {
      window.localStorage.setItem("notrelix-locale", "en");
    } catch {
      // Private mode: the default language applies.
    }
  });
}

test("a message, an edit, a delete, typing, unread badges, a lost connection and a removal reach the other person live, and presence shows on a direct conversation", async ({
  page: owner,
  browser,
}) => {
  const unique = Date.now().toString(36);
  const nameA = `rt-a-${unique}`;
  const nameB = `rt-b-${unique}`;

  await english(owner);
  await signIn(owner, realUsers.workspaceOwner);
  const workspace = await findWorkspace(owner, "Workspace 3");
  const ws = workspace.id;
  const base = `/workspaces/${ws}`;

  const create = async (name: string) => {
    const r = await apiWrite<Channel>(
      owner,
      "POST",
      `/workspaces/${ws}/chat/channels`,
      { name, isPrivate: false },
    );
    expect(r.status).toBeLessThan(300);
    return r.data!.id;
  };
  const idA = await create(nameA);
  const idB = await create(nameB);

  // The second person joins both channels, then holds channel A open.
  const readerContext = await browser.newContext({
    viewport: { width: 1440, height: 900 },
  });
  // The reader's WebSocket goes through a route, so the spec can cut it and refuse reconnects.
  let cut = false;
  const sockets: WebSocketRoute[] = [];
  await readerContext.routeWebSocket(/\/realtime/, (ws) => {
    if (cut) {
      void ws.close({ code: 1006 });
      return;
    }
    ws.connectToServer();
    sockets.push(ws);
  });
  const reader = await readerContext.newPage();
  await english(reader);
  await signIn(reader, realUsers.workspaceAdmin);
  const readerMe = await apiRead<{ userId: string }>(
    reader,
    `/workspaces/${ws}/members/me`,
  );
  for (const id of [idA, idB]) {
    const joined = await apiWrite(
      reader,
      "POST",
      `/workspaces/${ws}/chat/channels/${id}/members/me`,
    );
    expect(joined.status).toBeLessThan(300);
  }
  await reader.goto(`${base}/chat/${idA}`);
  await expect(composer(reader, nameA)).toBeVisible({ timeout: 20_000 });
  await owner.goto(`${base}/chat/${idA}`);
  await expect(composer(owner, nameA)).toBeVisible({ timeout: 20_000 });

  // The owner sends while the first window may still be loading: the own bubble must show at once.

  // A new message arrives live (subscribe, fan-out, insert by sequence), once, with no reload.
  const first = `Live ${unique}`;
  await composer(owner, nameA).fill(first);
  await composer(owner, nameA).press("Enter");
  await expect(log(reader).getByText(first)).toHaveCount(1, {
    timeout: 20_000,
  });
  await expect(log(owner).getByText(first)).toHaveCount(1, {
    timeout: 20_000,
  });

  // An edit patches the bubble in place and a delete turns it into a tombstone, both live.
  const edited = `${first} edited`;
  const row = owner.locator("[data-message-id]", { hasText: first }).first();
  await row.hover();
  await row.getByRole("button", { name: "More actions" }).click();
  await owner.getByRole("menuitem", { name: "Edit message" }).click();
  await owner.getByRole("textbox", { name: "Edit message" }).fill(edited);
  await owner.getByRole("button", { name: "Save", exact: true }).click();
  await expect(log(reader).getByText(edited)).toHaveCount(1, {
    timeout: 20_000,
  });
  await expect(log(reader).getByText(first, { exact: true })).toHaveCount(0);
  const editedRow = owner
    .locator("[data-message-id]", { hasText: edited })
    .first();
  await editedRow.hover();
  await editedRow.getByRole("button", { name: "More actions" }).click();
  await owner.getByRole("menuitem", { name: "Delete message" }).click();
  await owner
    .getByRole("alertdialog")
    .getByRole("button", { name: "Delete", exact: true })
    .click();
  await expect(log(reader).getByText("This message was deleted")).toBeVisible({
    timeout: 20_000,
  });
  await expect(log(reader).getByText(edited)).toHaveCount(0);

  // Typing: the reader sees the typing cue while the owner types and not after.
  await composer(owner, nameA).click();
  await composer(owner, nameA).pressSequentially("typing along", {
    delay: 120,
  });
  await expect(reader.getByTestId("chat-typing-bubble")).toBeVisible({
    timeout: 20_000,
  });
  await expect(reader.getByTestId("chat-typing-indicator")).toContainText(
    /is typing/,
  );
  await composer(owner, nameA).fill("");
  await expect(reader.getByTestId("chat-typing-bubble")).toHaveCount(0, {
    timeout: 20_000,
  });

  // Opening B once reads its join notice, so the badge below counts only what follows.
  await channelButton(reader, nameB).click();
  await expect(composer(reader, nameB)).toBeVisible({ timeout: 20_000 });
  await expect(log(reader).getByText("joined the channel")).toBeVisible({
    timeout: 20_000,
  });
  await expect(channelButton(reader, nameB)).not.toHaveAccessibleName(
    /unread/i,
    { timeout: 20_000 },
  );
  await channelButton(reader, nameA).click();
  await expect(composer(reader, nameA)).toBeVisible({ timeout: 20_000 });

  // The per-user stream: a message in channel B moves the reader's badge on B without a reload, and opening B clears it.
  await expect(channelButton(reader, nameB)).not.toHaveAccessibleName(
    /unread/i,
    { timeout: 20_000 },
  );
  const ping = `Badge ${unique}`;
  const sentB = await apiWrite(
    owner,
    "POST",
    `/workspaces/${ws}/chat/spaces/${idB}/messages`,
    { content: ping, clientMessageId: crypto.randomUUID() },
  );
  expect(sentB.status).toBeLessThan(300);
  await expect(channelButton(reader, nameB)).toHaveAccessibleName(/unread/i, {
    timeout: 20_000,
  });
  await channelButton(reader, nameB).click();
  await expect(log(reader).getByText(ping)).toBeVisible({ timeout: 20_000 });
  await expect(channelButton(reader, nameB)).not.toHaveAccessibleName(
    /unread/i,
    { timeout: 20_000 },
  );
  await channelButton(reader, nameA).click();
  await expect(composer(reader, nameA)).toBeVisible();

  // A lost connection: the reader goes offline, two messages are sent meanwhile, and on return both
  // are there once (gap recovery and dedupe) with the reconnect notice gone.
  cut = true;
  for (const ws of sockets.splice(0)) await ws.close({ code: 1006 });
  await expect(
    reader.getByText("Reconnecting. New messages may not show yet."),
  ).toBeVisible({ timeout: 30_000 });
  const missedOne = `Missed one ${unique}`;
  const missedTwo = `Missed two ${unique}`;
  for (const text of [missedOne, missedTwo]) {
    const r = await apiWrite(
      owner,
      "POST",
      `/workspaces/${ws}/chat/spaces/${idA}/messages`,
      { content: text, clientMessageId: crypto.randomUUID() },
    );
    expect(r.status).toBeLessThan(300);
  }
  cut = false;
  await expect(log(reader).getByText(missedOne)).toHaveCount(1, {
    timeout: 60_000,
  });
  await expect(log(reader).getByText(missedTwo)).toHaveCount(1);
  await expect(
    reader.getByText("Reconnecting. New messages may not show yet."),
  ).toHaveCount(0, { timeout: 30_000 });
  const stored = await apiRead<MessagePage>(
    owner,
    `/workspaces/${ws}/chat/spaces/${idA}/messages?limit=100`,
  );
  expect(stored.items.filter((m) => m.content === missedOne)).toHaveLength(1);

  // Presence: a direct conversation shows the other person as online while both are connected.
  const dm = await apiWrite<{ id: string }>(
    owner,
    "POST",
    `/workspaces/${ws}/chat/conversations`,
    { kind: "direct", userId: readerMe.userId },
  );
  expect(dm.status).toBeLessThan(300);
  await reader.goto(`${base}/chat/dm/${dm.data!.id}`);
  await owner.goto(`${base}/chat/dm/${dm.data!.id}`);
  await expect(owner.locator('[data-presence="online"]').first()).toBeVisible({
    timeout: 30_000,
  });

  // axe finds nothing serious on the channel with the typing cue up.
  await owner.goto(`${base}/chat/${idA}`);
  await expect(composer(owner, nameA)).toBeVisible();
  const axe = await new AxeBuilder({ page: owner })
    .disableRules(["color-contrast", "scrollable-region-focusable"])
    .analyze();
  expect(
    axe.violations.filter((v) =>
      ["serious", "critical"].includes(v.impact ?? ""),
    ),
  ).toEqual([]);

  // A change of access while connected: removing the reader changes their open channel at once (a public
  // channel stays readable, but they can no longer post and are offered Join).
  await reader.goto(`${base}/chat/${idA}`);
  await expect(composer(reader, nameA)).toBeEnabled({ timeout: 20_000 });
  const removed = await apiWrite(
    owner,
    "DELETE",
    `/workspaces/${ws}/chat/channels/${idA}/members/${readerMe.userId}`,
  );
  expect(removed.status).toBeLessThan(300);
  await expect(composer(reader, nameA)).toBeDisabled({ timeout: 30_000 });
  await expect(reader.getByText("Join the channel to post.")).toBeVisible();

  await readerContext.close();
});
